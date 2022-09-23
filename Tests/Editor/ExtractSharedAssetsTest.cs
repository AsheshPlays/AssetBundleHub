using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using AssetBundleHubEditor;

namespace AssetBundleHubEditorTests
{
    public class ExtractSharedAssetsTest
    {
        [Test]
        public void RunTest()
        {
            var groups = new Dictionary<string, List<string>>
            {
                { "group1", new List<string>{Utils.RootDir + "Tests/Editor/Fixtures/Prefabs/1/Square1.prefab" } },
                { "group2", new List<string>{Utils.RootDir + "Tests/Editor/Fixtures/Prefabs/2/Square2.prefab" } }
            };
            var extractSharedAssets = new ExtractSharedAssets();
            var sharedAssets = extractSharedAssets.Run(groups);
            Assert.AreEqual(sharedAssets.Count, 1);
            Assert.AreEqual(sharedAssets[0], Utils.RootDir + "Tests/Editor/Fixtures/Prefabs/Dep/Square.png");   
        }
    }
}
