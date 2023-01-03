using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub
{
    /// <summary>
    /// Fileから非同期でAssetBundleを読むクラス
    /// Readerという名前にすることで「Fileを触るクラス感」を出す。
    /// </summary>
    public interface IAssetBundleReader
    {
        UniTask<AssetBundle> LoadFromFileAsync(string path, CancellationToken cancellationToken = default);
    }

    public static class DefaultAssetBundleReader
    {
        // NOTE: 暗号化機能作ったらこの中で分岐する想定
        public static IAssetBundleReader New() => new AssetBundleReader();
    }

    /// <summary>
    /// 暗号化されていないAssetBundleのLoader
    /// </summary>
    public class AssetBundleReader : IAssetBundleReader
    {
        public async UniTask<AssetBundle> LoadFromFileAsync(string path, CancellationToken cancellationToken = default)
        {
            return await AssetBundle.LoadFromFileAsync(path, 0).ToUniTask(cancellationToken: cancellationToken);
        }
    }

    public class XORAssetBundleReader : IAssetBundleReader
    {
        byte[] keyBytes;

        public XORAssetBundleReader(byte[] keyBytes)
        {
            this.keyBytes = keyBytes;
        }

        public async UniTask<AssetBundle> LoadFromFileAsync(string path, CancellationToken cancellationToken = default)
        {
            AssetBundle assetBundle = null;
            using (var fs = new FileStream(path, FileMode.Open))
            using (var cs = new XORCryptStream(fs, keyBytes))
            {
                assetBundle = await AssetBundle.LoadFromStreamAsync(cs, 0).ToUniTask(cancellationToken: cancellationToken);
            }
            return assetBundle;
        }
    }
}
