using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub
{
    public class QueueRequestDecorator : IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>
    {
        // 待ち行列
        readonly Queue<(UniTaskCompletionSource<IDownloadResponseContext>, IDownloadRequestContext, CancellationToken, Func<IDownloadRequestContext, CancellationToken, UniTask<IDownloadResponseContext>>)> q = new Queue<(UniTaskCompletionSource<IDownloadResponseContext>, IDownloadRequestContext, CancellationToken, Func<IDownloadRequestContext, CancellationToken, UniTask<IDownloadResponseContext>>)>();
        int runCapacity; // 最大同時ダウンロード数
        public int RunningCount { get; private set; } = 0; // Taskのループはメインスレッドで回す想定(ロックしていないため)

        public QueueRequestDecorator(int runCapacity = 1)
        {
            if (runCapacity < 1)
            {
                throw new ArgumentException("runCapacityは1以上");
            }

            this.runCapacity = runCapacity;
        }

        public async UniTask<IDownloadResponseContext> DownloadAsync(IDownloadRequestContext context, CancellationToken cancellationToken, Func<IDownloadRequestContext, CancellationToken, UniTask<IDownloadResponseContext>> next)
        {
            if (RunningCount < runCapacity)
            {
                // capacityに余裕があれば即実行
                RunningCount++;
                IDownloadResponseContext response = null;
                try
                {
                    response = await next(context, cancellationToken);
                }
                finally
                {
                    RunningCount--;
                    // 処理後に待ちがあれば待ち行列を処理するタスクを起動
                    // NOTE: レジで行列があればそのまま次の会計をする。なければ裏に帰っていくイメージ
                    if (q.Count > 0)
                    {
                        Run().Forget();
                    }
                }

                return response;
            }
            else
            {
                var completionSource = new UniTaskCompletionSource<IDownloadResponseContext>();
                q.Enqueue((completionSource, context, cancellationToken, next));
                return await completionSource.Task;
            }
        }

        async UniTaskVoid Run()
        {
            while (q.Count != 0)
            {
                var (completionSource, context, cancellationToken, next) = q.Dequeue();
                RunningCount++;
                try
                {
                    var response = await next(context, cancellationToken);
                    completionSource.TrySetResult(response);
                }
                catch (Exception ex)
                {
                    completionSource.TrySetException(ex);
                }
                finally
                {
                    RunningCount--;
                }
            }
        }
    }
}
