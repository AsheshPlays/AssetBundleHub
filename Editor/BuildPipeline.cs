using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using AssetBundleHub;
using AssetBundleHubEditor.Tasks;

namespace AssetBundleHubEditor
{
    // AssetBundleビルドのサンプル
    // 別のビルド処理を行いたい場合には本クラスを参考に自分用のビルドスクリプトを実装すると良さそう。
    public class BuildPipeline
    {
        public static void ExportBuildMap(string loadPath = "Assets/AssetBundleResources")
        {
            var buildMap = new BuildMapFactory().Create(loadPath, false);
            var exporter = new BuildMapExporter();
            exporter.Export(buildMap);
            Debug.Log($"export buildMap {exporter.ExportPath}");
        }

        public static void BuildAssetBundlesStandaloneOSX(string outputFolder = "AssetBundles/StandaloneOSX", string loadPath = "Assets/AssetBundleResources")
        {
            var buildTarget = BuildTarget.StandaloneOSX;
            var buildTargetGroup = BuildTargetGroup.Standalone;
            var buildParameters = new BundleBuildParameters(buildTarget, buildTargetGroup, outputFolder);
            buildParameters.UseCache = false;
            buildParameters.BundleCompression = BuildCompression.LZ4;
            buildParameters.AppendHash = false; // ファイル名にHashを加えるのはAssetBundleの機能ではなくAssetBundleHub側で行う。

            var buildMap = new BuildMapFactory().Create(loadPath, false);
            BuildAssetBundles(buildParameters, buildMap);
        }

        /// <summary>
        /// AssetBundleをビルドするメソッドのサンプル
        /// </summary>
        /// <param name="buildMap">key: assetBundle名 value: assetのプロジェクトの相対パス</param>
        /// <returns></returns>
        public static void BuildAssetBundles(BundleBuildParameters buildParameters, Dictionary<string, List<string>> buildMap, bool extractBuiltinShader = true)
        {
            foreach (var kvp in buildMap)
            {
                if (string.IsNullOrEmpty(kvp.Key))
                {
                    throw new ArgumentException("AssetBundleName is null or empty");
                }
            }

            // AssetBundle名とそれに含めるアセットを指定する
            // ~~Resources/以下がアドレスになる
            var regex = new Regex(".*Resources/", RegexOptions.Compiled);
            // AssetBundle名とそれに含めるアセットを指定する
            var builds = buildMap.Select(x =>
            {
                var build = new AssetBundleBuild();
                build.assetBundleName = x.Key;
                var assetNames = new string[x.Value.Count];
                var addressableNames = new string[x.Value.Count];
                for (int i = 0; i < x.Value.Count; i++)
                {
                    string assetPath = x.Value[i];
                    assetNames[i] = assetPath;
                    string addressableName = Path.ChangeExtension(assetPath, null); // 拡張子消す
                    var match = regex.Match(addressableName.ToString());
                    if (!string.IsNullOrEmpty(match.Value))
                    {
                        addressableName = addressableName.Replace(match.Value, "");
                    }
                    addressableNames[i] = addressableName;
                }
                build.assetNames = assetNames;
                build.addressableNames = addressableNames;
                return build;
            }).ToArray();

            if (builds.Length == 0)
            {
                Debug.Log("AssetBundleBuild Count 0");
                return;
            }

            var buildContent = new BundleBuildContent(builds);

            IBundleBuildResults results; // 依存関係とかはここからとってこれる
            IList<IBuildTask> tasks = null;
            if (extractBuiltinShader)
            {
                tasks = DefaultBuildTasks.Create(DefaultBuildTasks.Preset.AssetBundleBuiltInShaderExtraction);
            }
            else
            {
                tasks = DefaultBuildTasks.Create(DefaultBuildTasks.Preset.AssetBundleCompatible);
            }

            tasks.Add(new CreateAssetBundleList(new MD5FileHashGenerator())); // ビルド結果からAssetBundleListを生成
            ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParameters, buildContent, out results, tasks);

            Debug.Log($"BuildAssetBundles finished exitCode : {exitCode}");
        }
    }
}
