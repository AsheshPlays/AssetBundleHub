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
