using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System;
using System.IO;

namespace AssetBundleHubEditor
{
    public class ExtractSharedAssets
    {
        /// <summary>
        /// 2つ以上のグループから参照されていて、かつgroup内にないファイルが共有ファイル
        /// </summary>
        /// <param name="groups"></param>
        /// <returns>共有ファイルのパスのリスト</returns>
        public List<string> Run(Dictionary<string, List<string>> groups)
        {
            // key: 依存しているAssetのパス value: 参照しているグループの数
            Dictionary<string, int> dependenciesRefCount = new Dictionary<string, int>();

            HashSet<string> allAssetPathsInGroup = new HashSet<string>(groups.SelectMany(x => x.Value));

            foreach (var group in groups)
            {
                HashSet<string> groupDependencies = new HashSet<string>();
                foreach (var assetPath in group.Value)
                {
                    var assetDependencies = AssetDatabase.GetDependencies(assetPath, recursive: true); // 全依存ファイルを取得
                    foreach (var depPath in assetDependencies)
                    {
                        // すでにグループに入ってるassetは抽出しない
                        if (allAssetPathsInGroup.Contains(depPath))
                        {
                            continue;
                        }

                        // グループ内で1つ目ならば参照カウント
                        if (groupDependencies.Add(depPath))
                        {
                            if (dependenciesRefCount.ContainsKey(depPath))
                            {
                                dependenciesRefCount[depPath] += 1;
                            }
                            else
                            {
                                dependenciesRefCount[depPath] = 1;
                            }
                        }
                    }
                }
            }

            // 2つ以上のグループから参照されていて、かつgroup内にないファイルが共有ファイル
            var sharedAssetPaths = new List<string>();
            foreach (var kvp in dependenciesRefCount)
            {
                if (kvp.Value >= 2)
                {
                    sharedAssetPaths.Add(kvp.Key);
                }
            }
            return sharedAssetPaths;
        }
    }
}
