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
}
