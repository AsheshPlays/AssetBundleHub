using System;
using System.Collections;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub.Tasks
{
    /// <summary>
    /// TempからDestにAssetBundleを移動させる
    /// </summary>
    public class MergeBundles
    {
        public UniTask Run(IBundlePullContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var assetBundleName in context.GetTempAssetBundles())
            {
                string srcPath = context.GetTempSavePath(assetBundleName);
                string destPath = context.GetDestPath(assetBundleName);
                string destDir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                File.Delete(destPath);
                File.Move(srcPath, destPath);
                context.SetMergedAssetBundle(assetBundleName);
            }
            return UniTask.CompletedTask;
        }
    }
}
