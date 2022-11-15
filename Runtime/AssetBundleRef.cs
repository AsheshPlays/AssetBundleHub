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

        public AssetBundleRef(AssetBundle assetBundle)
        {
            if (assetBundle == null)
            {
                throw new ArgumentNullException("assetBundle is null");
            }

            AssetBundle = assetBundle;
            Count = 1;
        }

        internal void IncrementRefCount()
        {
            Count++;
        }

        internal void DecrementRefCount()
        {
            Count--;
        }
    }
}
