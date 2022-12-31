using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using AssetBundleHub;
using AssetBundleHub.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AssetBundleHubTests
{
    public class XORCryptStreamTest
    {
        [Test]
        public void Read_XORをかけつつ読み込める()
        {
            string key = "1234abcd";
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] encryptedHelloWorld = new byte[] { 89, 87, 95, 88, 14, 21, 12, 22, 93, 86 };

            byte[] buffer1 = new byte[3];
            byte[] buffer2 = new byte[4];

            using (var ms = new MemoryStream(encryptedHelloWorld, false))
            using (var cs = new XORCryptStream(ms, keyBytes))
            {
                int readCount1 = cs.Read(buffer1, 1, 2);
                Assert.AreEqual(2, readCount1);

                int readCount2 = cs.Read(buffer2, 2, 1);
                Assert.AreEqual(1, readCount2);
            }

            Assert.AreEqual(0, Convert.ToInt32(buffer1[0]), "offset指定で書き込まない");
            Assert.AreEqual(104, Convert.ToInt32(buffer1[1]), "h");
            Assert.AreEqual(101, Convert.ToInt32(buffer1[2]), "e");

            Assert.AreEqual(0, Convert.ToInt32(buffer2[0]), "offset指定で書き込まない");
            Assert.AreEqual(0, Convert.ToInt32(buffer2[1]), "offset指定で書き込まない");
            Assert.AreEqual(108, Convert.ToInt32(buffer2[2]), "l");
            Assert.AreEqual(0, Convert.ToInt32(buffer2[3]), "書き込み範囲外");
        }

        [Test]
        public void Write_XORをかけつつ書き込める()
        {
            string key = "1234abcd";
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);

            byte[] encryptedHello = new byte[] { 89, 87, 95, 88, 14 };
            byte[] encryptedWorld = new byte[] { 0, 0, 21, 12, 22, 93, 86 }; // 最初の2つはoffsetの確認用
            byte[] buffer = new byte[10];

            using (var ms = new MemoryStream())
            using (var cs = new XORCryptStream(ms, keyBytes))
            {
                cs.Write(encryptedHello, 0, 5);
                cs.Write(encryptedWorld, 2, 5); // offset分ずらす

                Assert.AreEqual(10, ms.Length);
                ms.Seek(0L, SeekOrigin.Begin);
                ms.Read(buffer, 0, buffer.Length);
            }
            Assert.AreEqual("helloworld", Encoding.UTF8.GetString(buffer));
        }
    }
}
