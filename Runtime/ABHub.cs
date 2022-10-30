using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleHub
{
    public class ABHub
    {
        AssetBundleLocalRepository localRepository;

        public void Initialize()
        {
            // TODO: localRepositoryの初期化
        }

        public AssetBundleDownloader CreateDownloader()
        {
            IDownloadAssetBundleInfoStore assetBundleInfoStore = localRepository;
            IPullAssetBundles repository = localRepository;
            return new AssetBundleDownloader(
                assetBundleInfoStore,
                repository
            );
        }
    }
}
