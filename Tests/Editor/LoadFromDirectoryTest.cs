using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using AssetBundleHubEditor;

namespace AssetBundleHubEditorTests
{
    public class LoadFromDirectoryTest
    {
        [Test]
        public void RunTest()
        {
            var targetDir = Utils.RootDir + "Tests/Editor/Fixtures/Sprites";
            var loadFromDirectory = new LoadFromDirectory();
            var paths = loadFromDirectory.Run(targetDir);
            Assert.AreEqual(paths.Count, 2);
            Assert.AreEqual(paths[0], Utils.RootDir + "Tests/Editor/Fixtures/Sprites/SpriteA.prefab");
            Assert.AreEqual(paths[1], Utils.RootDir + "Tests/Editor/Fixtures/Sprites/Sub/SpriteB.prefab");
        }
    }
}
