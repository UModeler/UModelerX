using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace Tripolygon.UModelerX.Editor.Importers.GLTF
{
    internal sealed class UMXGltfBuiltPrimitive
    {
        public Material Material;
        public Mesh Mesh;
        public string Name;
    }

    internal sealed class UMXGltfBuiltMesh
    {
        public string Name;
        public List<UMXGltfBuiltPrimitive> Primitives = new List<UMXGltfBuiltPrimitive>();
    }

    internal sealed class UMXGltfMeshBuilder
    {
        private readonly UMXGltfAssetContext assetContext;
        private readonly UMXGltfLoadedDocument document;
        private readonly UMXGltfMaterialBuilder materialBuilder;

        public UMXGltfMeshBuilder(
            UMXGltfLoadedDocument loadedDocument,
            UMXGltfAssetContext context,
            UMXGltfMaterialBuilder gltfMaterialBuilder)
        {
            document = loadedDocument;
            assetContext = context;
            materialBuilder = gltfMaterialBuilder;
        }

        public UMXGltfBuiltMesh[] BuildAll()
        {
            if (document.Root.meshes == null || document.Root.meshes.Length == 0)
            {
                return Array.Empty<UMXGltfBuiltMesh>();
            }

            var result = new UMXGltfBuiltMesh[document.Root.meshes.Length];
            for (var meshIndex = 0; meshIndex < result.Length; ++meshIndex)
            {
                result[meshIndex] = BuildMesh(document.Root.meshes[meshIndex], meshIndex);
            }

            return result;
        }

        private UMXGltfBuiltMesh BuildMesh(UMXGltfMesh sourceMesh, int meshIndex)
        {
            var builtMesh = new UMXGltfBuiltMesh();
            builtMesh.Name = string.IsNullOrEmpty(sourceMesh?.name) ? $"GLTF_Mesh_{meshIndex}" : sourceMesh.name;

            if (sourceMesh?.primitives == null)
            {
                return builtMesh;
            }

            for (var primitiveIndex = 0; primitiveIndex < sourceMesh.primitives.Length; ++primitiveIndex)
            {
                var primitive = sourceMesh.primitives[primitiveIndex];
                if (primitive == null)
                {
                    continue;
                }

                var unityMesh = BuildPrimitiveMesh(sourceMesh, primitive, meshIndex, primitiveIndex);
                builtMesh.Primitives.Add(new UMXGltfBuiltPrimitive
                {
                    Mesh = unityMesh,
                    Material = materialBuilder.GetMaterial(primitive.material),
                    Name = unityMesh.name
                });
            }

            return builtMesh;
        }

        private Mesh BuildPrimitiveMesh(
            UMXGltfMesh sourceMesh,
            UMXGltfPrimitive primitive,
            int meshIndex,
            int primitiveIndex)
        {
            if (primitive.mode != UMXGltfSupportedSubset.TrianglesMode)
            {
                throw new NotSupportedException($"Primitive mode {primitive.mode} is not supported.");
            }

            var positions = UMXGltfAccessorReader.ReadVector3(document, primitive.attributes.POSITION);
            var normals = primitive.attributes.NORMAL >= 0
                ? UMXGltfAccessorReader.ReadVector3(document, primitive.attributes.NORMAL)
                : null;
            var tangents = primitive.attributes.TANGENT >= 0
                ? UMXGltfAccessorReader.ReadVector4(document, primitive.attributes.TANGENT)
                : null;
            var uvs = primitive.attributes.TEXCOORD_0 >= 0
                ? UMXGltfAccessorReader.ReadVector2(document, primitive.attributes.TEXCOORD_0)
                : null;
            var colors = primitive.attributes.COLOR_0 >= 0
                ? UMXGltfAccessorReader.ReadColors(document, primitive.attributes.COLOR_0)
                : null;
            var indices = primitive.indices >= 0
                ? UMXGltfAccessorReader.ReadIndices(document, primitive.indices)
                : BuildSequentialIndices(positions.Length);

            ConvertCoordinates(positions, normals, tangents, uvs);
            UMXGltfCoordinateUtility.ReverseTriangleWinding(indices);

            var mesh = new Mesh();
            mesh.name = BuildMeshName(sourceMesh, meshIndex, primitiveIndex);
            mesh.indexFormat = positions.Length > UInt16.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = positions;
            mesh.triangles = indices;

            if (normals != null && normals.Length == positions.Length)
            {
                mesh.normals = normals;
            }
            else
            {
                mesh.RecalculateNormals();
            }

            if (tangents != null && tangents.Length == positions.Length)
            {
                mesh.tangents = tangents;
            }

            if (uvs != null && uvs.Length == positions.Length)
            {
                mesh.uv = uvs;
            }

            if (colors != null && colors.Length == positions.Length)
            {
                mesh.colors = colors;
            }

            ApplyBoneWeights(mesh, primitive, positions.Length);

            mesh.RecalculateBounds();

            if ((tangents == null || tangents.Length == 0) && uvs != null && normals != null)
            {
                mesh.RecalculateTangents();
            }

            assetContext.AddObject(mesh.name, mesh);
            return mesh;
        }

        /// <summary>
        /// JOINTS_0/WEIGHTS_0 가 모두 있으면 <see cref="Mesh.boneWeights"/> 를 채운다.
        /// 첫 마일스톤은 vertex 당 4본 한정. weight 합이 1.0 에서 벗어난 데이터는 정규화.
        /// bindposes 는 NodeBuilder 에서 skin 단위로 채워진다(여기서는 다루지 않는다).
        /// </summary>
        private void ApplyBoneWeights(Mesh mesh, UMXGltfPrimitive primitive, int vertexCount)
        {
            if (primitive.attributes.JOINTS_0 < 0 || primitive.attributes.WEIGHTS_0 < 0)
            {
                return;
            }

            var joints = UMXGltfAccessorReader.ReadJointsVec4(document, primitive.attributes.JOINTS_0);
            var weights = UMXGltfAccessorReader.ReadWeightsVec4(document, primitive.attributes.WEIGHTS_0);

            if (joints.Length != vertexCount * 4 || weights.Length != vertexCount)
            {
                throw new InvalidOperationException(
                    $"JOINTS_0/WEIGHTS_0 length mismatch: joints={joints.Length}, weights={weights.Length}, vertices={vertexCount} (expected joints={vertexCount * 4}).");
            }

            var boneWeights = new BoneWeight[vertexCount];
            for (var i = 0; i < vertexCount; ++i)
            {
                var w = weights[i];
                var sum = w.x + w.y + w.z + w.w;
                if (sum > 0f)
                {
                    var inv = 1f / sum;
                    w.x *= inv; w.y *= inv; w.z *= inv; w.w *= inv;
                }

                boneWeights[i] = new BoneWeight
                {
                    boneIndex0 = joints[i * 4 + 0], weight0 = w.x,
                    boneIndex1 = joints[i * 4 + 1], weight1 = w.y,
                    boneIndex2 = joints[i * 4 + 2], weight2 = w.z,
                    boneIndex3 = joints[i * 4 + 3], weight3 = w.w,
                };
            }

            mesh.boneWeights = boneWeights;
        }

        private static string BuildMeshName(UMXGltfMesh sourceMesh, int meshIndex, int primitiveIndex)
        {
            var sourceName = string.IsNullOrEmpty(sourceMesh?.name) ? $"GLTF_Mesh_{meshIndex}" : sourceMesh.name;
            return $"{sourceName}_Primitive_{primitiveIndex}";
        }

        private static int[] BuildSequentialIndices(int count)
        {
            var result = new int[count];
            for (var i = 0; i < count; ++i)
            {
                result[i] = i;
            }

            return result;
        }

        private static void ConvertCoordinates(Vector3[] positions, Vector3[] normals, Vector4[] tangents, Vector2[] uvs)
        {
            if (positions != null)
            {
                for (var i = 0; i < positions.Length; ++i)
                {
                    positions[i] = UMXGltfCoordinateUtility.ToUnityPosition(positions[i]);
                }
            }

            if (normals != null)
            {
                for (var i = 0; i < normals.Length; ++i)
                {
                    normals[i] = UMXGltfCoordinateUtility.ToUnityNormal(normals[i]);
                }
            }

            if (tangents != null)
            {
                for (var i = 0; i < tangents.Length; ++i)
                {
                    tangents[i] = UMXGltfCoordinateUtility.ToUnityTangent(tangents[i]);
                }
            }

            if (uvs != null)
            {
                for (var i = 0; i < uvs.Length; ++i)
                {
                    uvs[i] = UMXGltfCoordinateUtility.ToUnityUv(uvs[i]);
                }
            }
        }
    }
}
