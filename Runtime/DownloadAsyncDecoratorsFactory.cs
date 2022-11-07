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
                new QueueRequestDecorator(runCapacity: 4),
                new UnityWebRequestDownloadFile()
            };
        }

        public static DownloadAsyncDecoratorsFactory New() => new DownloadAsyncDecoratorsFactory();
    }
}
