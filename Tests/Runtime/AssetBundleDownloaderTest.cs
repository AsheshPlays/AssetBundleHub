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

        public class MockAssetBundleInfoStore : IDownloadAssetBundleInfoStore
        {
            public class Data
            {
                public bool existsNewRelease = false;
                public List<string> dependencies = new List<string>();
                public AssetBundleInfo assetBundleInfo = null;
            }

            public Dictionary<string, Data> dataMap = new Dictionary<string, Data>();
            // key: assetPath, value: assetBundleName
            public Dictionary<string, string> assetMap = new Dictionary<string, string>();

            public bool ExistsNewRelease(string assetBundleName)
            {
                return dataMap[assetBundleName].existsNewRelease;
            }

            public List<string> GetAllDependencies(string assetBundleName)
            {
                return dataMap[assetBundleName].dependencies;
            }

            public AssetBundleInfo GetAssetBundleInfo(string assetBundleName)
            {
                return dataMap[assetBundleName].assetBundleInfo;
            }

            public bool TryGetAssetBundleName(string assetPath, out string assetBundleName)
            {
                return assetMap.TryGetValue(assetPath, out assetBundleName);
            }
        }

        public class MockPullAssetBundles : IPullAssetBundles
        {
            public ulong bytes = 0L;

            public async UniTask PullAssetBundles(IList<string> assetBundleNames, Action<ulong> reportDownloadedBytes = null)
            {
                await UniTask.DelayFrame(1);
                reportDownloadedBytes?.Invoke(bytes);
            }
        }

        AssetBundleDownloader downloader;

        [SetUp]
        public void SetUp()
        {
            var assetBundleInfoStore = new MockAssetBundleInfoStore();
            assetBundleInfoStore.dataMap = new Dictionary<string, MockAssetBundleInfoStore.Data>()
            {
                {
                    "ab1",
                    new MockAssetBundleInfoStore.Data(){
                        existsNewRelease = true,
                        dependencies = new List<string>() { "dep1" },
                        assetBundleInfo = new AssetBundleInfo("ab1", "", "", 100, null, null)
                    }
                },
                {
                    "ab2",
                    new MockAssetBundleInfoStore.Data(){
                        existsNewRelease = true,
                        dependencies = new List<string>() { "dep1" },
                        assetBundleInfo = new AssetBundleInfo("ab2", "", "", 100, null, null)
                    }
                },
                {
                    "dep1",
                    new MockAssetBundleInfoStore.Data(){
                        existsNewRelease = true,
                        assetBundleInfo = new AssetBundleInfo("dep1", "", "", 10, null, null)
                    }
                },
            };
            assetBundleInfoStore.assetMap = new Dictionary<string, string>()
            {
                { "asset1", "ab1" },
                { "asset2", "ab2" },
            };

            var pullAssetBundles = new MockPullAssetBundles()
            {
                bytes = 210L
            };
            downloader = new AssetBundleDownloader(assetBundleInfoStore, pullAssetBundles);
        }

        [Test]
        public void 正常系_SetDownloadTarget()
        {
            var inputs = new List<string>() { "asset1", "asset2" };

            downloader.SetDownloadTarget(inputs);

            Assert.That(downloader.DownloadSize, Is.EqualTo(210L));
        }

        [UnityTest]
        public IEnumerator 正常系_DownloadAsync() => UniTask.ToCoroutine(async () =>
        {
            var inputs = new List<string>() { "asset1", "asset2" };
            downloader.SetDownloadTarget(inputs);
            await downloader.DownloadAsync();
            Assert.That(downloader.DownloadProgress, Is.EqualTo(1f));
            Assert.That(downloader.DownloadSize, Is.EqualTo(210L));
        });
    }
}
