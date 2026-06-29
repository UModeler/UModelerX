using System;
using System.Text;

namespace Tripolygon.UModelerX.Editor.Importers.GLTF
{
    internal readonly struct UMXGlbContents
    {
        public UMXGlbContents(string json, byte[] binaryChunk)
        {
            Json = json;
            BinaryChunk = binaryChunk;
        }

        public string Json { get; }

        public byte[] BinaryChunk { get; }
    }

    internal static class UMXGlbReader
    {
        private const uint GlbMagic = 0x46546C67;
        private const uint JsonChunkType = 0x4E4F534A;
        private const uint BinaryChunkType = 0x004E4942;
        private const int HeaderLength = 12;
        private const int ChunkHeaderLength = 8;

        public static UMXGlbContents Read(byte[] bytes)
        {
            if (bytes == null || bytes.Length < HeaderLength + ChunkHeaderLength)
            {
                throw new InvalidOperationException("The GLB file is too small to contain a valid header.");
            }

            var magic = BitConverter.ToUInt32(bytes, 0);
            if (magic != GlbMagic)
            {
                throw new InvalidOperationException("The file does not contain a valid GLB magic header.");
            }

            var version = BitConverter.ToUInt32(bytes, 4);
            if (version != 2)
            {
                throw new NotSupportedException($"GLB version {version} is not supported. Only glTF 2.0 is supported.");
            }

            var fileLength = BitConverter.ToUInt32(bytes, 8);
            if (fileLength != bytes.Length)
            {
                throw new InvalidOperationException("The GLB file length does not match the header length.");
            }

            var offset = HeaderLength;
            string json = null;
            byte[] binaryChunk = null;

            while (offset + ChunkHeaderLength <= bytes.Length)
            {
                var chunkLength = BitConverter.ToInt32(bytes, offset);
                var chunkType = BitConverter.ToUInt32(bytes, offset + 4);
                offset += ChunkHeaderLength;

                if (chunkLength < 0 || offset + chunkLength > bytes.Length)
                {
                    throw new InvalidOperationException("The GLB chunk length is invalid.");
                }

                if (chunkType == JsonChunkType)
                {
                    json = Encoding.UTF8.GetString(bytes, offset, chunkLength).TrimEnd('\0', ' ', '\r', '\n', '\t');
                }
                else if (chunkType == BinaryChunkType)
                {
                    binaryChunk = new byte[chunkLength];
                    Buffer.BlockCopy(bytes, offset, binaryChunk, 0, chunkLength);
                }

                offset += chunkLength;
            }

            if (string.IsNullOrEmpty(json))
            {
                throw new InvalidOperationException("The GLB file does not contain a JSON chunk.");
            }

            return new UMXGlbContents(json, binaryChunk);
        }
    }
}
