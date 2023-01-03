using System;
using System.IO;

namespace AssetBundleHub
{
    public class XORCryptStream : Stream
    {
        Stream baseStream;
        byte[] key;

        public XORCryptStream(Stream baseStream, byte[] key)
        {
            this.baseStream = baseStream;
            this.key = key;
        }

        public override bool CanRead => baseStream.CanRead;

        public override bool CanSeek => baseStream.CanSeek;

        public override bool CanWrite => baseStream.CanWrite;

        public override long Length => baseStream.Length;

        public override long Position { get { return baseStream.Position; } set { baseStream.Position = value; } }

        public override void Flush() => baseStream.Flush();

        public override long Seek(long offset, SeekOrigin origin) => baseStream.Seek(offset, origin);

        public override void SetLength(long value) => baseStream.SetLength(value);

        /// <summary>
        /// 復号化しつつ読む
        /// </summary>
        /// <param name="buffer">結果を格納するbuffer</param>
        /// <param name="offset">bufferのoffset番目から格納</param>
        /// <param name="count">何byte読み込むか</param>
        /// <returns>読み取ったbyte数を返す</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var startPosition = Position;
            int readCount = baseStream.Read(buffer, offset, count);

            int keyIndex = (int)(startPosition % key.Length);
            for (int i = 0; i < readCount; i++)
            {
                int bufferIndex = i + offset;
                if (keyIndex == key.Length)
                {
                    keyIndex = 0;
                }
                buffer[bufferIndex] ^= key[keyIndex]; // xor
                keyIndex++;
            }
            return readCount;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer.Length < offset + count)
            {
                throw new ArgumentException("offset + count is larger than buffer.Length");
            }

            byte[] writeBuffer = new byte[count];
            var startPosition = Position;
            int keyIndex = (int)(startPosition % key.Length);
            for (int i = 0; i < count; i++)
            {
                int bufferIndex = i + offset;
                if (keyIndex == key.Length)
                {
                    keyIndex = 0;
                }

                writeBuffer[i] = buffer[bufferIndex];
                writeBuffer[i] ^= key[keyIndex];
                keyIndex++;
            }
            baseStream.Write(writeBuffer, 0, count);
        }
    }
}
