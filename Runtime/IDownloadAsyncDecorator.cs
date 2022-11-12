using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace AssetBundleHub
{
    public interface ILinkedDecorator<TRequest, TResponse>
    {
        IDownloadAsyncDecorator<TRequest, TResponse> GetNextDecorator();
    }

    // 個別にダウンロードする用のインタフェース
    public interface IDownloadRequestContext : ILinkedDecorator<IDownloadRequestContext, IDownloadResponseContext>
    {
        public string URL { get; }
        public string SavePath { get; }
        public TimeSpan Timeout { get; }
        IProgress<float> Progress { get; }
    }

    public class DownloadRequestContext : IDownloadRequestContext
    {
        int decoratorIndex;
        readonly IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] decorators;
        public string URL { get; }
        public string SavePath { get; }
        public TimeSpan Timeout { get; }
        public IProgress<float> Progress { get; }

        public IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext> GetNextDecorator() => decorators[++decoratorIndex];

        public DownloadRequestContext(string url, string savePath, TimeSpan timeout, IProgress<float> progress, IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] decorators)
        {
            this.decoratorIndex = -1;
            this.decorators = decorators;
            this.URL = url;
            this.SavePath = savePath;
            this.Progress = progress;
            this.Timeout = timeout;
        }

        public static DownloadRequestContext Create(string url, string savePath, TimeSpan timeout, IProgress<float> progress = null, params IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] decorators)
        {
            return new DownloadRequestContext(
                url,
                savePath,
                timeout,
                progress,
                decorators.Length != 0 ? decorators : DefaultDecorators()
            );
        }

        static IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] DefaultDecorators()
        {
            return ServiceLocator.Instance.Resolve<IDownloadAsyncDecoratorsFactory>().Create();
        }
    }

    public interface IDownloadResponseContext
    {
    }

    // ダウンロードの結果でほしいものがあればここに入れる。特に思いつくものはないが、後で追加するの大変なのでいれておく。
    public class DownloadResponseContext : IDownloadResponseContext
    {
    }

    public interface IDownloadAsyncDecorator<TRequestContext, TResponseContext>
    {
        UniTask<TResponseContext> DownloadAsync(TRequestContext context, CancellationToken cancellationToken, Func<TRequestContext, CancellationToken, UniTask<TResponseContext>> next);
    }
}