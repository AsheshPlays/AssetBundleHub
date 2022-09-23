using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System;
using System.IO;

namespace AssetBundleHubEditor
{
    /// <summary>
    /// AssetGraphのLoaderを参考に作成
    /// 特定のパスからファイルを抽出して返す
    /// </summary>
    public class GroupByFilePath
    {
        /// <summary>
        /// DirPathをキーとしてグループ化
        /// </summary>
        /// <returns>key: DirPath value: filePathのリスト</returns>
        public Dictionary<string, List<string>> Run(List<string> filePaths)
        {
            return filePaths.GroupBy(x => Path.GetDirectoryName(x))
                .ToDictionary(group => group.Key, group => group.ToList());
        }
    }
}
