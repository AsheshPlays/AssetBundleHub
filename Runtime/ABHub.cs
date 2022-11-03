using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub
{
    public class ABHub
    {
        static ABHub instance;

        AssetBundleLocalRepository localRepository;

        public static void Initialize()
        {
            instance = new ABHub();
            AssetBundleHubSettings.Load();
            instance.localRepository = new AssetBundleLocalRepository();
        }

        public static bool ExistsAssetBundleList() => instance.localRepository.ExistsAssetBundleList();
        public static UniTask DownloadAssetBundleList(CancellationToken cancellationToken = default(CancellationToken))
        {
            return instance.localRepository.PullAssetBundleList(cancellationToken);
        }
        public static void LoadAndCacheAssetBundleList()
        {
            instance.localRepository.LoadAndCacheAssetBundleList();
        }


        public static AssetBundleDownloader CreateDownloader()
        {
            IDownloadAssetBundleInfoStore assetBundleInfoStore = instance.localRepository;
            IPullAssetBundles repository = instance.localRepository;
            return new AssetBundleDownloader(
                assetBundleInfoStore,
                repository
            );
        }
    }
}
