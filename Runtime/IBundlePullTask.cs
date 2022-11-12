using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AssetBundleHub.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub
{
    public interface IBundlePullTask
    {
        UniTask Run(IBundlePullContext context, CancellationToken cancellationToken = default(CancellationToken));
    }

    public interface IBundlePullTasksFactory
    {
        IList<IBundlePullTask> Create();
    }

    public class DefaultBundlePullTasks : IBundlePullTasksFactory
    {
        public IList<IBundlePullTask> Create()
        {
            return new List<IBundlePullTask>(){
                new FetchBundles(),
                new ExtractBrokenBundles(new MD5FileHashGenerator()),
                new MergeBundles(),
                new UpdateLocalAssetBundleTable()
            };
        }

        public static DefaultBundlePullTasks New() => new DefaultBundlePullTasks();
    }
}
