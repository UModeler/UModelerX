using System;

using UnityEngine;

namespace Tripolygon.UModelerX.Editor.Importers.GLTF
{
    internal static class UMXGltfAccessorReader
    {
        private readonly struct AccessorContext
        {
            public AccessorContext(
                byte[] buffer,
                int elementCount,
                int elementOffset,
                int componentCount,
                int componentSize,
                int componentType,
                bool normalized,
                int stride)
            {
                Buffer = buffer;
                ElementCount = elementCount;
                ElementOffset = elementOffset;
                ComponentCount = componentCount;
                ComponentSize = componentSize;
                ComponentType = componentType;
                Normalized = normalized;
                Stride = stride;
            }

            public byte[] Buffer { get; }

            public int ComponentCount { get; }

            public int ComponentSize { get; }

            public int ComponentType { get; }

            public int ElementCount { get; }

            public int ElementOffset { get; }

            public bool Normalized { get; }

            public int Stride { get; }
        }

        public static Vector3[] ReadVector3(UMXGltfLoadedDocument document, int accessorIndex)
        {
            var context = CreateContext(document, accessorIndex, "VEC3");
            var result = new Vector3[context.ElementCount];

            for (var i = 0; i < result.Length; ++i)
            {
                var baseOffset = context.ElementOffset + context.Stride * i;
                result[i] = new Vector3(
                    ReadFloat(context, baseOffset, 0),
                    ReadFloat(context, baseOffset, 1),
                    ReadFloat(context, baseOffset, 2));
            }

            return result;
        }

        public static Vector4[] ReadVector4(UMXGltfLoadedDocument document, int accessorIndex)
        {
            var context = CreateContext(document, accessorIndex, "VEC4");
            var result = new Vector4[context.ElementCount];

            for (var i = 0; i < result.Length; ++i)
            {
                var baseOffset = context.ElementOffset + context.Stride * i;
                result[i] = new Vector4(
                    ReadFloat(context, baseOffset, 0),
                    ReadFloat(context, baseOffset, 1),
                    ReadFloat(context, baseOffset, 2),
                    ReadFloat(context, baseOffset, 3));
            }

            return result;
        }

        public static Vector2[] ReadVector2(UMXGltfLoadedDocument document, int accessorIndex)
        {
            var context = CreateContext(document, accessorIndex, "VEC2");
            var result = new Vector2[context.ElementCount];

            for (var i = 0; i < result.Length; ++i)
            {
                var baseOffset = context.ElementOffset + context.Stride * i;
                result[i] = new Vector2(
                    ReadFloat(context, baseOffset, 0),
                    ReadFloat(context, baseOffset, 1));
            }

            return result;
        }

        public static Color[] ReadColors(UMXGltfLoadedDocument document, int accessorIndex)
        {
            var accessor = GetAccessor(document, accessorIndex);
            if (accessor.type != "VEC3" && accessor.type != "VEC4")
            {
                throw new InvalidOperationException($"Accessor {accessorIndex} must be VEC3 or VEC4 for vertex colors.");
            }

            var context = CreateContext(document, accessorIndex, accessor.type);
            var result = new Color[context.ElementCount];
            var hasAlpha = context.ComponentCount == 4;

            for (var i = 0; i < result.Length; ++i)
            {
                var baseOffset = context.ElementOffset + context.Stride * i;
                var r = ReadFloat(context, baseOffset, 0);
                var g = ReadFloat(context, baseOffset, 1);
                var b = ReadFloat(context, baseOffset, 2);
                var a = hasAlpha ? ReadFloat(context, baseOffset, 3) : 1.0f;
                result[i] = new Color(r, g, b, a);
            }

            return result;
        }

        /// <summary>
        /// 애니메이션 sampler input(시간 keyframe)이나 CUBICSPLINE output 같은 SCALAR/VEC* float
        /// 데이터를 그대로 float 배열로 읽는다. <see cref="ReadIndices"/> 는 정수 변환을 하므로
        /// keyframe 시간에 부적합하다.
        /// </summary>
        public static float[] ReadFloatScalar(UMXGltfLoadedDocument document, int accessorIndex)
        {
            var context = CreateContext(document, accessorIndex, "SCALAR");
            var result = new float[context.ElementCount];
            for (var i = 0; i < result.Length; ++i)
            {
                var baseOffset = context.ElementOffset + context.Stride * i;
                result[i] = ReadFloat(context, baseOffset, 0);
            }

            return result;
        }

