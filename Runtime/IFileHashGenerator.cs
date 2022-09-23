using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace AssetBundleHub
{
    public interface IFileHashGenerator
    {
        string ComputeHash(string filePath);
    }

    public class MD5FileHashGenerator : IFileHashGenerator
    {
        public string ComputeHash(string filePath)
        {
            StringBuilder hashString = new StringBuilder();
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (MD5CryptoServiceProvider md5Service = new MD5CryptoServiceProvider())
            {
                byte[] hashBytes = md5Service.ComputeHash(fs);
                foreach (var hashByte in hashBytes)
                {
                    hashString.Append(hashByte.ToString("x2")); // 16進数に変換
                }
            }
            return hashString.ToString();
        }
    }
}
