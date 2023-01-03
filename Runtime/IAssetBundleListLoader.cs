using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        public static AssetBundleListLoader New() => new AssetBundleListLoader();
    }

    public class XORAssetBundleListLoader : IAssetBundleListLoader
    {
        byte[] keyBytes;

        public XORAssetBundleListLoader(byte[] keyBytes)
        {
            this.keyBytes = keyBytes;
        }

        public AssetBundleList Load(string path)
        {
            AssetBundleList assetBundleList = null;
            using (var fs = new FileStream(path, FileMode.Open))
            using (var cs = new XORCryptStream(fs, keyBytes))
            {
                var sr = new StreamReader(cs, Encoding.UTF8);
                assetBundleList = JsonUtility.FromJson<AssetBundleList>(sr.ReadToEnd());
            }
            return assetBundleList;
        }
    }
}
