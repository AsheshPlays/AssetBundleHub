using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub.Tasks
{
    /// <summary>
    /// TempのAssetBundleが破損しているかどうかを確認
    /// 破損していた場合にはそれをPullContextに報告
    /// </summary>
    public class ExtractBrokenBundles
    {
        IFileHashGenerator hashGenerator;

        public ExtractBrokenBundles(IFileHashGenerator hashGenerator)
        {
            this.hashGenerator = hashGenerator;
        }

        public UniTask Run(IBundlePullContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // ダウンロード成功したやつリストの取得が必要
            foreach (var assetBundleName in context.GetTempAssetBundles())
            {
                string assetBundlePath = context.GetTempSavePath(assetBundleName);
                if (!context.AssetBundleList.Infos.TryGetValue(assetBundleName, out AssetBundleInfo assetBundleInfo))
                {
                    throw new Exception($"AssetBundleInfo not found {assetBundleName}");
                }
                string fileHash = hashGenerator.ComputeHash(assetBundlePath);
                if (fileHash != assetBundleInfo.FileHash)
                {
                    Debug.Log($"fileHash not match want {assetBundleInfo.FileHash} got {fileHash}");
                    context.ReportBrokenAssetBundle(assetBundleName);
                }
            }
            return UniTask.CompletedTask;
        }
    }
}
