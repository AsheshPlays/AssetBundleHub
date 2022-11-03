using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssetBundleHub
{
    // Pullの一連の流れの途中経過を全てここに入れる
    public interface IBundlePullContext
    {
        // Input
        AssetBundleList AssetBundleList { get; }
        List<string> AssetBundleNames { get; }
        string GetURL(string assetBundleName);
        string GetTempSavePath(string assetBundleName);
        string GetDestPath(string assetBundleName);
        bool Shuffle { get; } // ダウンロード順をシャッフルするかどうか
        public TimeSpan Timeout { get; } // 各ファイルダウンロードのタイムアウト
        IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] DownloadAsyncDecorators { get; }

        // Output
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

        void SetMergedAssetBundle(string assetBundleName);
        IEnumerable<string> GetMergedAssetBundles();

        Exception Error { get; set; } // Fetchでエラーが発生したらここに入れる
    }

    public class BundlePullContext : IBundlePullContext
    {
        public AssetBundleList AssetBundleList { get; set; }

        public List<string> AssetBundleNames { get; set; }

        public bool Shuffle { get; set; } = true;

        public TimeSpan Timeout { get; set; }

        public IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] DownloadAsyncDecorators { get; set; }

        public Exception Error { get; set; } = null;

        AssetBundleHubSettings settings;

        // key: assetBundleName, value: tempPath
        Dictionary<string, string> tempSavePathMap = new Dictionary<string, string>();
        Dictionary<string, float> downloadProgress = new Dictionary<string, float>();
        List<string> tempAssetBundles = new List<string>();
        List<string> mergedAssetBundles = new List<string>();

        public BundlePullContext(AssetBundleHubSettings settings, params IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[] decorators)
        {
            this.settings = settings;
            Timeout = settings.Timeout;
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
            if(!tempSavePathMap.TryGetValue(assetBundleName, out var tempPath))
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

        public void SetDownloadedAssetBundle(string assetBundleName)
        {
            tempAssetBundles.Add(assetBundleName);
        }

        public IEnumerable<string> GetTempAssetBundles() => tempAssetBundles;

        public void ReportBrokenAssetBundle(string assetBundleName)
        {
            tempAssetBundles.Remove(assetBundleName);
        }

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
            return new IDownloadAsyncDecorator<IDownloadRequestContext, IDownloadResponseContext>[]
            {
                new QueueRequestDecorator(runCapacity: 4),
                new UnityWebRequestDownloadFile()
            };
        }
    }
}
