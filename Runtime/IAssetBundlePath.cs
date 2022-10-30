using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleHub
{
    /// <summary>
    /// AssetBundleやAssetBundleListは暗号化することもあるので、パス取得インタフェースが必要
    /// </summary>
    public interface IAssetBundlePath
    {
        string GetAssetBundleListPath();
    }
}
