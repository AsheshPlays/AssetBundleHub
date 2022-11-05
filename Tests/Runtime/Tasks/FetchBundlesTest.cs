using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AssetBundleHub;
using AssetBundleHub.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AssetBundleHubTests
{
    public class FetchBundlesTest
    {
        [UnityTest]
        public IEnumerator 正常系_Run() => UniTask.ToCoroutine(async () =>
        {
            var assetBundleNames = new List<string>() { "Prefabs001", "Prefabs002", "Prefabs003" };
            var describedClass = new FetchBundles();
            var context = BundlePullContextFixture.Load();
            context.AssetBundleNames = assetBundleNames;

            await describedClass.Run(context);
            Assert.That(context.downloadedAssetBundles.Count, Is.EqualTo(3));
        });

        [UnityTest]
        public IEnumerator 正常系_キャンセルしたらダウンロード止まること() => UniTask.ToCoroutine(async () =>
        {
            var assetBundleNames = new List<string>() { "Prefabs001", "Prefabs002", "Prefabs003" };
            var describedClass = new FetchBundles();
            var context = BundlePullContextFixture.Load();
            context.AssetBundleNames = assetBundleNames;
            using var cts = new CancellationTokenSource();
            UniTask.Create(async () =>
            {
                await UniTask.DelayFrame(2);
                cts.Cancel();
            }).Forget();
            await describedClass.Run(context, cts.Token);

            Assert.That(context.Error, Is.Null);
            Assert.That(context.downloadedAssetBundles.Count, Is.EqualTo(0));
        });

        public class MockErrorDownload : IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>
        {

            int count = 0;
            int throwCount;

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
                    throw new Exception("test error: throw count");
                }
                await UniTask.DelayFrame(10);
                return new DownloadResponseContext();
            }
        }

        [UnityTest]
        public IEnumerator 異常系_なんらかのエラーが起こったときに後続のダウンロード処理が中断されること() => UniTask.ToCoroutine(async () =>
        {
            var assetBundleNames = new List<string>() { "Prefabs001", "Prefabs002", "Prefabs003" };
            var describedClass = new FetchBundles();

            var context = BundlePullContextFixture.Load(
                new QueueRequestDecorator(runCapacity: 1),
                new MockErrorDownload(throwCount: 2)
            );

            context.AssetBundleNames = assetBundleNames;

            await describedClass.Run(context);
            Assert.That(context.Error.Message, Is.EqualTo("test error: throw count"));
            Assert.That(context.downloadedAssetBundles.Count, Is.EqualTo(1));
        });
    }
}
