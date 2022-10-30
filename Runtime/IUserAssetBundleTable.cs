using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleHub
{
    public interface IUserAssetBundle
    {
        string Name { get; } // AssetBundle識別用の名前
        string Hash { get; } // Version管理用のHash
    }

    public interface IReadonlyUserAssetBundleTable
    {
        bool Contains(string assetBundleName);
        bool TryGetHash(string assetBundleName, out string hash);
        //bool TryGetValue(string assetBundleName, out IUserAssetBundle userAssetBundle);
    }

    public interface IUserAssetBundleTable : IReadonlyUserAssetBundleTable
    {
        void BulkSet(List<IUserAssetBundle> values); // まとめてSetして書き込む
    }
}