        public static int[] ReadIndices(UMXGltfLoadedDocument document, int accessorIndex)
        {
            var context = CreateContext(document, accessorIndex, "SCALAR");
            var result = new int[context.ElementCount];

            for (var i = 0; i < result.Length; ++i)
            {
                var elementOffset = context.ElementOffset + context.Stride * i;
                result[i] = ReadInt(context, elementOffset);
            }

            return result;
        }

        /// <summary>
        /// JOINTS_0 — VEC4 SCALAR (componentType UByte/UShort) → 정수 4개.
        /// 결과는 [count][4] 형태의 평탄화 배열. 인덱스 i 의 4개 본 인덱스는 (4*i .. 4*i+3).
        /// </summary>
        public static int[] ReadJointsVec4(UMXGltfLoadedDocument document, int accessorIndex)
        {
            var context = CreateContext(document, accessorIndex, "VEC4");
            if (context.ComponentType != 5121 && context.ComponentType != 5123)
            {
                throw new NotSupportedException($"JOINTS accessor {accessorIndex} component type {context.ComponentType} is not supported (expected UByte/UShort).");
            }

            var result = new int[context.ElementCount * 4];
            for (var i = 0; i < context.ElementCount; ++i)
            {
                var baseOffset = context.ElementOffset + context.Stride * i;
                result[i * 4 + 0] = ReadUnsignedInt(context, baseOffset, 0);
                result[i * 4 + 1] = ReadUnsignedInt(context, baseOffset, 1);
                result[i * 4 + 2] = ReadUnsignedInt(context, baseOffset, 2);
                result[i * 4 + 3] = ReadUnsignedInt(context, baseOffset, 3);
            }

            return result;
        }

        /// <summary>
        /// WEIGHTS_0 — VEC4. 일반적으로 Float, 드물게 normalized UByte/UShort.
        /// 기존 <see cref="ReadVector4"/> 와 동일 경로지만 normalized 정수형 처리를 위해 별도 헬퍼.
        /// (ReadFloat 가 normalized 케이스를 이미 처리하므로 사실상 ReadVector4 와 동일.)
        /// </summary>
        public static Vector4[] ReadWeightsVec4(UMXGltfLoadedDocument document, int accessorIndex)
        {
            return ReadVector4(document, accessorIndex);
        }

        /// <summary>
        /// inverseBindMatrices — MAT4 Float. 각 element 는 column-major 16 float.
        /// </summary>
        public static Matrix4x4[] ReadMatrix4x4Array(UMXGltfLoadedDocument document, int accessorIndex)
        {
            var context = CreateContext(document, accessorIndex, "MAT4");
            if (context.ComponentType != 5126)
            {
                throw new NotSupportedException($"MAT4 accessor {accessorIndex} component type {context.ComponentType} is not supported (expected Float).");
            }

            var result = new Matrix4x4[context.ElementCount];
            for (var i = 0; i < context.ElementCount; ++i)
            {
                var baseOffset = context.ElementOffset + context.Stride * i;
                var m = new Matrix4x4();
                m.SetColumn(0, new Vector4(
                    ReadFloat(context, baseOffset, 0),
                    ReadFloat(context, baseOffset, 1),
                    ReadFloat(context, baseOffset, 2),
                    ReadFloat(context, baseOffset, 3)));
                m.SetColumn(1, new Vector4(
                    ReadFloat(context, baseOffset, 4),
                    ReadFloat(context, baseOffset, 5),
                    ReadFloat(context, baseOffset, 6),
                    ReadFloat(context, baseOffset, 7)));
                m.SetColumn(2, new Vector4(
                    ReadFloat(context, baseOffset, 8),
                    ReadFloat(context, baseOffset, 9),
                    ReadFloat(context, baseOffset, 10),
                    ReadFloat(context, baseOffset, 11)));
                m.SetColumn(3, new Vector4(
                    ReadFloat(context, baseOffset, 12),
                    ReadFloat(context, baseOffset, 13),
                    ReadFloat(context, baseOffset, 14),
                    ReadFloat(context, baseOffset, 15)));
                result[i] = m;
            }

            return result;
        }

