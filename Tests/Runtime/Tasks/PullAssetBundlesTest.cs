using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AssetBundleHub;
using AssetBundleHub.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AssetBundleHubTests
{
    public class PullAssetBundlesTest
    {
        [SetUp]
        public void SetUp()
        {
            BundlePullContextFixture.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            BundlePullContextFixture.Clear();
        }

#if ABHUB_SKIP_DL_TEST
        [Ignore("ダウンロード先に依存するので基本はスキップ")]
#endif
        [UnityTest]
        public IEnumerator 正常系_Run() => UniTask.ToCoroutine(async () =>
        {
            var describedClass = new PullAssetBundles();
            var assetBundleNames = new List<string>() { "Prefabs001", "Prefabs002", "Prefabs003" };
            var context = BundlePullContextFixture.Load(
                new QueueRequestDecorator(runCapacity: 4),
                new UnityWebRequestDownloadFile()
            );
            context.AssetBundleNames = assetBundleNames;
            await describedClass.Run(context);

            Assert.That(context.GetMergedAssetBundles().Count(), Is.EqualTo(3));
        });
    }
}
