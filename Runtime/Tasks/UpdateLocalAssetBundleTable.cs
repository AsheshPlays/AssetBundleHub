using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub
{
    public class UpdateLocalAssetBundleTable
    {
        public UniTask Run(IBundlePullContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = new Dictionary<string, string>();
            foreach (var assetBundleName in context.GetMergedAssetBundles())
            {
                var abInfo = context.AssetBundleList.Infos[assetBundleName];
                values[assetBundleName] = abInfo.Hash;
            }
            var localAssetBundleTable = ServiceLocator.Instance.Resolve<ILocalAssetBundleTable>();
            localAssetBundleTable.BulkSet(values);
            return UniTask.CompletedTask;
        }
    }
}
