using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;

namespace Tripolygon.UModelerX.Editor.Importers.GLTF
{
    internal sealed class UMXGltfLoadedDocument
    {
        private readonly List<byte[]> buffers;
        private readonly List<byte[]> images;

        public UMXGltfLoadedDocument(string assetPath, UMXGltfRoot root, List<byte[]> resolvedBuffers, List<byte[]> resolvedImages)
        {
            AssetPath = assetPath;
            Root = root;
            buffers = resolvedBuffers ?? new List<byte[]>();
            images = resolvedImages ?? new List<byte[]>();
        }

        public string AssetPath { get; }

        public string DirectoryPath
        {
            get
            {
                return Path.GetDirectoryName(AssetPath) ?? string.Empty;
            }
        }

        public UMXGltfRoot Root { get; }

        public byte[] GetBufferBytes(int index)
        {
            if (index < 0 || index >= buffers.Count)
            {
                throw new IndexOutOfRangeException($"Buffer index {index} is outside the resolved buffer list.");
            }

            return buffers[index];
        }

        public byte[] GetImageBytes(int index)
        {
            if (index < 0 || index >= images.Count)
            {
                throw new IndexOutOfRangeException($"Image index {index} is outside the resolved image list.");
            }

            return images[index];
        }

        public byte[] GetBufferViewBytes(int index)
        {
            if (Root.bufferViews == null || index < 0 || index >= Root.bufferViews.Length)
            {
                throw new IndexOutOfRangeException($"BufferView index {index} is invalid.");
            }

            var bufferView = Root.bufferViews[index];
            var source = GetBufferBytes(bufferView.buffer);
            var sourceOffset = bufferView.byteOffset;
            var length = bufferView.byteLength;
            var slice = new byte[length];
            Buffer.BlockCopy(source, sourceOffset, slice, 0, length);
            return slice;
        }
    }

    internal static class UMXGltfDocumentLoader
    {
        /// <summary>GLB 바이트 배열에서 직접 로드한다. 디스크 I/O 없이 메모리에서만 동작한다.</summary>
        public static UMXGltfLoadedDocument LoadFromBytes(byte[] glbData, string assetName)
        {
            if (glbData == null || glbData.Length == 0)
                throw new ArgumentException("GLB data is empty.", nameof(glbData));

            var glbContents = UMXGlbReader.Read(glbData);
            var root = JsonUtility.FromJson<UMXGltfRoot>(glbContents.Json);
            UMXGltfSupportedSubset.ValidateOrThrow(root);

            var resolvedBuffers = ResolveBuffersFromGlb(root, glbContents.BinaryChunk);
            var document = new UMXGltfLoadedDocument(
                assetName ?? "memory",
                root,
                resolvedBuffers,
                new List<byte[]>());

            var resolvedImages = ResolveImagesFromGlb(document);
            return new UMXGltfLoadedDocument(assetName ?? "memory", root, resolvedBuffers, resolvedImages);
        }

        private static List<byte[]> ResolveBuffersFromGlb(UMXGltfRoot root, byte[] binaryChunk)
        {
            var result = new List<byte[]>();
            if (root.buffers == null || root.buffers.Length == 0) return result;

            for (var i = 0; i < root.buffers.Length; ++i)
            {
                var buffer = root.buffers[i];
                if (buffer == null) { result.Add(Array.Empty<byte>()); continue; }
                if (string.IsNullOrEmpty(buffer.uri))
                {
                    if (binaryChunk == null)
                        throw new InvalidOperationException($"Buffer {i} has no uri and no GLB BIN chunk was found.");
                    result.Add(binaryChunk);
                    continue;
                }
                if (buffer.uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(DecodeDataUri(buffer.uri));
                    continue;
                }
                // 외부 파일 참조는 인메모리 모드에서 지원하지 않음
                result.Add(Array.Empty<byte>());
            }
            return result;
        }

