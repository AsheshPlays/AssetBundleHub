using AssetBundleHub;
using AssetBundleHubEditor;
using UnityEditor.Build.Pipeline.Interfaces;

namespace AssetBundleHubEditor.Interfaces
{
    /// <summary>
    /// ABHub専用のAssetBundleビルド時の設定パラメータ
    /// </summary>
    public interface IABHubBuildParameters : IBundleBuildParameters
    {
        /// <summary>
        /// AssetBundleListのファイル名
        /// </summary>
        string AssetBundleListName { get; set; }
        EncryptType EncryptType { get; set; }

        /// <summary>
        /// EncryptTypeがNone以外の場合に使用
        /// </summary>
        string CryptKeyBase { get; set; }
    }
}
