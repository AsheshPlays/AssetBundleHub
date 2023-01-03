using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;
using AssetBundleHub;
using AssetBundleHubEditor.Interfaces;
using System.Text;

namespace AssetBundleHubEditor.Tasks
{
    public class EncryptAssetBundles : IBuildTask
    {
        public int Version => 1;

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBundleBuildResults results;

        [InjectContext(ContextUsage.In)]
        IABHubBuildParameters parameters;
#pragma warning restore 649

        Dictionary<string, string> abNameToTempMap = new Dictionary<string, string>();
        Dictionary<string, string> abNameToOutputMap = new Dictionary<string, string>();
        Dictionary<string, string> tempToOutputMap = new Dictionary<string, string>(); // 最後にtempから移動する用

        public ReturnCode Run()
        {
            var assetBundleListPath = parameters.GetOutputFilePathForIdentifier(parameters.AssetBundleListName);
            var assetBundleListTempPath = string.Format("{0}/{1}", parameters.TempOutputFolder, parameters.AssetBundleListName);
            var assetBundleList = AssetBundleList.LoadFromFile(assetBundleListPath);

            SetAssetBundlePathMap(assetBundleList);
            tempToOutputMap[assetBundleListTempPath] = assetBundleListPath;

            var cryptKey = Encoding.UTF8.GetBytes(parameters.CryptKeyBase); // TODO: keyをそのまま使わず、変換したい
            EncryptBundles(cryptKey);
            UpdateAssetBundleList(assetBundleList);
            EncryptAssetBundleList(assetBundleList, assetBundleListTempPath, cryptKey);

            MoveTempToOutput();

            return ReturnCode.Success;
        }

        // 何度も呼ばないために、使うパスを取得してキャッシュ
        void SetAssetBundlePathMap(AssetBundleList assetBundleList)
        {
            foreach (var kvp in assetBundleList.Infos)
            {
                string abName = kvp.Value.Name;
                string tempPath = string.Format("{0}/{1}", parameters.TempOutputFolder, abName);
                string outputPath = parameters.GetOutputFilePathForIdentifier(abName);
                abNameToTempMap[abName] = tempPath;
                abNameToOutputMap[abName] = outputPath;
                tempToOutputMap[tempPath] = outputPath;
            }
        }

        // NOTE: 時間がかかる場合には非同期にして並列で走らせることを検討
        void EncryptBundles(byte[] keyBytes)
        {
            foreach (var kvp in abNameToTempMap)
            {
                string srcPath = abNameToOutputMap[kvp.Key];
                EncryptFile(srcPath, kvp.Value, keyBytes);
            }
        }

        void EncryptFile(string srcPath, string destPath, byte[] keyBytes)
        {
            using var fileReader = new FileStream(srcPath, FileMode.Open);
            using var fileWriter = new FileStream(destPath, FileMode.Create);
            using var cs = new XORCryptStream(fileWriter, keyBytes);
            fileReader.CopyTo(cs);
        }

        /// <summary>
        /// AssetBundleを暗号化するとFileHashが変わるので更新する必要がある
        /// </summary>
        void UpdateAssetBundleList(AssetBundleList assetBundleList)
        {
            foreach (var kvp in assetBundleList.Infos)
            {
                var abInfo = kvp.Value;
                var tempABPath = abNameToTempMap[abInfo.Name];
                abInfo.SetFileHash(parameters.FileHashGenerator.ComputeHash(tempABPath));
            }
        }

        void EncryptAssetBundleList(AssetBundleList assetBundleList, string destPath, byte[] keyBytes)
        {
            var assetBundleListBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(assetBundleList));
            WriteFileWithEncryption(assetBundleListBytes, destPath, keyBytes);
        }

        void WriteFileWithEncryption(byte[] content, string destPath, byte[] keyBytes)
        {
            using var ms = new MemoryStream(content);
            using var fileWriter = new FileStream(destPath, FileMode.Create);
            using var cs = new XORCryptStream(fileWriter, keyBytes);
            ms.CopyTo(cs);
        }

        void MoveTempToOutput()
        {
            foreach (var kvp in tempToOutputMap)
            {
                File.Delete(kvp.Value);
                File.Move(kvp.Key, kvp.Value);
            }
        }
    }
}
