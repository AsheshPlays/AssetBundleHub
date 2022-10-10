using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AssetBundleHub
{
    public class UnityWebRequestDownloadFile : IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>
    {
        // 参考
        // https://neue.cc/2022/07/13_Cancellation.html
        public async UniTask<IDownloadResponseContext> DownloadAsync(IDownloadRequestContext context, CancellationToken cancellationToken, Func<IDownloadRequestContext, CancellationToken, UniTask<IDownloadResponseContext>> next)
        {
            using var request = UnityWebRequest.Get(context.URL);
            var handler = new DownloadHandlerFile(context.SavePath);
            handler.removeFileOnAbort = true; // エラーならダウンロード中のファイルを消す
            request.downloadHandler = handler;

            using var linkToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkToken.CancelAfterSlim(context.Timeout);

            try
            {
                await request.SendWebRequest().ToUniTask(context.Progress, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    // 引数のCancellationTokenが原因なので、それを保持したOperationCanceledExceptionとして投げる
                    throw new OperationCanceledException(ex.Message, ex, cancellationToken);
                }
                else
                {
                    // 元キャンセレーションソースがキャンセルしてなければTimeoutによるものと判定
                    throw new TimeoutException($"The request was canceled due to the configured Timeout of {context.Timeout.TotalSeconds} seconds elapsing.", ex);
                }
            }

            return new DownloadResponseContext();
        }
    }
}
