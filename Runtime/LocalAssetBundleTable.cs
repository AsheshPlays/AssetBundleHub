using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

namespace AssetBundleHub
{
    /// <summary>
    /// ローカルに保存済みのAssetBundleを記録
    /// SQLite等に変更するのもあり。
    /// </summary>
    [Serializable]
    public class LocalAssetBundleTable : ILocalAssetBundleTable, ISerializationCallbackReceiver
    {
        [Serializable]
        class Model
        {
            public string name;
            public string hash;

            public Model(string name, string hash)
            {
                this.name = name;
                this.hash = hash;
            }
        }

        // key: assetBundleName, value: hash
        Dictionary<string, string> assetBundleHashMap = null;

        [SerializeField] List<Model> data = null;

        public string FilePath { get; set; }

        public LocalAssetBundleTable(string filePath)
        {
            FilePath = filePath;
            assetBundleHashMap = new Dictionary<string, string>();
        }

        /// <summary>
        /// なかったらnullを返す
        /// </summary>
        public static LocalAssetBundleTable LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var contents = File.ReadAllText(filePath);
            var obj = JsonUtility.FromJson<LocalAssetBundleTable>(contents);
            obj.FilePath = filePath;
            return obj;
        }

        /// <summary>
        /// FilePathが空でなければ保存
        /// </summary>
        public bool TrySave()
        {
            if (!string.IsNullOrEmpty(FilePath))
            {
                return false;
            }

            string dirPath = Path.GetDirectoryName(FilePath);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            var contents = JsonUtility.ToJson(this);
            File.Delete(FilePath);
            File.WriteAllText(FilePath, contents);
            return true;
        }

        /// <summary>
        /// まとめて書き込み。
        /// ファイルに保存もする
        /// </summary>
        /// /// <param name="values">key: assetBundleName, value: version管理用のhash</param>
        public void BulkSet(Dictionary<string, string> values)
        {
            foreach (var kvp in values)
            {
                assetBundleHashMap[kvp.Key] = kvp.Value;
            }
            TrySave();
        }

        public bool Contains(string assetBundleName)
        {
            return assetBundleHashMap.ContainsKey(assetBundleName);
        }

        public bool TryGetHash(string assetBundleName, out string hash)
        {
            return assetBundleHashMap.TryGetValue(assetBundleName, out hash);
        }

        public void OnAfterDeserialize()
        {
            assetBundleHashMap = data.ToDictionary(x => x.name, x => x.hash);
        }

        public void OnBeforeSerialize()
        {
            data = assetBundleHashMap.Select(x => new Model(x.Key, x.Value)).ToList();
        }

        public void Clear()
        {
            assetBundleHashMap.Clear();
            TrySave();
        }

        public static LocalAssetBundleTable Create()
        {
            string filePath = AssetBundleHubSettings.Instance.localAssetBundleTablePath;
            LocalAssetBundleTable instance = null;
            try
            {
                instance = LoadFromFile(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }

            if (instance == null)
            {
                instance = new LocalAssetBundleTable(filePath);
            }
            return instance;
        }
    }
}
