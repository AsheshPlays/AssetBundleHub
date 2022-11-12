using System.Collections;
using System.Collections.Generic;
using AssetBundleHub;
using UnityEngine;

namespace AssetBundleHubTests
{
    public class AssetBundleHubSettingsFixture
    {
        public static void BuildInstance()
        {
            AssetBundleHubSettings.Load();
            var settings = AssetBundleHubSettings.Instance;
            settings.baseUrl = "https://teach310.github.io/AssetBundleHubSample/AssetBundles/StandaloneOSX/";
            settings.tempSavePath = Utils.testDir + "/Temp";
            settings.saveDataPath = Utils.testDir + "/SaveData";
            settings.localAssetBundleTablePath = Utils.testDir + "/SaveData/LocalAssetBundleTable.json";
            string assetBundleListName = "AssetBundleList.json";
            settings.assetBundleListUrl = settings.baseUrl + assetBundleListName;
            settings.assetBundleListName = assetBundleListName;
            settings.parallelCount = 4;
            settings.shuffle = true;
        }
    }
}
