using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub
{
    /// <summary>
    /// Assetのロードと解放をグループ化するための入れ物
    /// まとめてロード、まとめて解放する。
    /// </summary>
    public class AssetContainer : IDisposable
    {
        readonly ABAssetRepository assetRepository;
        bool isDisposed = false;

        HashSet<string> requestedAssets = new HashSet<string>();
        public bool ignoreRefCount = false;

        public AssetContainer(ABAssetRepository assetRepository)
        {
            this.assetRepository = assetRepository;
        }

        /// <summary>
        /// 対象のAssetをロード
        /// </summary>
        /// <param name="assetNames">ビルド時に設定したaddressableNames</param>
        /// <param name="cancellationToken">キャンセルしても参照カウントはするのでUnload呼ぶ必要あり</param>
        public async UniTask LoadAllAsync(List<string> assetNames, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("AssetContainer already disposed");
            }

            var tasks = new List<UniTask<UnityEngine.Object>>();
            foreach (var assetName in assetNames)
            {
                if (requestedAssets.Add(assetName))
                {
                    tasks.Add(assetRepository.LoadAsync<UnityEngine.Object>(assetName, cancellationToken));
                }
            }
            await UniTask.WhenAll(tasks);
        }

        /// <summary>
        /// 全てのリクエストしたAssetを解放する。
        /// ロード中だと解放がスキップされるので注意
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            foreach (var assetName in requestedAssets)
            {
                assetRepository.Unload(assetName, ignoreRefCount);
            }
            isDisposed = true;
        }
    }
}
