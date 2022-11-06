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

    /// <summary>
    /// gitのLocalRepositoryをイメージしたクラス。
    /// ローカルのAssetBundleや状態を管理する。
    /// AssetBundleListの保持
    /// AssetBundleのロード、キャッシュ
    /// </summary>
    public class AssetBundleLocalRepository : IDownloadAssetBundleInfoStore, IPullAssetBundles
    {
        ILocalAssetBundleTable localAssetBundleTable;
        AssetBundleList assetBundleList;
        public AssetBundleList AssetBundleList => assetBundleList;

        // key: assetName value: assetBundleName
        ReadOnlyDictionary<string, string> assetNameToAssetBundleMap;

        public AssetBundleLocalRepository(ILocalAssetBundleTable localAssetBundleTable)
        {
            this.localAssetBundleTable = localAssetBundleTable;
        }

        string assetBundleListPath
        {
            get
            {
                var settings = AssetBundleHubSettings.Instance;
                return Path.Combine(settings.SaveDataPath, settings.assetBundleListName);
            }
        }

        public bool TryGetAssetBundleName(string assetName, out string assetBundleName) => assetNameToAssetBundleMap.TryGetValue(assetName, out assetBundleName);

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
    }
}
