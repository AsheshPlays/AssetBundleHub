using System;
using System.Collections.Generic;
using System.Linq;
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

    public interface IDownloadAssetBundleInfoStore
    {
        bool TryGetAssetBundleName(string assetPath, out string assetBundleName);
        List<string> GetAllDependencies(string assetBundleName);
        bool ExistsNewRelease(string assetBundleName); // ダウンロードが必要ならtrueを返す
        AssetBundleInfo GetAssetBundleInfo(string assetBundleName);
    }

    public interface IPullAssetBundles
    {
        // TODO CancellationTokenを渡すことを検討
        UniTask PullAssetBundles(IList<string> assetBundleNames, Action<ulong> reportDownloadedBytes = null);
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
        public ulong DownloadedSize { get; private set; } = 0L; // ダウンロードされたサイズ
        public float DownloadProgress { get; private set; } = 0L; // DownloadedSize / DownloadSize
        Action<ulong> downloadedBytesHandler = null;

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
        /// <param name="assetPaths"></param>
        public void SetDownloadTarget(List<string> assetPaths)
        {
            if (State == DownloadState.Running)
            {
                throw new Exception("AssetBundleDownloader is running");
            }
            State = DownloadState.Idle;
            DownloadProgress = 0f;
            initialTartetAssetBundles = GetAssetBundleNameSet(assetPaths).Select(x => assetBundleInfoStore.GetAssetBundleInfo(x)).ToList();
            DownloadSize = SumSize(initialTartetAssetBundles);
        }

        public async UniTask<AssetBundleDownloadResult> DownloadAsync()
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
                DownloadProgress = 1.0f;
                State = DownloadState.Completed;
                return new AssetBundleDownloadResult(AssetBundleDownloadResult.ReturnStatus.Success);
            }

            try
            {
                State = DownloadState.Running;
                await repository.PullAssetBundles(latestTargetAssetBundles.Select(x => x.Name).ToList(), downloadedBytesHandler);
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

        HashSet<string> GetAssetBundleNameSet(List<string> assetPaths)
        {
            var assetBundleNameSet = new HashSet<string>();

            foreach (var assetPath in assetPaths)
            {
                if (!assetBundleInfoStore.TryGetAssetBundleName(assetPath, out var abName))
                {
                    throw new Exception($"AssetBundle not found asset {assetPath}");
                }

                assetBundleNameSet.Add(abName);

                // 依存関係のあるAssetBundleもダウンロード対象
                var allDeps = assetBundleInfoStore.GetAllDependencies(abName);
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
            downloadedBytesHandler = CreateDownloadedBytesHandler(downloadedSize);
            downloadedBytesHandler(0L);
        }

        Action<ulong> CreateDownloadedBytesHandler(ulong startDownloadedSize)
        {
            return new Action<ulong>(bytes =>
            {
                DownloadedSize = startDownloadedSize + bytes;
                DownloadProgress = Mathf.Clamp01(DownloadedSize / DownloadSize);
            });
        }
    }
}
