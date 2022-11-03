using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssetBundleHubTests
{
    public static class Utils
    {
        public static readonly string testDir = "Test/ABHub";
        public static void ClearTestDir()
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }
}
