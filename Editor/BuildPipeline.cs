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
using AssetBundleHubEditor.Interfaces;

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

        /// <summary>
        /// AssetBundleをビルドするメソッドのサンプル
        /// </summary>
        /// <param name="buildMap">key: assetBundle名 value: assetのプロジェクトの相対パス</param>
        /// <returns></returns>
        public static void BuildAssetBundles(ABHubBuildParameters buildParameters, Dictionary<string, List<string>> buildMap, params IContextObject[] contextObjects)
        {
            foreach (var kvp in buildMap)
            {
                if (string.IsNullOrEmpty(kvp.Key))
                {
                    throw new ArgumentException("AssetBundleName is null or empty");
                }
            }
            buildParameters.SetDefaultParamsIfNeeded();

            var builds = CreateAssetBundleBuilds(buildMap);
            if (builds.Length == 0)
            {
                Debug.Log("AssetBundleBuild Count 0");
                return;
            }

            var buildContent = new BundleBuildContent(builds);
            var tasks = CreateTaskList(buildParameters);
            var additionalContexts = new IContextObject[contextObjects.Length + 1];
            Array.Copy(contextObjects, additionalContexts, contextObjects.Length);
            additionalContexts[additionalContexts.Length - 1] = buildParameters as IABHubBuildParameters;
            ReturnCode exitCode = ContentPipeline.BuildAssetBundles(buildParameters, buildContent, out IBundleBuildResults results, tasks, additionalContexts);

            Debug.Log($"BuildAssetBundles finished exitCode : {exitCode}");
        }

        static AssetBundleBuild[] CreateAssetBundleBuilds(Dictionary<string, List<string>> buildMap)
        {
            // AssetBundle名とそれに含めるアセットを指定する
            // ~~Resources/以下がアドレスになる
            var regex = new Regex(".*Resources/", RegexOptions.Compiled);
            // AssetBundle名とそれに含めるアセットを指定する
            return buildMap.Select(x =>
            {
                var assetNames = new string[x.Value.Count];
                var addressableNames = new string[x.Value.Count];
                for (int i = 0; i < x.Value.Count; i++)
                {
                    string assetPath = x.Value[i];
                    assetNames[i] = assetPath;
                    addressableNames[i] = AssetPathToAddressableName(assetPath, regex);
                }
                var build = new AssetBundleBuild();
                build.assetBundleName = x.Key;
                build.assetNames = assetNames;
                build.addressableNames = addressableNames;
                return build;
            }).ToArray();
        }

        static string AssetPathToAddressableName(string assetPath, Regex regex)
        {
            string assetPathWithoutExtention = Path.ChangeExtension(assetPath, null);
            var match = regex.Match(assetPathWithoutExtention);
            if (string.IsNullOrEmpty(match.Value))
            {
                return assetPathWithoutExtention;
            }
            return assetPathWithoutExtention.Replace(match.Value, "");
        }

        static IList<IBuildTask> CreateTaskList(ABHubBuildParameters parameters)
        {
            IList<IBuildTask> tasks = null;
            if (parameters.ExtractBuiltinShader)
            {
                tasks = DefaultBuildTasks.Create(DefaultBuildTasks.Preset.AssetBundleBuiltInShaderExtraction);
            }
            else
            {
                tasks = DefaultBuildTasks.Create(DefaultBuildTasks.Preset.AssetBundleCompatible);
            }

            tasks.Add(new CreateAssetBundleList()); // ビルド結果からAssetBundleListを生成
            return tasks;
        }
    }
}
