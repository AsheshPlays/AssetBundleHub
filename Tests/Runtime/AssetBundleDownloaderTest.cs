using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AssetBundleHub;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using NUnit.Framework;
using System.Linq;
using System.IO;
using AssetBundleHub.Tasks;

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
            Assert.That(downloader.CalcProgress(), Is.EqualTo(0));

            var result = await downloader.DownloadAsync();
            Assert.That(downloader.CalcProgress(), Is.EqualTo(1.0f));
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
        public IEnumerator 準正常系_Timeout() => UniTask.ToCoroutine(async () =>
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

        // ダウンロードしたAssetBundleを故意に破損させる
        public class BreakDownloadedAssetBundle : IBundlePullTask
        {
            public async UniTask Run(IBundlePullContext context, CancellationToken cancellationToken = default(CancellationToken))
            {
                string targetAssetBundle = context.GetTempAssetBundles().Last();
                string assetBundlePath = context.GetTempSavePath(targetAssetBundle);
                await File.AppendAllTextAsync(assetBundlePath, "hoge", System.Text.Encoding.UTF8, cancellationToken);
            }
        }

        public class ThrowBrokenAssetBundleTasks : IBundlePullTasksFactory
        {
            public IList<IBundlePullTask> Create()
            {
                return new List<IBundlePullTask>(){
                    new FetchBundles(),
                    new BreakDownloadedAssetBundle(),
                    new ExtractBrokenBundles(new MD5FileHashGenerator()),
                    new MergeBundles(),
                    new UpdateLocalAssetBundleTable()
                };
            }
        }

        [UnityTest]
        public IEnumerator 準正常系_BrokenAssetBundle() => UniTask.ToCoroutine(async () =>
        {
            ServiceLocator.Instance.Register<IBundlePullTasksFactory>(new ThrowBrokenAssetBundleTasks());

            var downloader = ABHub.CreateDownloader();
            var downloadAssetNames = new List<string>()
            {
                "Prefabs/001/BaseAttackPrefab",
                "Scenes/Scene01"
            };

            downloader.SetDownloadTarget(downloadAssetNames);
            var result = await downloader.DownloadAsync();
            Assert.That(result.Status, Is.EqualTo(AssetBundleDownloadResult.ReturnStatus.AssetBundleBroken));
        });

        [UnityTest]
        public IEnumerator リトライ() => UniTask.ToCoroutine(async () =>
        {
            AssetBundleHubSettings.Instance.parallelCount = 1;
            AssetBundleHubSettings.Instance.shuffle = false; // 破損させるAssetBundleを固定
            // before ダウンロードを失敗させる
            ServiceLocator.Instance.Register<IBundlePullTasksFactory>(new ThrowBrokenAssetBundleTasks());

            var downloader = ABHub.CreateDownloader();
            var downloadAssetNames = new List<string>()
            {
                "Prefabs/001/BaseAttackPrefab",
                "Scenes/Scene01"
            };

            downloader.SetDownloadTarget(downloadAssetNames);
            var result = await downloader.DownloadAsync();
            Assert.That(result.Status, Is.EqualTo(AssetBundleDownloadResult.ReturnStatus.AssetBundleBroken));

            // Mockを消すことでリトライは成功させる
            ServiceLocator.Instance.UnRegister<IBundlePullTasksFactory>();

            Assert.That(downloader.CalcProgress(), Is.EqualTo(1.0f));
            // NOTE: 実際にはPrepareDownloadを事前に呼ぶ必要はないが本テストではProgress取得のために呼ぶ。
            downloader.PrepareDownload();
            int percentage = (int)(downloader.CalcProgress() * 100);
            // ulong expectedDownloadSize =
            //     4287L + 6543L + 7805L + 32184L +
            //     7580L; <- 破損分
            // => 合計 58399
            // ダウンロード成功分 50819
            // 50819 / 58399 = 0.870203257f
            Assert.That(percentage, Is.EqualTo(87));
            result = await downloader.DownloadAsync();
            Assert.That(downloader.CalcProgress(), Is.EqualTo(1.0f));
            Assert.That(result.Status, Is.EqualTo(AssetBundleDownloadResult.ReturnStatus.Success));
        });
    }
}
