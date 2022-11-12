using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleHub
{
    public interface IReadonlyLocalAssetBundleTable
    {
        bool Contains(string assetBundleName);
        bool TryGetHash(string assetBundleName, out string hash);
    }

    public interface ILocalAssetBundleTable : IReadonlyLocalAssetBundleTable
    {
        /// <summary>
        /// まとめてSetして書き込む
        /// </summary>
        /// <param name="values">key: assetBundleName, value: version管理用のhash</param>
        void BulkSet(Dictionary<string, string> values);
    }
}
