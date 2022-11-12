using System;
using System.Threading;
using AssetBundleHub;
using Cysharp.Threading.Tasks;

public class MockErrorDownload : IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>
{
    int count = 0;
    int throwCount;
    public Exception exception;

    /// <summary>
    /// エラー出す用クラス
    /// </summary>
    /// <param name="throwCount"> 何番目のリクエストでエラーを出すか </param>
    public MockErrorDownload(int throwCount = 1)
    {
        this.throwCount = throwCount;
    }

    public async UniTask<IDownloadResponseContext> DownloadAsync(IDownloadRequestContext context, CancellationToken cancellationToken, Func<IDownloadRequestContext, CancellationToken, UniTask<IDownloadResponseContext>> next)
    {
        cancellationToken.ThrowIfCancellationRequested();
        count++;
        if (count == throwCount)
        {
            if (exception == null)
            {
                throw new Exception("test error: throw count");
            }
            else
            {
                throw exception;
            }
        }
        await UniTask.DelayFrame(10, cancellationToken: cancellationToken);
        return new DownloadResponseContext();
    }
}