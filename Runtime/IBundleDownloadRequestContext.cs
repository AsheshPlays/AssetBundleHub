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

    // AssetBundleをまとめてダウンロードする用のインタフェース
    public interface IBundleDownloadRequestContext : ILinkedDecorator<IBundleDownloadRequestContext, IBundleDownloadResponseContext>
    {
        bool Shuffle { get; } // ダウンロード順をシャッフルするかどうか
        List<IDownloadRequestContext> CreatePerRequests(IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] decorators);
    }

    /// <summary>
    /// リクエストのデータをまとめたもの
    /// </summary>
    // public class DownloadRequest
    // {
    //     public string URL { get; }
    //     public string SavePath { get; }
    //     public TimeSpan Timeout { get; }
    //     public int RetryCount { get; }
    //     public TimeSpan RetryDelay { get; }
    //     public IProgress<float> Progress { get; }

    //     public DownloadRequest(string url, string savePath, TimeSpan timeout, int retryCount = 3, IProgress<float> progress = null)
    //     {
    //         URL = url;
    //         SavePath = savePath;
    //         Timeout = timeout;
    //         RetryCount = retryCount;
    //         RetryDelay = TimeSpan.FromSeconds(1f); // default TODO 外だし
    //         Progress = progress;
    //     }
    // }

    // public class BundleDownloadRequestContext : IBundleDownloadRequestContext
    // {
    //     readonly IAssetBundleDownloadParameter downloadParameter;
    //     readonly AssetBundleList assetBundleList;
    //     int decoratorIndex;
    //     readonly IBundleDownloadAsyncDecorator[] decorators;

    //     public bool Shuffle { get; }

    //     public IDownloadAsyncDecorator<IBundleDownloadRequestContext, IBundleDownloadResponseContext> GetNextDecorator() => decorators[++decoratorIndex];

    //     public BundleDownloadRequestContext(IAssetBundleDownloadParameter downloadParameter, AssetBundleList assetBundleList, IBundleDownloadAsyncDecorator[] decorators, bool shuffle = true)
    //     {
    //         this.downloadParameter = downloadParameter;
    //         this.assetBundleList = assetBundleList;
    //         this.decoratorIndex = -1;
    //         this.decorators = decorators;
    //         Shuffle = shuffle;
    //     }

    //     public List<IDownloadRequestContext> CreatePerRequests(IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] decorators)
    //     {
    //         var downloadRequests = new List<IDownloadRequestContext>();
    //         // foreach (var assetBundleName in downloadParameter.AssetBundleNames)
    //         // {
    //         //     if (!assetBundleList.Infos.TryGetValue(assetBundleName, out AssetBundleInfo abInfo)){
    //         //         throw new Exception($"AssetBundleInfo not found {assetBundleName}");
    //         //     }
    //         //     // TODO: DownloadSizeとかprogressとかを格納
    //         //     downloadRequests.Add(new DownloadRequestContext(
    //         //         downloadParameter.GetURL(assetBundleName),
    //         //         downloadParameter.GetTempSavePath(assetBundleName),
    //         //         TimeSpan.FromSeconds(10f), // TODO
    //         //         decorators
    //         //     ));
    //         // }
    //         return downloadRequests;
    //     }
    // }


    public interface IBundleDownloadResponseContext
    {
    }

    // ダウンロードの結果でほしいものがあればここに入れる。特に思いつくものはないが、後で追加するの大変なのでいれておく。
    public class BundleDownloadResponseContext : IBundleDownloadResponseContext
    {
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
            return new IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[]
            {
                new QueueRequestDecorator(runCapacity: 4),
                new UnityWebRequestDownloadFile()
            };
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

    // インタフェースを短く書くために作成
    public interface IBundleDownloadAsyncDecorator : IDownloadAsyncDecorator<IBundleDownloadRequestContext, IBundleDownloadResponseContext> { }

    public interface IBundleDownloadTasks
    {
        // 最後にPullAssetBundlesが挿入される
        IBundleDownloadAsyncDecorator[] BundleDownloadFilters { get; }
        // ダウンロードする本体も含める
        IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] DownloadPerFileDecorators { get; }
    }

    internal class DownloadPipeline<TRequest, TResponse> where TRequest : ILinkedDecorator<TRequest, TResponse>
    {
        readonly Func<TRequest, CancellationToken, UniTask<TResponse>> next;

        public DownloadPipeline()
        {
            this.next = InvokeRecursive;
        }

        public UniTask<TResponse> InvokeRecursive(TRequest context, CancellationToken cancellationToken)
        {
            return context.GetNextDecorator().DownloadAsync(context, cancellationToken, next);
        }
    }
}