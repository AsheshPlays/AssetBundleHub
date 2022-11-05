using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleHub
{
    public interface ILocalAssetBundle
    {
        string Name { get; } // AssetBundle識別用の名前
        string Hash { get; } // Version管理用のHash
    }

    public interface IReadonlyLocalAssetBundleTable
    {
        bool Contains(string assetBundleName);
        bool TryGetHash(string assetBundleName, out string hash);
        //bool TryGetValue(string assetBundleName, out ILocalAssetBundle localAssetBundle);
    }

    public interface ILocalAssetBundleTable : IReadonlyLocalAssetBundleTable
    {
        void BulkSet(List<ILocalAssetBundle> values); // まとめてSetして書き込む
    }
}
