using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub
{
    public class ABSceneRepository
    {
        IAssetBundleCache assetBundleCache;

        public ABSceneRepository(IAssetBundleCache assetBundleCache)
        {
            this.assetBundleCache = assetBundleCache;
        }

        /// <summary>
        /// AssetBundleがロードされているかどうか
        /// </summary>
        public bool IsSceneAssetBundleLoaded(string sceneName)
        {
            if (!assetBundleCache.TryGetAssetBundleName(sceneName, out string abName))
            {
                UnityEngine.Debug.LogError($"assetBundle not found scene name {sceneName}");
                return false;
            }

            if (!assetBundleCache.TryGetRef(abName, out var abRef))
            {
                return false;
            }

            return abRef.Count > 0;
        }

        public async UniTask<AssetBundle> LoadAsync(string sceneName)
        {
            if (!assetBundleCache.TryGetAssetBundleName(sceneName, out string abName))
            {
                throw new Exception($"assetBundle not found scene name {sceneName}");
            }

            var found = assetBundleCache.TryGetRef(abName, out var abRef);

            if(found && abRef.Count > 0)
            {
                Debug.LogError($"{abName} already loaded");
                return abRef.AssetBundle;
            }

            return await assetBundleCache.LoadAsync(abName);
        }

        public void Unload(string sceneName)
        {
            if (!assetBundleCache.TryGetAssetBundleName(sceneName, out string abName))
            {
                throw new Exception($"assetBundle not found scene name {sceneName}");
            }

            assetBundleCache.Unload(abName);
        }
    }
}
