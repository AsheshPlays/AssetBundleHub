using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AssetBundleHub
{
    // ダウンロード失敗時のエラーをまとめる
    public class AssetBundleDownloadResult
    {
        public enum ReturnStatus
        {
            Success = 0,
            Error = -1,
            ErrorAssetBundleInfoNotFound = -3,
            ErrorDuplicatedRequest = -4, // SavePathが同じだと呼ばれる。
            HTTPException = -5,
            NetworkException = -6,
            Timeout = -7,
            AssetBundleBroken = -8,
        }

        public ReturnStatus Status { get; private set; }
        public string Message { get; private set; }

        public AssetBundleDownloadResult(ReturnStatus status, string message = "")
        {
            Status = status;
            Message = message;
        }
    }

    /// <summary>
    /// AssetBundleをダウンロードするクラス。
    /// ダウンロード進捗を状態としてもつ。
    /// リトライ可能。
    /// 複雑なクラス構成にしないためにダウンロード単位でのクラスは作らず本クラスを使い回す。
    /// </summary>
    public class AssetBundleDownloader
    {
        public enum DownloadState
        {
            Idle,
            Running,
            Completed,
            Failed
        }

        List<AssetBundleInfo> initialTartetAssetBundles; // Setした時点でのダウンロード対象
        List<AssetBundleInfo> latestTargetAssetBundles; // リトライを考慮した直近のダウンロード対象

        public DownloadState State { get; private set; } = DownloadState.Idle;
        public ulong DownloadSize { get; private set; } = 0L; // 合計DLサイズ 単位はbytes
        float startProgress = 0f; // DownloadAsyncを呼んだ時点のprogress。リトライ時にはStart時点で0以上
        IBundlePullOutputProgress pullOutputProgress;


        IDownloadAssetBundleInfoStore assetBundleInfoStore;
        IPullAssetBundles repository;

        public AssetBundleDownloader(IDownloadAssetBundleInfoStore assetBundleInfoStore, IPullAssetBundles repository)
        {
            this.assetBundleInfoStore = assetBundleInfoStore;
            this.repository = repository;
        }

        /// <summary>
        /// ダウンロード対象となるassetのパスを追加
        /// 状態がリセットされる。
        /// </summary>
        /// <param name="assetNames">ビルド時に設定したaddressableNameのリスト</param>
        public void SetDownloadTarget(List<string> assetNames)
        {
            if (State == DownloadState.Running)
            {
                throw new Exception("AssetBundleDownloader is running");
            }
            State = DownloadState.Idle;
            pullOutputProgress = null;
            initialTartetAssetBundles = GetAssetBundleNameSet(assetNames).Select(x => assetBundleInfoStore.AssetBundleList.Infos[x]).ToList();
            DownloadSize = SumSize(initialTartetAssetBundles);
        }

        public async UniTask<AssetBundleDownloadResult> DownloadAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!IsDownloadableState())
            {
                throw new Exception("AssetBundleDownloader is not downloadable state");
            }

            PrepareDownload();
            AssetBundleDownloadResult result = null;

            // 対象がなければ即返す
            if (latestTargetAssetBundles.Count == 0)
            {
                State = DownloadState.Completed;
                return new AssetBundleDownloadResult(AssetBundleDownloadResult.ReturnStatus.Success);
            }

            var context = new BundlePullContext();
            pullOutputProgress = context;
            context.AssetBundleList = assetBundleInfoStore.AssetBundleList;
            context.AssetBundleNames = latestTargetAssetBundles.Select(x => x.Name).ToList();
            try
            {
                State = DownloadState.Running;
                await repository.PullAssetBundles(context, cancellationToken);
                result = new AssetBundleDownloadResult(AssetBundleDownloadResult.ReturnStatus.Success);
                State = DownloadState.Completed;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                State = DownloadState.Failed;
                result = new AssetBundleDownloadResult(AssetBundleDownloadResult.ReturnStatus.Error, ex.Message);
            }
            return result;
        }

        HashSet<string> GetAssetBundleNameSet(List<string> assetNames)
        {
            var assetBundleNameSet = new HashSet<string>();

            foreach (var assetName in assetNames)
            {
                if (!assetBundleInfoStore.TryGetAssetBundleName(assetName, out var abName))
                {
                    throw new Exception($"AssetBundle not found asset {assetName}");
                }

                assetBundleNameSet.Add(abName);

                // 依存関係のあるAssetBundleもダウンロード対象
                var allDeps = assetBundleInfoStore.AssetBundleList.GetAllDependencies(abName);
                foreach (var dep in allDeps)
                {
                    assetBundleNameSet.Add(dep);
                }
            }

            // すでにダウンロード済みのものは除外する
            assetBundleNameSet.RemoveWhere(x => !assetBundleInfoStore.ExistsNewRelease(x));
            return assetBundleNameSet;
        }

        ulong SumSize(IEnumerable<AssetBundleInfo> abInfo)
        {
            return abInfo.Select(x => (ulong)x.Size).Aggregate((sum, size) => sum += size);
        }

        bool IsDownloadableState() => State == DownloadState.Idle || State == DownloadState.Failed;

        // ダウンロード対象と、ダウンロード済みのサイズの確認
        void PrepareDownload()
        {
            // リトライ用に、すでにダウンロード済みのABをダウンロード対象から外す
            ulong downloadedSize = 0L;
            var target = new List<AssetBundleInfo>();
            foreach (var abInfo in initialTartetAssetBundles)
            {
                if (assetBundleInfoStore.ExistsNewRelease(abInfo.Name))
                {
                    target.Add(abInfo);
                }
                else
                {
                    downloadedSize += (ulong)abInfo.Size;
                }
            }
            latestTargetAssetBundles = target;
            startProgress = Mathf.Clamp01((float)(downloadedSize / (double)DownloadSize));
        }

        // 計算量を少なくするために必要なときにだけ進捗を取得するようにする。
        public float CalcProgress()
        {
            if(pullOutputProgress == null)
            {
                return startProgress;
            }

            return Mathf.Lerp(startProgress, 1.0f, pullOutputProgress.CalcProgress());
        }
    }
}
