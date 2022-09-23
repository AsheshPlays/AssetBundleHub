using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace AssetBundleHubEditor
{
    /// <summary>
    /// AssetGraphのLoaderを参考に作成
    /// 特定のパスからファイルを抽出して返す。
    /// </summary>
    public class LoadFromDirectory
    {
        public List<string> ignore = new List<string>(); //正規表現のリスト

        public static LoadFromDirectory CreateWithIgnoreDep()
        {
            var rtn = new LoadFromDirectory();
            rtn.ignore.Add(".*Dep.*");
            return rtn;
        }

        public List<string> Run(string dirPath)
        {
            return AssetDatabase.FindAssets("", new string[] { dirPath })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(p => Path.HasExtension(p) && !IsMatchIgnore(p)) // フォルダとignore取り除く
                .Distinct()
                .ToList();
        }

        // ignoreの条件に当てはまるか
        bool IsMatchIgnore(string target)
        {
            foreach (var pattern in ignore)
            {
                if (Regex.IsMatch(target, pattern))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
