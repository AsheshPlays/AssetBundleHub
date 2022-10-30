using System;
using UnityEngine;

namespace AssetBundleHub
{
    [System.Serializable]
    public class AssetBundleHubSettings
    {
        static AssetBundleHubSettings instance = null;
        public static AssetBundleHubSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new Exception("AssetBundleHubSettings id not loaded");
                }
                return instance;
            }
        }

        // Initiallize後に入れる想定
        public int timeoutSec = 30;
        public string baseUrl;

        string tempSavePath;
        public string TempSavePath => tempSavePath;
        string saveDataPath;
        public string SaveDataPath => saveDataPath;
        // TODO: BuiltinAssetのパス

        public TimeSpan Timeout => TimeSpan.FromSeconds(timeoutSec);

        static AssetBundleHubSettings EditorSettings()
        {
            return new AssetBundleHubSettings()
            {
                tempSavePath = "Temp/AB",
                saveDataPath = "SaveData/AB"
            };
        }

        // TODO: iosでBackup対象からはずすこと
        // https://light11.hatenadiary.com/entry/2019/10/07/031405
        static AssetBundleHubSettings AppSettings()
        {
            return new AssetBundleHubSettings()
            {
                tempSavePath = Application.temporaryCachePath + "/AB",
                saveDataPath = Application.persistentDataPath + "/AB"
            };
        }

        public static void Load()
        {
#if UNITY_EDITOR
            instance = EditorSettings();
#else
            instance = AppSettings();
#endif
        }
    }
}
