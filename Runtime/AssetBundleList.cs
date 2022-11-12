using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;

namespace AssetBundleHub
{
    [Serializable]
    public class AssetBundleList : ISerializationCallbackReceiver
    {
        [SerializeField] int version = 1;
        public int Version => version; // AssetBundleListの形式のバージョン

        // Dictionaryとして扱いたいが、SerializeできないためListのメンバ変数を持っておく
        public ReadOnlyDictionary<string, AssetBundleInfo> Infos { get; private set; }

        [SerializeField] List<AssetBundleInfo> assetBundleInfoList; // Inspectorビューでの確認用変数

        public AssetBundleList(List<AssetBundleInfo> infoList)
        {
            Infos = new ReadOnlyDictionary<string, AssetBundleInfo>(infoList.ToDictionary(x => x.Name, x => x));
        }

        public void OnBeforeSerialize()
        {
            assetBundleInfoList = Infos.Values.ToList();
        }

        public void OnAfterDeserialize()
        {
            Infos = new ReadOnlyDictionary<string, AssetBundleInfo>(assetBundleInfoList.ToDictionary(x => x.Name, x => x));
        }

        /// <summary>
        /// 依存する全AssetBundle名を返す。自身は含めない
        /// </summary>
        public List<string> GetAllDependencies(string assetBundleName)
        {
            var depSet = new HashSet<string>();
            GetDependenciesRecursive(assetBundleName, assetBundleName, depSet);
            return depSet.ToList();
        }

        void GetDependenciesRecursive(string srcAssetBundle, string targetAssetBundle, HashSet<string> depSet)
        {
            if (!Infos.TryGetValue(targetAssetBundle, out AssetBundleInfo assetBundleInfo))
            {
                throw new Exception($"AssetBundleInfo not found {targetAssetBundle} src {srcAssetBundle}");
            }

            foreach (var dep in assetBundleInfo.DirectDependencies)
            {
                depSet.Add(dep);
                GetDependenciesRecursive(srcAssetBundle, dep, depSet);
            }
        }

        // TODO: AssetBundleListを暗号化したらこのメソッドは使えないので別メソッドを経由する。
        public static AssetBundleList LoadFromFile(string path)
        {
            string assetBundleListJson = File.ReadAllText(path);
            return JsonUtility.FromJson<AssetBundleList>(assetBundleListJson);
        }
    }

    [Serializable]
    public class AssetBundleInfo
    {
        [SerializeField] string name = "";
        public string Name => name;

        [SerializeField] string hash = "";
        public string Hash => hash; // この値が異なっていたらダウンロードする

        [SerializeField] string fileHash = "";
        public string FileHash => fileHash; // ファイルから算出したFileHashが不一致なら破損している。

        [SerializeField] int size = 0; // 単位はバイト, 型がintなので2GB未満に抑えること。
        public int Size => size;

        // 直接依存するAssetBundleのパス
        [SerializeField] List<string> directDependencies;
        public List<string> DirectDependencies => directDependencies;

        // NOTE: プロジェクト直下Assetsからのパスではなく、ビルド時に設定したaddressableNameが格納されている。
        [SerializeField] List<string> assetNames;
        public List<string> AssetNames => assetNames;

        public AssetBundleInfo(string name, string hash, string fileHash, int size, List<string> directDependencies, List<string> assetNames)
        {
            this.name = name;
            this.hash = hash;
            this.fileHash = fileHash;
            this.size = size;
            this.directDependencies = directDependencies;
            this.assetNames = assetNames;
        }
    }
}
