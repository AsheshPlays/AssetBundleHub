using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleHub
{
    public interface IAssetBundleListLoader
    {
        AssetBundleList Load(string path);
    }

    // AssetBundleListが暗号化されていない場合に使用するLoader
    public class AssetBundleListLoader : IAssetBundleListLoader
    {
        public AssetBundleList Load(string path) => AssetBundleList.LoadFromFile(path);

        public static AssetBundleListLoader Create() => new AssetBundleListLoader();
    }
}
