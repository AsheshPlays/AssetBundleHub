using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleHub
{
    /// <summary>
    /// AssetBundleをキャッシュするための入れ物
    /// 多重ロードを避けるためにキャッシュは必須
    /// CountはLoadで+1 Unloadで-1 Getでは不変
    /// </summary>
    public class AssetBundleRef
    {
        public AssetBundle AssetBundle { get; private set; }
        public int Count { get; private set; }
        List<IDisposable> disposables; // Streamの解放タイミングを制御するために保持

        public AssetBundleRef(AssetBundle assetBundle, List<IDisposable> disposables = null)
        {
            if (assetBundle == null)
            {
                throw new ArgumentNullException("assetBundle is null");
            }

            AssetBundle = assetBundle;
            Count = 1;
            this.disposables = disposables;
        }

        internal void IncrementRefCount()
        {
            Count++;
        }

        internal void DecrementRefCount()
        {
            Count--;
        }

        public void Unload(bool unloadAllLoadedObjects)
        {
            AssetBundle.Unload(unloadAllLoadedObjects);
            if (disposables != null)
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
