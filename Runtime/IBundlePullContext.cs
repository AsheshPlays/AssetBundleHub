using System;
using System.Collections;
using System.Collections.Generic;
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
}
