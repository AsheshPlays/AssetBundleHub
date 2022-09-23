using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AssetBundleHub;

namespace AssetBundleHubEditor
{
    public class BuildMapFactory
    {
        public Dictionary<string, List<string>> Create(string loadPath, bool useFileNameHash = true)
        {
            // FolderからAsset取得
            var loadFromDirectory = LoadFromDirectory.CreateWithIgnoreDep();
            var allFilePaths = loadFromDirectory.Run(loadPath);

            // Grouping
            var groupByFilePath = new GroupByFilePath();
            var groups = groupByFilePath.Run(allFilePaths);

            // 依存している共通アセットを別グループ化
            var extractSharedAssets = new ExtractSharedAssets();
            var sharedAssets = extractSharedAssets.Run(groups);
            var sharedGroups = groupByFilePath.Run(sharedAssets);

            // AssetBundle名をつける
            // TODO: パスからハッシュを生成するのではなく、AssetBundleのファイルのHashを名前につけたほうがいい気がしている。
            var buildMap = new Dictionary<string, List<string>>();
            IHashGenerator hashGenerator = null;
            if (useFileNameHash)
            {
                hashGenerator = new HashGenerator("AssetBundleNameKey"); // TODO: このまま行くならキーの差し替えできるようにする
            }
            foreach (var group in groups)
            {
                // パスをそのままバンドル名にする。
                string bundleName = "";
                if (!useFileNameHash)
                {
                    bundleName = group.Key.Substring(loadPath.Length, group.Key.Length - loadPath.Length);
                    bundleName = bundleName.Replace("/", "");
                }
                else
                {
                    bundleName = hashGenerator.GenerateHash(group.Key);
                }
                buildMap[bundleName] = group.Value;
            }

            foreach (var group in sharedGroups)
            {
                string bundleName = "";
                if (!useFileNameHash)
                {
                    bundleName = group.Key.Substring(loadPath.Length, group.Key.Length - loadPath.Length);
                    bundleName = bundleName.Replace("/", "");
                }
                else
                {
                    bundleName = hashGenerator.GenerateHash(group.Key);
                }
                buildMap[bundleName] = group.Value;
            }
            return buildMap;
        }
    }
}
