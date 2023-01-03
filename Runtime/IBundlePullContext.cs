using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssetBundleHub
{
    // NOTE: 実際にダウンロードしたbytesではなく、Progressから算出する。
    // 実際にダウンロードしたbytesで加算していったほうが計算は軽いが、
    // AssetBundleListのsizeと実際のダウンロードしたサイズがずれていた場合に挙動おかしくなるため。
    // (起こりうるケースに心当たりがあるわけではない)
    public interface IBundlePullOutputProgress
    {
        float CalcProgress();
    }

    // Pullの一連の流れの途中経過を全てここに入れる
    public interface IBundlePullContext : IBundlePullOutputProgress
    {
        // Input
        List<string> AssetBundleNames { get; }
        AssetBundleList AssetBundleList { get; }

        string GetURL(string assetBundleName);
        string GetTempSavePath(string assetBundleName);
        string GetDestPath(string assetBundleName);
        bool Shuffle { get; } // ダウンロード順をシャッフルするかどうか
        public TimeSpan Timeout { get; } // 各ファイルダウンロードのタイムアウト
        IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] DownloadAsyncDecorators { get; }

        // Output Setter
        // 進捗はProgressのみを保持し、どれくらいダウンロードしたのかは取得時に算出する。
        // 理由は計算量減らすため。
        void SetDownloadProgress(string assetBundleName, float progress);
        void SetDownloadedAssetBundle(string assetBundleName); // ダウンロードが完了したABを格納。保存先はSet時点ではTempを想定

        /// <summary>
        /// FetchでTempに保存したAssetBundleの名前を返す。
        /// ダウンロード失敗、破損しているAssetは除く
        /// </summary>
        IEnumerable<string> GetTempAssetBundles();
        void ReportBrokenAssetBundle(string assetBundleName);
        public bool ExistsBrokenAssetBundle();

        void SetMergedAssetBundle(string assetBundleName);
        IEnumerable<string> GetMergedAssetBundles();

        Exception Error { get; set; } // Fetchでエラーが発生したらここに入れる
    }

    public class BundlePullContext : IBundlePullContext
    {
        public AssetBundleList AssetBundleList { get; set; }

        public List<string> AssetBundleNames { get; set; }

        public bool Shuffle { get; set; }

        public TimeSpan Timeout { get; set; }

        public IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] DownloadAsyncDecorators { get; set; }

        public Exception Error { get; set; } = null;

        AssetBundleHubSettings settings;

        // key: assetBundleName, value: tempPath
        Dictionary<string, string> tempSavePathMap = new Dictionary<string, string>();
        Dictionary<string, float> downloadProgress = new Dictionary<string, float>();
        List<string> tempAssetBundles = new List<string>();
        List<string> mergedAssetBundles = new List<string>();
        bool broken = false;

        public BundlePullContext(params IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] decorators)
        {
            this.settings = AssetBundleHubSettings.Instance;
            Timeout = settings.Timeout;
            Shuffle = settings.shuffle;
            DownloadAsyncDecorators = decorators.Length != 0 ? decorators : DefaultDecorators();
        }

        public string GetURL(string assetBundleName)
        {
            if (string.IsNullOrEmpty(settings.baseUrl))
            {
                throw new Exception("Invalid baseUrl: is null or empty");
            }
            return Path.Combine(settings.baseUrl, assetBundleName);
        }

        public string GetTempSavePath(string assetBundleName)
        {
            if (!tempSavePathMap.TryGetValue(assetBundleName, out var tempPath))
            {
                // https://learn.microsoft.com/en-us/dotnet/api/system.guid.tostring?view=net-7.0
                // ハイフンを含まない32字
                tempPath = settings.TempSavePath + "/AB/" + Guid.NewGuid().ToString("N");
                tempSavePathMap[assetBundleName] = tempPath;
            }

            return tempPath;
        }

        public string GetDestPath(string assetBundleName)
        {
            // TODO: ロード時やAssetBundleListにも使うパスなのでこの取得は共通化する
            return Path.Combine(settings.SaveDataPath, assetBundleName);
        }

        public void SetDownloadProgress(string assetBundleName, float progress)
        {
            downloadProgress[assetBundleName] = progress;
        }

        public float CalcProgress()
        {
            ulong downloadSize = 0L;
            ulong downloadedSize = 0L;

            foreach (var abName in AssetBundleNames)
            {
                var abSize = AssetBundleList.Infos[abName].Size;
                downloadSize += (ulong)abSize;

                if (!downloadProgress.TryGetValue(abName, out float progress))
                {
                    continue;
                }

                if (Mathf.Approximately(progress, 1.0f))
                {
                    // ダウンロード済みなら計算を省略
                    downloadedSize += (ulong)abSize;
                }
                else if (progress > 0f)
                {
                    // floatだとint最大値のときにはみ出すので8byteのdoubleを使用
                    downloadedSize += (ulong)(abSize * (double)progress);
                }
            }
            if (downloadSize == 0L)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)(downloadedSize / (double)downloadSize));
        }

        public void SetDownloadedAssetBundle(string assetBundleName)
        {
            tempAssetBundles.Add(assetBundleName);
        }

        public IEnumerable<string> GetTempAssetBundles() => tempAssetBundles;

        public void ReportBrokenAssetBundle(string assetBundleName)
        {
            tempAssetBundles.Remove(assetBundleName);
            broken = true;
        }

        public bool ExistsBrokenAssetBundle() => broken;

        public void SetMergedAssetBundle(string assetBundleName)
        {
            mergedAssetBundles.Add(assetBundleName);
        }

        public IEnumerable<string> GetMergedAssetBundles()
        {
            return mergedAssetBundles;
        }

        static IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] DefaultDecorators()
        {
            return ServiceLocator.Instance.Resolve<IDownloadAsyncDecoratorsFactory>().Create();
        }
    }
}
