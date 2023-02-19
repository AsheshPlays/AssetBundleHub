using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using AssetBundleHub.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub
{
    public interface IDownloadAssetBundleInfoStore
    {
        AssetBundleList AssetBundleList { get; }
        bool TryGetAssetBundleName(string assetName, out string assetBundleName);
        bool ExistsNewRelease(string assetBundleName); // ダウンロードが必要ならtrueを返す
    }

    public interface IPullAssetBundles
    {
        UniTask PullAssetBundles(IBundlePullContext context, CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IAssetBundleCache
    {
        bool TryGetRef(string assetBundleName, out AssetBundleRef assetBundleRef);
        bool TryGetAssetBundleName(string assetName, out string assetBundleName);
        UniTask<AssetBundle> LoadAsync(string assetBundleName, CancellationToken cancellationToken = default(CancellationToken));
        void Unload(string assetBundleName, bool ignoreRefCount = false);
        void UnloadAll();
    }

    /// <summary>
    /// gitのLocalRepositoryをイメージしたクラス。
    /// ローカルのAssetBundleや状態を管理する。
    /// AssetBundleListの保持
    /// AssetBundleのロード、キャッシュ
    /// </summary>
    public class AssetBundleLocalRepository : IDownloadAssetBundleInfoStore, IPullAssetBundles, IAssetBundleCache
    {
        ILocalAssetBundleTable localAssetBundleTable;
        AssetBundleList assetBundleList;
        public AssetBundleList AssetBundleList => assetBundleList;

        // key: assetName value: assetBundleName
        ReadOnlyDictionary<string, string> assetNameToAssetBundleMap;
        public bool TryGetAssetBundleName(string assetName, out string assetBundleName) => assetNameToAssetBundleMap.TryGetValue(assetName, out assetBundleName);

        // key: assetBundleName value: ロード済みのAssetBundle
        Dictionary<string, AssetBundleRef> assetBundleRefs = new Dictionary<string, AssetBundleRef>();
        public Dictionary<string, AssetBundleRef> GetCurrentAssetBundleRefs() => new Dictionary<string, AssetBundleRef>(assetBundleRefs); // テスト、確認用
        public bool TryGetRef(string assetBundleName, out AssetBundleRef assetBundleRef) => assetBundleRefs.TryGetValue(assetBundleName, out assetBundleRef);

        IAssetBundleReader assetBundleReader;
        // key: assetBundleName
        HashSet<string> loadingAssetBundles = new HashSet<string>();

        public AssetBundleLocalRepository(ILocalAssetBundleTable localAssetBundleTable, IAssetBundleReader assetBundleReader)
        {
            this.localAssetBundleTable = localAssetBundleTable;
            this.assetBundleReader = assetBundleReader;
        }

        string assetBundleListPath
        {
            get
            {
                var settings = AssetBundleHubSettings.Instance;
                return Path.Combine(settings.SaveDataPath, settings.assetBundleListName);
            }
        }

        public bool ExistsAssetBundleList() => File.Exists(assetBundleListPath);
        public async UniTask PullAssetBundleList(CancellationToken cancellationToken = default(CancellationToken))
        {
            // TODO: refactor
            var settings = AssetBundleHubSettings.Instance;
            if (!Directory.Exists(settings.TempSavePath))
            {
                Directory.CreateDirectory(settings.TempSavePath);
            }

            var tempPath = Path.Combine(settings.TempSavePath, settings.assetBundleListName);
            var destPath = Path.Combine(settings.SaveDataPath, settings.assetBundleListName);
            var request = DownloadRequestContext.Create(
                settings.assetBundleListUrl,
                tempPath,
                settings.Timeout
            );
            IFileDownloader fileDownloader = new FileDownloader();
            await fileDownloader.Run(request, cancellationToken);
            if (!Directory.Exists(settings.SaveDataPath))
            {
                Directory.CreateDirectory(settings.SaveDataPath);
            }
            File.Delete(destPath);
            File.Move(tempPath, destPath);
        }

        /// <summary>
        /// AssetBundleListをロード。初期化時に呼ぶ必要がある。
        /// </summary>
        public void LoadAndCacheAssetBundleList()
        {
            var assetBundleListLoader = ServiceLocator.Instance.Resolve<IAssetBundleListLoader>();
            assetBundleList = assetBundleListLoader.Load(assetBundleListPath);
            // Assetのフルパス : AssetBundle名のmapを作ってキャッシュ
            var assetToAssetBundleMap = new Dictionary<string, string>();
            foreach (var kvp in assetBundleList.Infos)
            {
                var assetBundleName = kvp.Key;
                foreach (var assetName in kvp.Value.AssetNames)
                {
                    if (assetToAssetBundleMap.ContainsKey(assetName))
                    {
                        Debug.LogWarning($"AssetBundleに含まれるAssetが重複しています。 Asset {assetName} AB {assetToAssetBundleMap[assetName]}, {assetBundleName}");
                        continue;
                    }
                    assetToAssetBundleMap[assetName] = assetBundleName;
                }
            }

            assetNameToAssetBundleMap = new ReadOnlyDictionary<string, string>(assetToAssetBundleMap);
        }

        public bool ExistsNewRelease(string assetBundleName)
        {
            // Listになければ対象外
            if (!assetBundleList.Infos.TryGetValue(assetBundleName, out AssetBundleInfo assetBundleInfo))
            {
                Debug.LogError($"AssetBundleInfo not found {assetBundleName}");
                return false;
            }

            // ローカルに対象のファイルが存在してなければ新規
            if (!localAssetBundleTable.TryGetHash(assetBundleName, out string localAssetBundleHash))
            {
                return true;
            }

            // ローカルのAssetBundleのバージョンが古かったら新規
            if (assetBundleInfo.Hash != localAssetBundleHash)
            {
                return true;
            }
            return false;
        }

        public async UniTask PullAssetBundles(IBundlePullContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (context.AssetBundleNames == null || context.AssetBundleNames.Count == 0)
            {
                return;
            }

            var pullAssetBundles = new PullAssetBundles();
            await pullAssetBundles.Run(context, cancellationToken);
        }

        // TODO: 共通化
        string GetAssetBundlePath(string assetBundleName)
        {
            return Path.Combine(AssetBundleHubSettings.Instance.SaveDataPath, assetBundleName);
        }

        /// <summary>
        /// AssetBundleをロードする
        /// 参照カウントを更新
        /// キャンセルはロード重複時のみ可能
        /// </summary>
        public async UniTask<AssetBundle> LoadAsync(string assetBundleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!assetBundleList.Infos.TryGetValue(assetBundleName, out AssetBundleInfo abInfo))
            {
                throw new Exception($"assetBundle not found in AssetBundleList {assetBundleName}");
            }

            if (!localAssetBundleTable.Contains(assetBundleName))
            {
                throw new Exception($"assetBundle is not downloaded {assetBundleName}");
            }

            if (assetBundleRefs.TryGetValue(assetBundleName, out AssetBundleRef abRef))
            {
                IncrementRefCountRecursive(assetBundleName, assetBundleName);
                return abRef.AssetBundle;
            }

            if (loadingAssetBundles.Contains(assetBundleName))
            {
                return await GetLoadingAssetBundleAsync(assetBundleName, cancellationToken);
            }

            return await LoadAssetBundleAsync(assetBundleName, abInfo, cancellationToken);
        }

        // ロード終了を待って、参照カウント増やして返す
        async UniTask<AssetBundle> GetLoadingAssetBundleAsync(string assetBundleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            await UniTask.WaitUntil(() => !loadingAssetBundles.Contains(assetBundleName), cancellationToken: cancellationToken);
            if (!assetBundleRefs.TryGetValue(assetBundleName, out AssetBundleRef downloadedAssetBundleRef))
            {
                throw new Exception($"assetBundleRef not found, after wait loading path {assetBundleName}");
            }
            IncrementRefCountRecursive(assetBundleName, assetBundleName);
            return downloadedAssetBundleRef.AssetBundle;
        }

        async UniTask<AssetBundle> LoadAssetBundleAsync(string assetBundleName, AssetBundleInfo abInfo, CancellationToken cancellationToken = default(CancellationToken))
        {
            AssetBundle assetBundle = null;
            try
            {
                loadingAssetBundles.Add(assetBundleName);

                // 依存関係のあるアセットバンドルを全てロードする。
                foreach (var d in abInfo.DirectDependencies)
                {
                    if (assetBundleRefs.ContainsKey(d))
                    {
                        IncrementRefCountRecursive(assetBundleName, d); // 依存先の一部がロード済みなら参照カウント増やす
                        continue;
                    }

                    await LoadAsync(d); // 参照カウントを中途半端に終わらせたくないためここではキャンセルさせない
                }

                var assetBundleRef = await assetBundleReader.LoadFromFileAsync(GetAssetBundlePath(assetBundleName));
                assetBundle = assetBundleRef.AssetBundle;
                if (assetBundle == null)
                {
                    throw new Exception($"loaded assetbundle is null {assetBundleName}");
                }
                assetBundleRefs.Add(assetBundleName, assetBundleRef);
            }
            finally
            {
                loadingAssetBundles.Remove(assetBundleName);
            }
            // ロード時にキャンセルさせない代わりにここで検知
            // キャンセルされていたら、解放しても良いが影響範囲が見えないのでとりあえずやらない。
            cancellationToken.ThrowIfCancellationRequested();
            return assetBundle;
        }

        /// <summary>
        /// 参照カウントを増やす。再帰
        /// </summary>
        /// <param name="root"> 最初の引数となったassetBundleName</param>
        /// <param name="assetBundleName">参照カウントをいじる対象のassetBundleName</param>
        void IncrementRefCountRecursive(string root, string assetBundleName)
        {
            if (!assetBundleRefs.TryGetValue(assetBundleName, out var abRef))
            {
                throw new Exception($"increment assetbundle refcount failed: ref not found, root: {root}, assetBundleName: {assetBundleName}");
            }
            abRef.IncrementRefCount();

            if (!AssetBundleList.Infos.TryGetValue(assetBundleName, out var abInfo))
            {
                throw new Exception($"assetBundle not downloaded {assetBundleName}");
            }

            foreach (var d in abInfo.DirectDependencies)
            {
                IncrementRefCountRecursive(root, d);
            }
        }

        /// <summary>
        /// 参照カウントを減らす
        /// </summary>
        void DecrementRefCountRecursive(string root, string assetBundleName, Action<string, AssetBundleRef> onNoRef)
        {
            if (!assetBundleRefs.TryGetValue(assetBundleName, out var abRef))
            {
                throw new Exception($"decrement assetbundle refcount failed: ref not found, root {root} path {assetBundleName}");
            }
            abRef.DecrementRefCount();
            if (abRef.Count <= 0)
            {
                onNoRef(assetBundleName, abRef);
            }

            if (!AssetBundleList.Infos.TryGetValue(assetBundleName, out var abInfo))
            {
                throw new Exception($"assetBundle not downloaded {assetBundleName}");
            }

            foreach (var d in abInfo.DirectDependencies)
            {
                DecrementRefCountRecursive(assetBundleName, d, onNoRef);
            }
        }

        /// <summary>
        /// ロード済みのAssetBundleを取得
        /// </summary>
        public bool TryGetAssetBundle(string assetBundleName, out AssetBundle value)
        {
            if (assetBundleRefs.TryGetValue(assetBundleName, out var abRef))
            {
                value = abRef.AssetBundle;
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// 参照カウントを減らし、0になったらアンロードする。
        /// </summary>
        /// <param name="assetBundleName">unload target</param>
        /// <param name="ignoreRefCount">シーン切り替え時等必ず解放したいときに使う</param>
        public void Unload(string assetBundleName, bool ignoreRefCount = false)
        {
            if (!assetBundleRefs.TryGetValue(assetBundleName, out var assetBundleRef))
            {
                return;
            }

            int iterationCount = ignoreRefCount ? assetBundleRef.Count : 1;
            for (int i = 0; i < iterationCount; i++)
            {
                DecrementRefCountRecursive(assetBundleName, assetBundleName, onNoRef: (key, abRef) =>
                {
                    abRef.AssetBundle.Unload(true); // NOTE: 読み込み済みAssetは強制解放。参照カウント0ならAssetは参照されていないと判断する。
                    assetBundleRefs.Remove(key);
                });
            }
        }

        /// <summary>
        /// 全てのAssetBundleを解放
        /// 永続的に保持しておきたいAssetBundleもあると思うので基本的には使用しない。
        /// </summary>
        public void UnloadAll()
        {
            foreach (var kvp in assetBundleRefs)
            {
                kvp.Value.AssetBundle.Unload(true);
            }
            assetBundleRefs.Clear();
        }
    }
}
