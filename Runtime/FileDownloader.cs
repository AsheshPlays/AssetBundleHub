using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub
{
    public interface IFileDownloader
    {
        UniTask<IDownloadResponseContext> Run(IDownloadRequestContext context, CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// AssetBundleListのダウンロード用
    /// AssetBundleのダウンロードには使用しない。
    /// 差し替えOK
    /// </summary>
    public class FileDownloader : IFileDownloader
    {
        readonly Func<IDownloadRequestContext, CancellationToken, UniTask<IDownloadResponseContext>> next;

        public FileDownloader()
        {
            this.next = InvokeRecursive;
        }

        public async UniTask<IDownloadResponseContext> Run(IDownloadRequestContext context, CancellationToken cancellationToken = default)
        {
            return await InvokeRecursive(context, cancellationToken);
        }

        UniTask<IDownloadResponseContext> InvokeRecursive(IDownloadRequestContext context, CancellationToken cancellationToken)
        {
            return context.GetNextDecorator().DownloadAsync(context, cancellationToken, next);
        }
    }
}
