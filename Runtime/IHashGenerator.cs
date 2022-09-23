using System.Security.Cryptography;
using System.Text;

namespace AssetBundleHub
{
    /// <summary>
    /// AssetBundleのパスからHash生成する用
    /// </summary>
    public interface IHashGenerator
    {
        string GenerateHash(string str);
    }

    public class HashGenerator : IHashGenerator
    {
        HMACSHA256 hmacsha256;
        public HashGenerator(string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            hmacsha256 = new HMACSHA256(keyBytes);
        }

        public string GenerateHash(string str)
        {
            byte[] srcBytes = Encoding.UTF8.GetBytes(str);
            byte[] destBytes = hmacsha256.ComputeHash(srcBytes);

            // byteを文字列に変換
            var sb = new StringBuilder();
            foreach (byte b in destBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
