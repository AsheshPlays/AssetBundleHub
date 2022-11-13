using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub
{
    public class ABHub
    {
        static ABHub instance;

        AssetBundleLocalRepository localRepository;
        ABAssetRepository assetRepository;
        ABSceneRepository sceneRepository;

        public static void Initialize()
        {
            instance = new ABHub();
            // SettingsがLoadされていなければここで読み込むが、上書きする場合には事前にLoadしておくこと。
            AssetBundleHubSettings.Load();
            var localAssetBundleTable = ServiceLocator.Instance.Resolve<ILocalAssetBundleTable>();
            var assetBundleReader = ServiceLocator.Instance.Resolve<IAssetBundleReader>();
            instance.localRepository = new AssetBundleLocalRepository(localAssetBundleTable, assetBundleReader);
            instance.assetRepository = new ABAssetRepository(instance.localRepository);
            instance.sceneRepository = new ABSceneRepository(instance.localRepository);
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

        public static AssetContainer CreateLoadContainer()
        {
            return new AssetContainer(instance.assetRepository);
        }

        public static T GetAsset<T>(string assetName) where T : UnityEngine.Object
        {
            return instance.assetRepository.GetAsset<T>(assetName);
        }


        /// <param name="sceneName">sceneのAssetBundleのaddressableName</param>
        public static bool IsSceneAssetBundleLoaded(string sceneName) => instance.sceneRepository.IsSceneAssetBundleLoaded(sceneName);
        public static UniTask<AssetBundle> LoadSceneAssetBundleAsync(string sceneName) => instance.sceneRepository.LoadAsync(sceneName);
        public static void UnloadSceneAssetBundle(string sceneName) => instance.sceneRepository.Unload(sceneName);

        /// <summary>
        /// ロードしたAssetBundleの状態を確認したい時等に使う
        /// </summary>
        public static ABHubReader CreateReader()
        {
            var reader = new ABHubReader();
            reader.localRepository = instance.localRepository;
            return reader;
        }

        public static void UnloadAllAssetBundles() => instance.localRepository.UnloadAll();
    }
}
