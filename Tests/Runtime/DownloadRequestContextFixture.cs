using System;
using AssetBundleHub;

namespace AssetBundleHubTests
{
    public class DownloadRequestContextFixture
    {
        public class DownloadRequestContext : IDownloadRequestContext
        {
            public string URL => throw new NotImplementedException();

            public string SavePath => throw new NotImplementedException();

            public TimeSpan Timeout => throw new NotImplementedException();

            public IProgress<float> Progress => throw new NotImplementedException();

            public IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext> GetNextDecorator()
            {
                throw new NotImplementedException();
            }
        }

        public static DownloadRequestContext Load()
        {
            return new DownloadRequestContext();
        }
    }
}
