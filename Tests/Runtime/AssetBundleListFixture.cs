using System.Collections;
using System.Collections.Generic;
using AssetBundleHub;
using UnityEngine;

namespace AssetBundleHubTests
{
    public class AssetBundleListFixture
    {
        internal static AssetBundleList Load()
        {
            return JsonUtility.FromJson<AssetBundleList>(AssetBundleListJson());
        }

        // https://teach310.github.io/AssetBundleHubSample/AssetBundles/StandaloneOSX/AssetBundleList.json
        static string AssetBundleListJson()
        {
            return "{\"version\":1,\"assetBundleInfoList\":[{\"name\":\"Prefabs001\",\"hash\":\"7128e4e0ad1fc32d3ed67c46828e2dd6\",\"fileHash\":\"7128e4e0ad1fc32d3ed67c46828e2dd6\",\"size\":4287,\"directDependencies\":[\"PrefabsDep\",\"Sprites\",\"UnityBuiltInShaders.bundle\"],\"assetNames\":[\"Prefabs/001/BaseAttackPrefab\"]},{\"name\":\"Prefabs002\",\"hash\":\"13727f14f2ff0a60f0508d9ae58d7a60\",\"fileHash\":\"13727f14f2ff0a60f0508d9ae58d7a60\",\"size\":4288,\"directDependencies\":[\"PrefabsDep\",\"Sprites\",\"UnityBuiltInShaders.bundle\"],\"assetNames\":[\"Prefabs/002/BaseHPPrefab\"]},{\"name\":\"Prefabs003\",\"hash\":\"b6d85f765ede59527f975fe3d9fa9949\",\"fileHash\":\"b6d85f765ede59527f975fe3d9fa9949\",\"size\":35478,\"directDependencies\":[\"UnityBuiltInShaders.bundle\"],\"assetNames\":[\"Prefabs/003/Cube\"]},{\"name\":\"Scenes\",\"hash\":\"9d7871a3cdb4598d5ba670913044df10\",\"fileHash\":\"9d7871a3cdb4598d5ba670913044df10\",\"size\":7580,\"directDependencies\":[\"PrefabsDep\",\"Sprites\",\"UnityBuiltInShaders.bundle\"],\"assetNames\":[\"Scenes/Scene01\"]},{\"name\":\"Sprites\",\"hash\":\"9a896f6a51819ac7cbc26d712dc02259\",\"fileHash\":\"9a896f6a51819ac7cbc26d712dc02259\",\"size\":7805,\"directDependencies\":[],\"assetNames\":[\"Sprites/Sprites\",\"Sprites/base_attack\",\"Sprites/base_hp\"]},{\"name\":\"PrefabsDep\",\"hash\":\"b207fcddd45c43217ea75208b98b61de\",\"fileHash\":\"b207fcddd45c43217ea75208b98b61de\",\"size\":6543,\"directDependencies\":[],\"assetNames\":[\"Prefabs/Dep/Circle\"]},{\"name\":\"UnityBuiltInShaders.bundle\",\"hash\":\"9d3168cf54cef8160abd0cf1c8f603cb\",\"fileHash\":\"9d3168cf54cef8160abd0cf1c8f603cb\",\"size\":32184,\"directDependencies\":[],\"assetNames\":[]}]}";
        }
    }
}
