using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AssetBundleHub;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using NUnit.Framework;

namespace AssetBundleHubTests
{
    public class AssetBundleDownloaderTest
    {
        [SetUp]
        public void SetUp()
        {
            Utils.ClearTestDir();
            AssetBundleHubSettingsFixture.BuildInstance();
            ServiceLocator.Instance.Register<IAssetBundleListLoader>(new AssetBundleListFixture());
            ABHub.Initialize();
            ABHub.LoadAndCacheAssetBundleList();
        }

        [TearDown]
        public void TearDown()
        {
            Utils.ClearTestDir();
            ServiceLocator.Instance.Clear();
        }

        [UnityTest]
        public IEnumerator 正常系() => UniTask.ToCoroutine(async () =>
        {
            var downloader = ABHub.CreateDownloader();
            var downloadAssetNames = new List<string>()
            {
                "Prefabs/001/BaseAttackPrefab",
                "Scenes/Scene01"
            };

            ulong expectedDownloadSize =
                4287L + 6543L + 7805L + 32184L +
                7580L;

            downloader.SetDownloadTarget(downloadAssetNames);

            Assert.That(downloader.DownloadSize, Is.EqualTo(expectedDownloadSize));
            Assert.That(downloader.CalcProgress, Is.EqualTo(0));

            var result = await downloader.DownloadAsync();
            Assert.That(downloader.CalcProgress, Is.EqualTo(1.0f));
            Assert.That(result.Status, Is.EqualTo(AssetBundleDownloadResult.ReturnStatus.Success));
        });

        public class MockDownloadAsyncDecoratorsFactory : IDownloadAsyncDecoratorsFactory
        {
            Func<IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[]> createFunc;

            public MockDownloadAsyncDecoratorsFactory(Func<IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[]> createFunc)
            {
                this.createFunc = createFunc;
            }

            public IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] Create() => createFunc();
        }

        [UnityTest]
        public IEnumerator 異常系_Timeout() => UniTask.ToCoroutine(async () =>
        {
            var asyncDecoratorsFactory = new MockDownloadAsyncDecoratorsFactory(() =>
            {
                return new IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[]
                {
                    new QueueRequestDecorator(runCapacity: 1),
                    new MockErrorDownload(){ exception = new TimeoutException("timeout!") }
                };
            });

            ServiceLocator.Instance.Register<IDownloadAsyncDecoratorsFactory>(asyncDecoratorsFactory);

            var downloader = ABHub.CreateDownloader();
            var downloadAssetNames = new List<string>()
            {
                "Prefabs/001/BaseAttackPrefab",
                "Scenes/Scene01"
            };

            downloader.SetDownloadTarget(downloadAssetNames);
            var result = await downloader.DownloadAsync();
            Assert.That(result.Status, Is.EqualTo(AssetBundleDownloadResult.ReturnStatus.Timeout));
        });

        // TODO: BrokenAssetBundleのテスト
        // TODO: リトライのテスト
    }
}
