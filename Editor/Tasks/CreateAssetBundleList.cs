using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;
using UnityEngine.Build.Pipeline;
using AssetBundleHub;
using AssetBundleHubEditor.Interfaces;

namespace AssetBundleHubEditor.Tasks
{
    public class CreateAssetBundleList : IBuildTask
    {
        public int Version => 1;

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBundleBuildResults results;

        [InjectContext(ContextUsage.In)]
        IABHubBuildParameters parameters;
#pragma warning restore 649

        IFileHashGenerator fileHashGenerator;

        public CreateAssetBundleList(IFileHashGenerator fileHashGenerator)
        {
            this.fileHashGenerator = fileHashGenerator;
        }

        public ReturnCode Run()
        {
            var assetBundleList = BuildDetailsToAssetBundleList(results.BundleInfos);
            File.WriteAllText(parameters.GetOutputFilePathForIdentifier(parameters.AssetBundleListName), JsonUtility.ToJson(assetBundleList));
            return ReturnCode.Success;
        }

        /// <summary>
        /// AssetBundleListを作成
        /// </summary>
        /// <param name="bundleInfos">keyはassetBundleのファイル名、value.FileNameは相対パス</param>
        /// <returns></returns>
        AssetBundleList BuildDetailsToAssetBundleList(Dictionary<string, BundleDetails> bundleInfos)
        {
            var infoList = new List<AssetBundleInfo>();
            AssetBundle.UnloadAllAssetBundles(true); // すでにロード済みだとバグるので
            foreach (var kvp in bundleInfos)
            {
                string assetBundleName = kvp.Key;
                var details = kvp.Value;
                string assetBundleRelativePath = details.FileName;
                var dep = details.Dependencies.ToList();
                string hash = fileHashGenerator.ComputeHash(assetBundleRelativePath);
                string fileHash = hash; // 破損チェック用

                // ファイルサイズ取得
                var fileInfo = new FileInfo(assetBundleRelativePath);
                long fileSize = fileInfo.Length;

                if (fileSize > int.MaxValue)
                {
                    throw new Exception($"too large size assetbundle. size {fileSize} path {assetBundleRelativePath}");
                }

                // 含まれるAsset取得
                var assetBundle = AssetBundle.LoadFromFile(assetBundleRelativePath);

                List<string> assetNames =
                    assetBundle.isStreamedSceneAssetBundle
                    ? assetBundle.GetAllScenePaths().ToList()
                    : assetBundle.GetAllAssetNames().ToList();

                assetBundle.Unload(true);

                infoList.Add(new AssetBundleInfo(
                    assetBundleName,
                    hash,
                    fileHash,
                    (int)fileSize,
                    dep,
                    assetNames
                ));
            }
            return new AssetBundleList(infoList);
        }
    }
}
