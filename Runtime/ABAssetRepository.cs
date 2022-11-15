using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub
{
    /// <summary>
    /// AssetBundleからロードしたAssetのキャッシュ
    /// </summary>
    public class ABAssetRepository
    {
        readonly IAssetBundleCache assetBundleCache;
        // ロードしたAssetのキャッシュ　ダウンキャストのコストが気になるようになった場合にはTypeごとにキャッシュするように修正する想定
        Dictionary<string, UnityEngine.Object> loadedAssets = new Dictionary<string, UnityEngine.Object>();
        HashSet<string> loadingAssets = new HashSet<string>(); // AssetBundleからロード中のassetの名前

        // AssetBundleのAssetロード状態を記録 key: assetBundleName, value: ロードされたassetNames
        Dictionary<string, List<string>> loadedAssetBundles = new Dictionary<string, List<string>>();

        public ABAssetRepository(IAssetBundleCache assetBundleCache)
        {
            this.assetBundleCache = assetBundleCache;
        }

        /// <summary>
        /// AssetBundleを介してAssetをロード
        /// 参照カウントを増やす
        /// </summary>
        /// <param name="assetName">ビルド時に設定したaddressableName</param>
        /// <param name="cancellationToken">キャンセルしても参照カウントはするのでUnload呼ぶ必要あり</param>
        public async UniTask<T> LoadAsync<T>(string assetName, CancellationToken cancellationToken = default(CancellationToken))
            where T : UnityEngine.Object
        {
            if (!assetBundleCache.TryGetAssetBundleName(assetName, out string assetBundleName))
            {
                throw new Exception($"get assetBundleName failed {assetName}");
            }

            if (loadingAssets.Contains(assetName))
            {
                await UniTask.WaitUntil(() => !loadingAssets.Contains(assetName)); // NOTE: キャンセルしても参照カウントは増やしたいのでcancellationTokenを伝搬しない
            }

            loadingAssets.Add(assetName);
            T asset = null;
            try
            {
                var assetBundle = await assetBundleCache.LoadAsync(assetBundleName, cancellationToken); // 参照カウントを増やす
                asset = await LoadAssetAsync<T>(assetName, assetBundle, cancellationToken);
                loadedAssets[assetName] = asset;
                if (!loadedAssetBundles.ContainsKey(assetBundleName))
                {
                    loadedAssetBundles[assetBundleName] = new List<string>();
                }
                loadedAssetBundles[assetBundleName].Add(assetName);
            }
            finally
            {
                loadingAssets.Remove(assetName);
            }
            return asset;
        }

        async UniTask<T> LoadAssetAsync<T>(string assetName, AssetBundle assetBundle, CancellationToken cancellationToken = default(CancellationToken))
            where T : UnityEngine.Object
        {
            var assetObject = await assetBundle.LoadAssetAsync<T>(assetName).ToUniTask(cancellationToken: cancellationToken);
            if (assetObject == null)
            {
                throw new Exception($"loaded asset is null : {assetName}");
            }

            var asset = assetObject as T;
            if (asset == null)
            {
                throw new Exception($"loaded asset type is not {typeof(T).Name}");
            }
            return asset;
        }

        /// <summary>
        /// ロード済みのAssetを取得
        /// 参照カウントを増やさない
        /// </summary>
        public T GetAsset<T>(string assetName) where T : UnityEngine.Object
        {
            T asset = null;
            if (!loadedAssets.TryGetValue(assetName, out var assetObject))
            {
                return null;
            }

            if (assetObject == null)
            {
                throw new Exception($"asset unloaded but key exists : {assetName}");
            }

            asset = assetObject as T;
            if (asset == null)
            {
                throw new Exception($"asset type is not {typeof(T).Name}");
            }
            return asset;
        }

        /// <summary>
        /// AssetをUnload。AssetBundleが複数のAssetを持っている場合には全てのAssetが消えるまでUnity側のメモリにキャッシュはされてる。
        /// 2箇所でロードされたとしても、1箇所でUnloadしたら本クラスのキャッシュからは消す
        /// (ただし、メモリに残っていたらLoadAssetで同期的に取得は可能)
        /// </summary>
        /// <param name="ignoreRefCount">trueにすると属するAssetBundleを参照カウントを無視して解放する。関連するロードされていたAssetが残っていた場合nullになる</param>
        public void Unload(string assetName, bool ignoreRefCount = false)
        {
            if (loadingAssets.Contains(assetName))
            {
                // ロード中にUnloadは禁止。
                Debug.LogWarning($"unload failed. loading asset can't unload assetName: {assetName}");
                return;
            }

            if (!assetBundleCache.TryGetAssetBundleName(assetName, out string assetBundleName))
            {
                throw new Exception($"get assetBundleName failed {assetName}");
            }

            if (!assetBundleCache.TryGetRef(assetBundleName, out var abRef))
            {
                return; // NOTE: Unload済み
            }

            // AssetBundle.LoadAssetを使わない方針のため、AssetBundle解放のタイミングまではAssetのキャッシュを解放しない。
            if (abRef.Count <= 1 || ignoreRefCount)
            {
                foreach (var removeAssetName in loadedAssetBundles[assetBundleName])
                {
                    loadedAssets[removeAssetName] = null; // nullをいれないと解放されないケースがMonoBehaviourにおいてはあるので念の為
                    loadedAssets.Remove(removeAssetName);
                }
                loadedAssetBundles.Remove(assetBundleName);
            }

            assetBundleCache.Unload(assetBundleName, ignoreRefCount);
        }
    }
}
