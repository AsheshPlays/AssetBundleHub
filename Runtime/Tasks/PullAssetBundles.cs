using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub.Tasks
{
    public class PullAssetBundles : IBundlePullTask
    {
        public async UniTask Run(IBundlePullContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bundlePullTasks = ServiceLocator.Instance.Resolve<IBundlePullTasksFactory>();
            foreach (var task in bundlePullTasks.Create())
            {
                await task.Run(context, cancellationToken);
            }
        }
    }
}
