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

        // https://github.com/teach310/AssetBundlesForDownloadTest/blob/master/SBP/AssetBundleList.json
        static string AssetBundleListJson()
        {
            return "{\"version\":1,\"assetBundleInfoList\":[{\"name\":\"Prefabs001\",\"hash\":\"0a484e8398d9fd7af35ad001e8ac514e\",\"fileHash\":\"0a484e8398d9fd7af35ad001e8ac514e\",\"size\":4008,\"directDependencies\":[\"PrefabsDep\",\"Sprites\",\"UnityBuiltInShaders.bundle\"],\"assetNames\":[\"Assets/DownloadSample/AssetBundleResources/Prefabs/001/BaseAttackPrefab.prefab\"]},{\"name\":\"Prefabs002\",\"hash\":\"3e2d5eba4f91eb98da5ab96fee4a333a\",\"fileHash\":\"3e2d5eba4f91eb98da5ab96fee4a333a\",\"size\":4007,\"directDependencies\":[\"PrefabsDep\",\"Sprites\",\"UnityBuiltInShaders.bundle\"],\"assetNames\":[\"Assets/DownloadSample/AssetBundleResources/Prefabs/002/BaseHPPrefab.prefab\"]},{\"name\":\"Prefabs003\",\"hash\":\"a520d3914b0139dca1b77c67bb9b60b7\",\"fileHash\":\"a520d3914b0139dca1b77c67bb9b60b7\",\"size\":34699,\"directDependencies\":[\"UnityBuiltInShaders.bundle\"],\"assetNames\":[\"Assets/DownloadSample/AssetBundleResources/Prefabs/003/Cube.prefab\"]},{\"name\":\"ScenesAssetBundleSample\",\"hash\":\"9c357b0ab7acff38ce5c9bb87fd452d6\",\"fileHash\":\"9c357b0ab7acff38ce5c9bb87fd452d6\",\"size\":7182,\"directDependencies\":[\"Sprites\",\"UnityBuiltInShaders.bundle\"],\"assetNames\":[\"Assets/DownloadSample/AssetBundleResources/Scenes/AssetBundleSample/AssetBundleSample.unity\"]},{\"name\":\"Sprites\",\"hash\":\"436f8245183409ec9bd700f3daaf859d\",\"fileHash\":\"436f8245183409ec9bd700f3daaf859d\",\"size\":7334,\"directDependencies\":[],\"assetNames\":[\"Assets/DownloadSample/AssetBundleResources/Sprites/Sprites.spriteatlas\",\"Assets/DownloadSample/AssetBundleResources/Sprites/base_attack.png\",\"Assets/DownloadSample/AssetBundleResources/Sprites/base_hp.png\"]},{\"name\":\"PrefabsDep\",\"hash\":\"ddaa72053ff580a39520170ce7af7468\",\"fileHash\":\"ddaa72053ff580a39520170ce7af7468\",\"size\":6363,\"directDependencies\":[],\"assetNames\":[\"Assets/DownloadSample/AssetBundleResources/Prefabs/Dep/Circle.png\"]},{\"name\":\"UnityBuiltInShaders.bundle\",\"hash\":\"f0819c2896febc6e27dceca9caa1ff85\",\"fileHash\":\"f0819c2896febc6e27dceca9caa1ff85\",\"size\":41609,\"directDependencies\":[],\"assetNames\":[]}]}";
        }
    }
}
