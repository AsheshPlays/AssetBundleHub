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
            ABHub.Initialize();
            AssetBundleHubSettingsFixture.BuildInstance();
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
                "Scenes/AssetBundleSample/AssetBundleSample"
            };

            // Downloaderを作成
            // var downloader = ABHub.CreateDownloader();

            // サイズを出力
            //

            // var describedClass = new PullAssetBundles();
            // var assetBundleNames = new List<string>() { "Prefabs001", "Prefabs002", "Prefabs003" };
            // var context = BundlePullContextFixture.Load(
            //     new QueueRequestDecorator(runCapacity: 4),
            //     new UnityWebRequestDownloadFile()
            // );
            // context.AssetBundleNames = assetBundleNames;
            // await describedClass.Run(context);

            // Assert.That(context.GetMergedAssetBundles().Count(), Is.EqualTo(3));
        });
    }
}
