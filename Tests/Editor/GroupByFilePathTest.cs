using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using AssetBundleHubEditor;

namespace AssetBundleHubEditorTests
{
    public class GroupByFilePathTest
    {
        [Test]
        public void RunTest()
        {
            var filePaths = new List<string>
            {
                "Assets/AssetControl/Tests/Editor/Fixtures/Sprites/SpriteA.prefab",
                "Assets/AssetControl/Tests/Editor/Fixtures/Sprites/SpriteB.prefab",
                "Assets/AssetControl/Tests/Editor/Fixtures/Sprites/SpriteC.prefab",
                "Assets/AssetControl/Tests/Editor/Fixtures/Sprites/SpriteD.prefab",
                "Assets/AssetControl/Tests/Editor/Fixtures/Prefabs/PrefabA.prefab",
                "Assets/AssetControl/Tests/Editor/Fixtures/Prefabs/PrefabB.prefab",
                "Assets/AssetControl/Tests/Editor/Fixtures/Prefabs/PrefabC.prefab",
            };
            var groupByFilePath = new GroupByFilePath();
            var group = groupByFilePath.Run(filePaths);
            Assert.AreEqual(group.Count, 2);
            Assert.AreEqual(group["Assets/AssetControl/Tests/Editor/Fixtures/Sprites"].Count, 4);
            Assert.AreEqual(group["Assets/AssetControl/Tests/Editor/Fixtures/Prefabs"].Count, 3);
        }
    }
}
