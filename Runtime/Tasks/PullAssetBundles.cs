using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub.Tasks
{
    public class PullAssetBundles
    {
        public async UniTask Run(IBundlePullContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Defaultの流れ TODO: インタフェース切って外から入れる。
            var fetchBundles = new FetchBundles();
            var extractBrokenBundles = new ExtractBrokenBundles(new MD5FileHashGenerator());
            var mergeBundles = new MergeBundles();
            var updateLocalAssetBundleTable = new UpdateLocalAssetBundleTable();
            var localAssetBundleTable = ServiceLocator.Instance.Resolve<ILocalAssetBundleTable>();
            await fetchBundles.Run(context, cancellationToken);
            await extractBrokenBundles.Run(context, cancellationToken);
            await mergeBundles.Run(context, cancellationToken);
            await updateLocalAssetBundleTable.Run(context, cancellationToken);
        }
    }
}