        public static int GetCount(UMXGltfLoadedDocument document, int accessorIndex)
        {
            return GetAccessor(document, accessorIndex).count;
        }

        private static AccessorContext CreateContext(UMXGltfLoadedDocument document, int accessorIndex, string expectedType)
        {
            var accessor = GetAccessor(document, accessorIndex);
            if (accessor.bufferView < 0)
            {
                throw new InvalidOperationException($"Accessor {accessorIndex} does not reference a bufferView.");
            }

            if (!string.Equals(accessor.type, expectedType, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Accessor {accessorIndex} must be {expectedType}, but was {accessor.type}.");
            }

            var bufferView = document.Root.bufferViews[accessor.bufferView];
            var sourceBuffer = document.GetBufferBytes(bufferView.buffer);
            var componentSize = GetComponentSize(accessor.componentType);
            var componentCount = GetComponentCount(accessor.type);
            var elementSize = componentSize * componentCount;
            var stride = bufferView.byteStride > 0 ? bufferView.byteStride : elementSize;
            var elementOffset = bufferView.byteOffset + accessor.byteOffset;

            return new AccessorContext(
                sourceBuffer,
                accessor.count,
                elementOffset,
                componentCount,
                componentSize,
                accessor.componentType,
                accessor.normalized,
                stride);
        }

        private static UMXGltfAccessor GetAccessor(UMXGltfLoadedDocument document, int accessorIndex)
        {
            if (document.Root.accessors == null || accessorIndex < 0 || accessorIndex >= document.Root.accessors.Length)
            {
                throw new IndexOutOfRangeException($"Accessor {accessorIndex} is invalid.");
            }

            var accessor = document.Root.accessors[accessorIndex];
            if (accessor == null)
            {
                throw new InvalidOperationException($"Accessor {accessorIndex} is null.");
            }

            return accessor;
        }

        private static int GetComponentCount(string accessorType)
        {
            return accessorType switch
            {
                "SCALAR" => 1,
                "VEC2" => 2,
                "VEC3" => 3,
                "VEC4" => 4,
                "MAT4" => 16,
                _ => throw new NotSupportedException($"Accessor type {accessorType} is not supported.")
            };
        }

        private static int ReadUnsignedInt(AccessorContext context, int baseOffset, int componentIndex)
        {
            var componentOffset = baseOffset + context.ComponentSize * componentIndex;
            switch (context.ComponentType)
            {
                case 5121:
                    return context.Buffer[componentOffset];
                case 5123:
                    return BitConverter.ToUInt16(context.Buffer, componentOffset);
                case 5125:
                    return checked((int)BitConverter.ToUInt32(context.Buffer, componentOffset));
            }

            throw new NotSupportedException($"Component type {context.ComponentType} cannot be read as unsigned int.");
        }

        private static int GetComponentSize(int componentType)
        {
            return componentType switch
            {
                5121 => 1,
                5123 => 2,
                5125 => 4,
                5126 => 4,
                _ => throw new NotSupportedException($"Component type {componentType} is not supported.")
            };
        }

        private static float ReadFloat(AccessorContext context, int baseOffset, int componentIndex)
        {
            var componentOffset = baseOffset + context.ComponentSize * componentIndex;

            switch (context.ComponentType)
            {
                case 5126:
                    return BitConverter.ToSingle(context.Buffer, componentOffset);
                case 5121:
                    {
                        var value = context.Buffer[componentOffset];
                        return context.Normalized ? value / 255.0f : value;
                    }
                case 5123:
                    {
                        var value = BitConverter.ToUInt16(context.Buffer, componentOffset);
                        return context.Normalized ? value / 65535.0f : value;
                    }
            }

            throw new NotSupportedException($"Component type {context.ComponentType} cannot be read as float.");
        }

        private static int ReadInt(AccessorContext context, int elementOffset)
        {
            switch (context.ComponentType)
            {
                case 5121:
                    return context.Buffer[elementOffset];
                case 5123:
                    return BitConverter.ToUInt16(context.Buffer, elementOffset);
                case 5125:
                    return checked((int)BitConverter.ToUInt32(context.Buffer, elementOffset));
            }

            throw new NotSupportedException($"Index component type {context.ComponentType} is not supported.");
        }
    }
}
