using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleHub
{
    public interface IDownloadAsyncDecoratorsFactory
    {
        IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] Create();
    }

    public class DownloadAsyncDecoratorsFactory : IDownloadAsyncDecoratorsFactory
    {
        public IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] Create()
        {
            return new IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[]
            {
                new QueueRequestDecorator(runCapacity: AssetBundleHubSettings.Instance.parallelCount),
                new UnityWebRequestDownloadFile()
            };
        }

        public static DownloadAsyncDecoratorsFactory New() => new DownloadAsyncDecoratorsFactory();
    }
}
