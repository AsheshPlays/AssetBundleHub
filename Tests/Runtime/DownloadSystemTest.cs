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
    public class DownloadSystemTest
    {
        [SetUp]
        public void SetUp()
        {
            Utils.ClearTestDir();
            AssetBundleHubSettingsFixture.BuildInstance();
            ABHub.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            Utils.ClearTestDir();
        }

        // AssetBundleListのダウンロード
        [UnityTest]
        public IEnumerator 正常系_初期化シーケンス_AssetBundleListがないとき() => UniTask.ToCoroutine(async () =>
        {
            if (!ABHub.ExistsAssetBundleList())
            {
                await ABHub.DownloadAssetBundleList();
            }
            ABHub.LoadAndCacheAssetBundleList();

            // 初期DLの必要なAssetの名前を全て取得(ここはABHubには未実装)
            var downloadAssetNames = new List<string>()
            {
                "Prefabs/001/BaseAttackPrefab",
                "Scenes/Scene01"
            };

            // 依存先もダウンロード対象にする
            ulong expectedDownloadSize =
                4287L + 6543L + 7805L + 32184L +
                7580L;

            var downloader = ABHub.CreateDownloader();
            downloader.SetDownloadTarget(downloadAssetNames);
            Assert.That(downloader.DownloadSize, Is.EqualTo(expectedDownloadSize));
            Assert.That(downloader.CalcProgress, Is.EqualTo(0));
            var result = await downloader.DownloadAsync();
            Assert.That(downloader.CalcProgress, Is.EqualTo(1.0f));
            Assert.That(result.Status, Is.EqualTo(AssetBundleDownloadResult.ReturnStatus.Success));
        });
    }
}