        private static List<byte[]> ResolveImagesFromGlb(UMXGltfLoadedDocument document)
        {
            var result = new List<byte[]>();
            var root = document.Root;
            if (root.images == null || root.images.Length == 0) return result;

            for (var i = 0; i < root.images.Length; ++i)
            {
                var image = root.images[i];
                if (image == null) { result.Add(Array.Empty<byte>()); continue; }
                if (!string.IsNullOrEmpty(image.uri))
                {
                    if (image.uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                        result.Add(DecodeDataUri(image.uri));
                    else
                        result.Add(Array.Empty<byte>()); // 외부 파일 무시
                    continue;
                }
                if (image.bufferView >= 0)
                {
                    result.Add(document.GetBufferViewBytes(image.bufferView));
                    continue;
                }
                result.Add(Array.Empty<byte>());
            }
            return result;
        }

        public static UMXGltfLoadedDocument Load(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new ArgumentException("A valid glTF asset path is required.", nameof(assetPath));
            }

            var extension = Path.GetExtension(assetPath)?.ToLowerInvariant();
            var sourceBytes = File.ReadAllBytes(assetPath);
            var embeddedGlbBuffer = default(byte[]);
            string json;

            if (extension == ".glb")
            {
                var glbContents = UMXGlbReader.Read(sourceBytes);
                json = glbContents.Json;
                embeddedGlbBuffer = glbContents.BinaryChunk;
            }
            else
            {
                json = Encoding.UTF8.GetString(sourceBytes);
            }

            var root = JsonUtility.FromJson<UMXGltfRoot>(json);
            UMXGltfSupportedSubset.ValidateOrThrow(root);

            var resolvedBuffers = ResolveBuffers(assetPath, root, embeddedGlbBuffer);
            var document = new UMXGltfLoadedDocument(
                assetPath,
                root,
                resolvedBuffers,
                new List<byte[]>());

            var resolvedImages = ResolveImages(document);
            return new UMXGltfLoadedDocument(assetPath, root, resolvedBuffers, resolvedImages);
        }

        private static List<byte[]> ResolveBuffers(string assetPath, UMXGltfRoot root, byte[] embeddedGlbBuffer)
        {
            var result = new List<byte[]>();
            var directoryPath = Path.GetDirectoryName(assetPath) ?? string.Empty;

            if (root.buffers == null || root.buffers.Length == 0)
            {
                return result;
            }

            for (var i = 0; i < root.buffers.Length; ++i)
            {
                var buffer = root.buffers[i];
                if (buffer == null)
                {
                    result.Add(Array.Empty<byte>());
                    continue;
                }

                if (string.IsNullOrEmpty(buffer.uri))
                {
                    if (embeddedGlbBuffer == null)
                    {
                        throw new InvalidOperationException($"Buffer {i} has no uri and no GLB BIN chunk was found.");
                    }

                    result.Add(embeddedGlbBuffer);
                    continue;
                }

                result.Add(ResolveUriBytes(directoryPath, buffer.uri));
            }

            return result;
        }

        private static List<byte[]> ResolveImages(UMXGltfLoadedDocument document)
        {
            var result = new List<byte[]>();
            var root = document.Root;

            if (root.images == null || root.images.Length == 0)
            {
                return result;
            }

            for (var i = 0; i < root.images.Length; ++i)
            {
                var image = root.images[i];
                if (image == null)
                {
                    result.Add(Array.Empty<byte>());
                    continue;
                }

                if (!string.IsNullOrEmpty(image.uri))
                {
                    result.Add(ResolveUriBytes(document.DirectoryPath, image.uri));
                    continue;
                }

                if (image.bufferView >= 0)
                {
                    result.Add(document.GetBufferViewBytes(image.bufferView));
                    continue;
                }

                throw new InvalidOperationException($"Image {i} does not define a uri or bufferView.");
            }

            return result;
        }

        private static byte[] ResolveUriBytes(string directoryPath, string uri)
        {
            if (uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return DecodeDataUri(uri);
            }

            var resolvedPath = Uri.UnescapeDataString(uri).Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(directoryPath, resolvedPath));
            return File.ReadAllBytes(fullPath);
        }

        private static byte[] DecodeDataUri(string uri)
        {
            var marker = ";base64,";
            var markerIndex = uri.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                throw new NotSupportedException("Only base64 data URIs are supported.");
            }

            var base64 = uri.Substring(markerIndex + marker.Length);
            return Convert.FromBase64String(base64);
        }
    }
}
