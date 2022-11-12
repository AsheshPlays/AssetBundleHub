using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AssetBundleHub;
using AssetBundleHub.Tasks;
using System;
using System.Threading;

namespace AssetBundleHubTests
{
    public class UnityWebRequestDownloadFileTest
    {
        string testDir = "Test/Tasks/DownloadFileTest";

        [SetUp]
        public void SetUp()
        {
            // リセット
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
            Directory.CreateDirectory(testDir);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(testDir, true);
        }

        public class MockRequestContext : IDownloadRequestContext, IProgress<float>
        {
            public string URL { get; set; }

            public string SavePath { get; set; }

            public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3f);

            public IProgress<float> Progress => this;
            public float progressValue = 0f;

            public void Report(float value)
            {
                progressValue = value;
            }

            public IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext> GetNextDecorator()
            {
                throw new NotImplementedException();
            }
        }

#if ABHUB_SKIP_DL_TEST
        [Ignore("ダウンロード先に依存するので基本はスキップ")]
#endif
        [UnityTest]
        public IEnumerator 正常系_DownloadAsync() => UniTask.ToCoroutine(async () =>
        {
            var context = new MockRequestContext()
            {
                URL = "https://teach310.github.io/AssetBundleHubSample/AssetBundles/StandaloneOSX/Prefabs001",
                SavePath = testDir + "/Prefabs001"
            };

            var described_class = new UnityWebRequestDownloadFile();
            await described_class.DownloadAsync(context, default(CancellationToken), null);
            Assert.That(context.progressValue, Is.EqualTo(1f));
        });
    }
}
