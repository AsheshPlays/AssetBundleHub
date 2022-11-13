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
    public class LoadSystemTest
    {
        bool initialized = false;
        async UniTask OneTimeSetUpAsyncIfNeeded()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            Utils.ClearTestDir();
            AssetBundleHubSettingsFixture.BuildInstance();
            ABHub.Initialize();
            // 時間あまりかからないためFixtureを用意せず、ダウンロードテストで使うリソースを使い回す。
            await ABHub.DownloadAssetBundleList();
            ABHub.LoadAndCacheAssetBundleList();
            var assetNames = new List<string>() {
                "Prefabs/001/BaseAttackPrefab",
                "Prefabs/002/BaseHPPrefab",
                "Scenes/Scene01"
            };
            var downloader = ABHub.CreateDownloader();
            downloader.SetDownloadTarget(assetNames);
            await downloader.DownloadAsync();
        }

        // クラス内の最後のテストが実行された後に一度だけ実行される
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Resources.UnloadUnusedAssets();
            Utils.ClearTestDir();
        }

        [UnityTest]
        public IEnumerator 正常系_Assetのロード() => UniTask.ToCoroutine(async () =>
        {
            await OneTimeSetUpAsyncIfNeeded();
            using var assetContainer = ABHub.CreateLoadContainer();
            var reader = ABHub.CreateReader();
            var assetNames = new List<string>() {
                "Prefabs/001/BaseAttackPrefab",
                "Prefabs/002/BaseHPPrefab"
            };

            using var cts = new CancellationTokenSource();
            try
            {
                await assetContainer.LoadAllAsync(assetNames, cts.Token);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.Log(ex.Message);

                throw;
            }

            var prefab1 = ABHub.GetAsset<GameObject>("Prefabs/001/BaseAttackPrefab");
            Assert.That(prefab1.name, Is.EqualTo("BaseAttackPrefab"));
            var abRefs = reader.localRepository.GetCurrentAssetBundleRefs();
            Assert.That(abRefs.Count, Is.EqualTo(5), "依存先がダウンロードされる");
            Assert.That(abRefs["Prefabs002"].Count, Is.EqualTo(1), "対象の参照カウント");
            Assert.That(abRefs["PrefabsDep"].Count, Is.EqualTo(2), "依存先の参照カウント");
            assetContainer.Dispose();
            abRefs = reader.localRepository.GetCurrentAssetBundleRefs();
            Assert.That(abRefs.Count, Is.EqualTo(0), "解放される");
        });
    }
}
