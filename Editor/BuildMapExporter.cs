using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AssetBundleHub;
using System.IO;

namespace AssetBundleHubEditor
{
    /// <summary>
	/// AssetBundleビルドをスキップして対象を出力するためのクラス
	/// 依存関係は可視化しない。
	/// </summary>
    public class BuildMapExporter
    {
        public string ExportPath { get; set; } = "BuildMap.json";
        public void Export(Dictionary<string, List<string>> buildMap)
        {
            List<AssetBundleInfo> infoList = new List<AssetBundleInfo>();
            foreach (var item in buildMap)
            {
                infoList.Add(new AssetBundleInfo(
                    name: item.Key,
                    hash: "",
                    fileHash: "",
                    size: 0,
                    directDependencies: null,
                    assetNames: item.Value
                ));
            }

            var abList = new AssetBundleList(infoList);
            var abListJSON = JsonUtility.ToJson(abList);
            File.WriteAllText(ExportPath, abListJSON);
        }
    }
}
