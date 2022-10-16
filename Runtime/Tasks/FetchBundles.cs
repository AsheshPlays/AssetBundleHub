using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub.Tasks
{
    /// <summary>
    /// AssetBundleをダウンロードしてTempに保存
    /// </summary>
    public class FetchBundles
    {
        readonly Func<IDownloadRequestContext, CancellationToken, UniTask<IDownloadResponseContext>> next;

        public FetchBundles()
        {
            this.next = InvokeRecursive;
        }

        /// <summary>
        /// ダウンロード処理を一通り行って、ダウンロードできたとこまでを結果としてcontextに記録
        /// エラーハンドリングは外ででやると後続のタスクが実行されなくなるのでここでハンドリング。
        /// </summary>
        public async UniTask Run(IBundlePullContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var requests = CreateDownloadRequests(context);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var tasks = DownloadFiles(requests, context.Shuffle, cts.Token);
            foreach (var task in tasks)
            {
                try
                {
                    int requestIndex = await task;
                    context.SetDownloadedAssetBundle(context.AssetBundleNames[requestIndex]);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == cts.Token) { } // キャンセルされたら上流に伝搬せず握りつぶす。
                catch (Exception ex) // TODO: ネットワークエラー以外は素通ししたい
                {
                    context.Error = ex;
                    // ネットワークエラーまたはタイムアウトが発生したら後続のダウンロードは止める
                    // このCancelでエラーが発生した場合は想定外なのでそのまま上流に伝搬
                    // 後続のダウンロードの止め方これで正しいのか自信がないため意見がほしいところ。
                    cts.Cancel();
                }
            }
        }

        List<IDownloadRequestContext> CreateDownloadRequests(IBundlePullContext context)
        {
            var downloadRequests = new List<IDownloadRequestContext>();
            foreach (var assetBundleName in context.AssetBundleNames)
            {
                if (!context.AssetBundleList.Infos.TryGetValue(assetBundleName, out AssetBundleInfo abInfo))
                {
                    throw new Exception($"AssetBundleInfo not found {assetBundleName}");
                }
                downloadRequests.Add(new DownloadRequestContext(
                    context.GetURL(assetBundleName),
                    context.GetTempSavePath(assetBundleName),
                    context.Timeout,
                    Progress.Create<float>(x => context.SetDownloadProgress(assetBundleName, x)),
                    context.DownloadAsyncDecorators
                ));
            }
            return downloadRequests;
        }

        List<UniTask<int>> DownloadFiles(List<IDownloadRequestContext> requests, bool shuffle, CancellationToken cancellationToken)
        {
            int[] shuffledIndexToSrcIndex = null;
            if (shuffle)
            {
                var srcIndexToShuffledIndex = Shuffle(requests);
                shuffledIndexToSrcIndex = Reverse(srcIndexToShuffledIndex);
            }

            var completionSources = CreateCompletionSources(requests.Count, cancellationToken);
            var rtn = completionSources.Select(x => x.Task).ToList();

            int finishedCount = 0;
            for (int i = 0; i < requests.Count; i++)
            {
                var request = requests[i];
                int srcRequestIndex = shuffle ? shuffledIndexToSrcIndex[i] : i;

                Func<UniTaskCompletionSource<int>> takeCompletionSource = () =>
                {
                    finishedCount++;
                    return completionSources[finishedCount - 1];
                };

                DownloadFileAsync(takeCompletionSource, srcRequestIndex, request, cancellationToken).Forget();
            }

            return rtn;
        }

        async UniTaskVoid DownloadFileAsync(Func<UniTaskCompletionSource<int>> takeCompletionSource, int srcRequestIndex, IDownloadRequestContext context, CancellationToken cancellationToken)
        {
            try
            {
                var response = await InvokeRecursive(context, cancellationToken);
                takeCompletionSource().TrySetResult(srcRequestIndex);
            }
            catch (Exception ex)
            {
                takeCompletionSource().TrySetException(ex);
            }
        }

        UniTask<IDownloadResponseContext> InvokeRecursive(IDownloadRequestContext context, CancellationToken cancellationToken)
        {
            return context.GetNextDecorator().DownloadAsync(context, cancellationToken, next);
        }

        List<UniTaskCompletionSource<int>> CreateCompletionSources(int count, CancellationToken cancellationToken)
        {
            var tasks = new List<UniTaskCompletionSource<int>>(count);
            for (int i = 0; i < count; i++)
            {
                tasks.Add(new UniTaskCompletionSource<int>());
            }
            return tasks;
        }

        /// <summary>
        /// Fisher-Yates shuffle
        /// 返り値は引数のlistのindexから変更後のindexを取得するための配列
        /// </summary>
        /// <returns> シャッフル後のIndex = result[シャッフル前のindex] </returns>
        static int[] Shuffle<T>(IList<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list is null");
            }
            var mirror = new int[list.Count]; // シャッフル記録用配列
            for (int i = 0; i < list.Count; i++)
            {
                mirror[i] = i;
            }

            var result = new int[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                int srcIndex = UnityEngine.Random.Range(i, list.Count);

                var temp = list[srcIndex]; // swap
                list[srcIndex] = list[i];
                list[i] = temp;

                int tempIndex = mirror[srcIndex]; // mirror
                mirror[srcIndex] = mirror[i];
                mirror[i] = tempIndex;

                result[tempIndex] = i; // tempIndexは確定
            }

            return result;
        }

        // indexとvalueを反転させる
        int[] Reverse(int[] src)
        {
            var dest = new int[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                dest[src[i]] = i;
            }
            return dest;
        }
    }
}
