using System.Collections;
using System.Collections.Generic;
using AssetBundleHub;
using AssetBundleHubEditor.Interfaces;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Player;
using UnityEngine;

namespace AssetBundleHubEditor
{
    /// <summary>
    /// AssetBundleのビルドに必要なパラメータを全てここにまとめる。
    /// ここからContextObjectを分解する。
    /// </summary>
    public class ABHubBuildParameters : BundleBuildParameters, IABHubBuildParameters
    {
        public string AssetBundleListName { get; set; } = "AssetBundleList.json";
        public EncryptType EncryptType { get; set; } = EncryptType.None;
        public string CryptKeyBase { get; set; }
        public bool ExtractBuiltinShader { get; set; } = true;
        public IFileHashGenerator FileHashGenerator { get; set; }

        public ABHubBuildParameters(BuildTarget target, BuildTargetGroup group, string outputFolder)
            : base(target, group, outputFolder)
        {
            AppendHash = false;
        }

        public void SetDefaultParamsIfNeeded()
        {
            if (FileHashGenerator == null)
            {
                FileHashGenerator = new MD5FileHashGenerator();
            }
        }
    }
}
