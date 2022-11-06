using System.Collections;
using System.Collections.Generic;
using AssetBundleHub;
using UnityEngine;

namespace AssetBundleHubTests
{
    public class MockLocalAssetBundleTable : ILocalAssetBundleTable
    {
        // key assetBundleName, value: hash
        public Dictionary<string, string> assetBundleHashMap = new Dictionary<string, string>();

        public void BulkSet(Dictionary<string, string> values)
        {
            foreach (var kvp in values)
            {
                assetBundleHashMap[kvp.Key] = kvp.Value;
            }
        }

        public bool Contains(string assetBundleName)
        {
            return assetBundleHashMap.ContainsKey(assetBundleName);
        }

        public bool TryGetHash(string assetBundleName, out string hash)
        {
            return assetBundleHashMap.TryGetValue(assetBundleName, out hash);
        }

        public static MockLocalAssetBundleTable Register()
        {
            var mock = new MockLocalAssetBundleTable();
            ServiceLocator.Instance.Register<ILocalAssetBundleTable>(mock);
            return mock;
        }
    }
}
