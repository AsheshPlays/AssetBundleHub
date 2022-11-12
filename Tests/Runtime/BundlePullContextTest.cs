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
    public class BundlePullContextTest
    {
        [Test]
        public void CalcProgress_DLサイズがint最大値でも計算できること()
        {
            AssetBundleHubSettingsFixture.BuildInstance();
            var context = new BundlePullContext();
            var assetBundleListFixture = AssetBundleListFixture.Load();
            // 全ABのサイズを最大にする
            var assetBundleList = new AssetBundleList(
                assetBundleListFixture.Infos.Values.Select(x => new AssetBundleInfo(
                    x.Name, x.Hash, x.FileHash, int.MaxValue, x.DirectDependencies, x.AssetNames
                )).ToList()
            );
            context.AssetBundleList = assetBundleList;
            context.AssetBundleNames = new List<string>() {
                "Prefabs001", "PrefabsDep", "Sprites", "UnityBuiltInShaders.bundle"
            };

            foreach (var abName in context.AssetBundleNames)
            {
                context.SetDownloadProgress(abName, 0.9f);
            }


            Assert.That(context.CalcProgress, Is.EqualTo(0.9f));
        }
    }
}
