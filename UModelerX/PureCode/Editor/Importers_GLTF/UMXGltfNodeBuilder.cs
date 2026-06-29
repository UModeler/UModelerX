using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace Tripolygon.UModelerX.Editor.Importers.GLTF
{
    internal sealed class UMXGltfNodeBuilder
    {
        private readonly UMXGltfBuiltMesh[] builtMeshes;
        private readonly UMXGltfLoadedDocument document;

        // mesh 자체는 skin 단위로 복제하지 않고 공유하므로(자원 절약), 동일 mesh 가 서로 다른 skin
        // 으로 두 번 부착되면 bindposes 가 덮어써져 첫 부착이 깨진다. 첫 마일스톤의 "1 mesh : 1 skin"
        // 가정이 깨졌는지 추적해 즉시 throw 한다.
        private readonly Dictionary<int, int> meshIndexToSkinIndex = new Dictionary<int, int>();

        public UMXGltfNodeBuilder(UMXGltfLoadedDocument loadedDocument, UMXGltfBuiltMesh[] meshes)
        {
            document = loadedDocument;
            builtMeshes = meshes;
            NodePaths = Array.Empty<string>();
        }

        /// <summary>
        /// 각 node 의 rootObject 기준 상대 path (예: "Armature/Hip/UpLeg"). active scene 에서 도달
        /// 가능하지 않은 node 의 entry 는 null. AnimationBuilder 의 <see cref="AnimationClip.SetCurve"/>
        /// 첫 인자로 직접 사용된다.
        /// </summary>
        public string[] NodePaths { get; private set; }

        public GameObject BuildSceneRoot()
        {
            var rootName = Path.GetFileNameWithoutExtension(document.AssetPath);
            var rootObject = new GameObject(string.IsNullOrEmpty(rootName) ? "GLTF_Root" : rootName);

            var sceneIndex = ResolveSceneIndex();
            if (document.Root.scenes == null || sceneIndex < 0 || sceneIndex >= document.Root.scenes.Length)
            {
                return rootObject;
            }

            var scene = document.Root.scenes[sceneIndex];
            if (scene?.nodes == null)
            {
                return rootObject;
            }

            // Pass 1: 모든 노드의 GameObject + Transform 만 먼저 생성한다. SkinnedMeshRenderer 의
            // bones 배열은 _attach 시점에 모든 joint Transform 이 존재해야_ 구성 가능하므로 hierarchy
            // 생성과 mesh attach 를 두 단계로 분리한다.
            var nodeCount = document.Root.nodes?.Length ?? 0;
            var nodeTransforms = new Transform[nodeCount];
            var nodePaths = new string[nodeCount];
            for (var i = 0; i < scene.nodes.Length; ++i)
            {
                BuildNodeHierarchyRecursive(scene.nodes[i], rootObject.transform, string.Empty, nodeTransforms, nodePaths);
            }

            NodePaths = nodePaths;

            // Pass 2: 각 노드에 mesh / SkinnedMesh attach.
            for (var i = 0; i < scene.nodes.Length; ++i)
            {
                AttachMeshesRecursive(scene.nodes[i], nodeTransforms);
            }

            return rootObject;
        }

        private int ResolveSceneIndex()
        {
            if (document.Root.scene >= 0)
            {
                return document.Root.scene;
            }

            return document.Root.scenes != null && document.Root.scenes.Length > 0 ? 0 : -1;
        }

        private void BuildNodeHierarchyRecursive(
            int nodeIndex,
            Transform parent,
            string parentPath,
            Transform[] nodeTransforms,
            string[] nodePaths)
        {
            if (nodeIndex < 0 || nodeIndex >= nodeTransforms.Length)
            {
                return;
            }

            var node = document.Root.nodes[nodeIndex];
            var nodeName = string.IsNullOrEmpty(node?.name) ? $"GLTF_Node_{nodeIndex}" : node.name;
            var nodeObject = new GameObject(nodeName);
            nodeObject.transform.SetParent(parent, false);
            nodeTransforms[nodeIndex] = nodeObject.transform;
            // AnimationClip path 는 root 의 자식부터 시작하므로 root 의 직속 자식은 parentPath 가
            // 빈 문자열이어야 한다. 빈 path 와 자식 이름 사이에 "/" 를 끼우지 않도록 분기.
            nodePaths[nodeIndex] = string.IsNullOrEmpty(parentPath) ? nodeName : parentPath + "/" + nodeName;

            if (node == null)
            {
                return;
            }

            ApplyNodeTransform(nodeObject.transform, node);

            if (node.children != null)
            {
                for (var i = 0; i < node.children.Length; ++i)
                {
                    BuildNodeHierarchyRecursive(node.children[i], nodeObject.transform, nodePaths[nodeIndex], nodeTransforms, nodePaths);
                }
            }
        }

        private void AttachMeshesRecursive(int nodeIndex, Transform[] nodeTransforms)
        {
            if (nodeIndex < 0 || nodeIndex >= nodeTransforms.Length)
            {
                return;
            }

            var node = document.Root.nodes[nodeIndex];
            var nodeTransform = nodeTransforms[nodeIndex];
            if (node == null || nodeTransform == null)
            {
                return;
            }

            if (node.mesh >= 0)
            {
                var skin = ResolveSkin(node.skin);
                if (skin != null)
                {
                    AttachSkinnedMesh(nodeTransform.gameObject, node.mesh, node.skin, skin, nodeTransforms);
                }
                else
                {
                    AttachMesh(nodeTransform.gameObject, node.mesh);
                }
            }

            if (node.children != null)
            {
                for (var i = 0; i < node.children.Length; ++i)
                {
                    AttachMeshesRecursive(node.children[i], nodeTransforms);
                }
            }
        }

        private UMXGltfSkin ResolveSkin(int skinIndex)
        {
            if (skinIndex < 0 || document.Root.skins == null || skinIndex >= document.Root.skins.Length)
            {
                return null;
            }

            var skin = document.Root.skins[skinIndex];
            return (skin?.joints != null && skin.joints.Length > 0) ? skin : null;
        }

        /// <summary>
        /// glTF 노드의 변환은 matrix 또는 TRS 둘 중 하나로 표현된다(spec). matrix 가 있으면
        /// TRS 로 분해해 좌표계 변환 함수에 흘려 보낸다 — 이 단계에서 분해하면 이후 처리 경로가
        /// translation/rotation/scale 케이스와 동일해져 좌표계 변환(handedness, axis flip)이 일관된다.
        /// </summary>
        private static void ApplyNodeTransform(Transform target, UMXGltfNode node)
        {
            if (node.matrix != null && node.matrix.Length == 16)
            {
                DecomposeGltfMatrix(node.matrix, out var translation, out var rotation, out var scale);
                target.localPosition = UMXGltfCoordinateUtility.ToUnityTranslation(translation);
                target.localRotation = UMXGltfCoordinateUtility.ToUnityRotation(rotation);
                target.localScale = UMXGltfCoordinateUtility.ToUnityScale(scale);
                return;
            }

            target.localPosition = UMXGltfCoordinateUtility.ToUnityTranslation(node.translation);
            target.localRotation = UMXGltfCoordinateUtility.ToUnityRotation(node.rotation);
            target.localScale = UMXGltfCoordinateUtility.ToUnityScale(node.scale);
        }

        /// <summary>
        /// glTF 의 column-major 4x4 matrix 를 (translation, rotation, scale) 로 분해한다.
        /// 결과는 glTF 좌표계 그대로의 float 배열 — 이후 ToUnity{...} 헬퍼가 좌표계 변환을 담당.
        /// 음의 scale(반사)은 column 0 의 부호로 보존(Matrix4x4 가 음수 스케일을 정상 처리).
        /// </summary>
        private static void DecomposeGltfMatrix(float[] m, out float[] translation, out float[] rotation, out float[] scale)
        {
            // column-major: m[col*4 + row]
            translation = new[] { m[12], m[13], m[14] };

            var col0 = new Vector3(m[0], m[1], m[2]);
            var col1 = new Vector3(m[4], m[5], m[6]);
            var col2 = new Vector3(m[8], m[9], m[10]);

            var sx = col0.magnitude;
            var sy = col1.magnitude;
            var sz = col2.magnitude;

            // 좌수계 반사: determinant 가 음수면 한 축에 음수 스케일을 부여해 회전이 정상 분리되도록.
            var det =
                col0.x * (col1.y * col2.z - col1.z * col2.y) -
                col0.y * (col1.x * col2.z - col1.z * col2.x) +
                col0.z * (col1.x * col2.y - col1.y * col2.x);
            if (det < 0f)
            {
                sx = -sx;
            }

            scale = new[] { sx, sy, sz };

            if (Mathf.Abs(sx) < Mathf.Epsilon || Mathf.Abs(sy) < Mathf.Epsilon || Mathf.Abs(sz) < Mathf.Epsilon)
            {
                rotation = new[] { 0f, 0f, 0f, 1f };
                return;
            }

            // 회전 matrix = scale 로 normalize 한 column 0/1/2.
            var matrix = new Matrix4x4();
            matrix.SetColumn(0, new Vector4(col0.x / sx, col0.y / sx, col0.z / sx, 0f));
            matrix.SetColumn(1, new Vector4(col1.x / sy, col1.y / sy, col1.z / sy, 0f));
            matrix.SetColumn(2, new Vector4(col2.x / sz, col2.y / sz, col2.z / sz, 0f));
            matrix.SetColumn(3, new Vector4(0f, 0f, 0f, 1f));
            var q = matrix.rotation;
            rotation = new[] { q.x, q.y, q.z, q.w };
        }

        private void AttachMesh(GameObject nodeObject, int meshIndex)
        {
            if (meshIndex < 0 || meshIndex >= builtMeshes.Length)
            {
                return;
            }

            var builtMesh = builtMeshes[meshIndex];
            if (builtMesh == null || builtMesh.Primitives == null || builtMesh.Primitives.Count == 0)
            {
                return;
            }

            if (builtMesh.Primitives.Count == 1)
            {
                AttachPrimitive(nodeObject, builtMesh.Primitives[0]);
                return;
            }

            for (var i = 0; i < builtMesh.Primitives.Count; ++i)
            {
                var primitiveObject = new GameObject($"{builtMesh.Name}_Primitive_{i}");
                primitiveObject.transform.SetParent(nodeObject.transform, false);
                AttachPrimitive(primitiveObject, builtMesh.Primitives[i]);
            }
        }

        private static void AttachPrimitive(GameObject target, UMXGltfBuiltPrimitive primitive)
        {
            var meshFilter = target.AddComponent<MeshFilter>();
            var meshRenderer = target.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = primitive.Mesh;
            meshRenderer.sharedMaterial = primitive.Material;
        }

        /// <summary>
        /// node.skin 이 가리키는 skin 정보를 사용해 mesh 를 SkinnedMeshRenderer 로 부착한다.
        /// 첫 마일스톤은 "1 mesh : 1 skin" 가정이므로 mesh.bindposes 를 여기서 직접 설정한다.
        /// </summary>
        private void AttachSkinnedMesh(GameObject nodeObject, int meshIndex, int skinIndex, UMXGltfSkin skin, Transform[] nodeTransforms)
        {
            if (meshIndex < 0 || meshIndex >= builtMeshes.Length)
            {
                return;
            }

            var builtMesh = builtMeshes[meshIndex];
            if (builtMesh == null || builtMesh.Primitives == null || builtMesh.Primitives.Count == 0)
            {
                return;
            }

            if (meshIndexToSkinIndex.TryGetValue(meshIndex, out var existingSkinIndex))
            {
                if (existingSkinIndex != skinIndex)
                {
                    throw new NotSupportedException(
                        $"Mesh {meshIndex} is referenced by multiple skins (skin {existingSkinIndex} and {skinIndex}). The current importer only supports a single skin per mesh.");
                }
            }
            else
            {
                meshIndexToSkinIndex[meshIndex] = skinIndex;
            }

            var bones = ResolveBones(skin, nodeTransforms);
            var rootBone = ResolveRootBone(skin, nodeTransforms, bones);
            var bindposes = ResolveBindposes(skin, bones.Length);

            if (builtMesh.Primitives.Count == 1)
            {
                AttachSkinnedPrimitive(nodeObject, builtMesh.Primitives[0], bones, rootBone, bindposes);
                return;
            }

            for (var i = 0; i < builtMesh.Primitives.Count; ++i)
            {
                var primitiveObject = new GameObject($"{builtMesh.Name}_Primitive_{i}");
                primitiveObject.transform.SetParent(nodeObject.transform, false);
                AttachSkinnedPrimitive(primitiveObject, builtMesh.Primitives[i], bones, rootBone, bindposes);
            }
        }

        // SkinnedMeshRenderer.bones 에 null Transform 이 섞이면 해당 본의 정점이 무작위 위치로
        // 깨져 보이지만 에러가 나지 않는다. 인덱스 범위는 SupportedSubset 에서 검증되므로 여기서
        // 남는 실패 사유는 "scene 그래프에서 도달 불가능한 joint" 뿐 — 그 경우 즉시 throw 한다.
        private static Transform[] ResolveBones(UMXGltfSkin skin, Transform[] nodeTransforms)
        {
            var bones = new Transform[skin.joints.Length];
            for (var i = 0; i < skin.joints.Length; ++i)
            {
                var jointIndex = skin.joints[i];
                var jointTransform = (jointIndex >= 0 && jointIndex < nodeTransforms.Length)
                    ? nodeTransforms[jointIndex]
                    : null;
                if (jointTransform == null)
                {
                    throw new InvalidOperationException(
                        $"Skin joint[{i}] references node {jointIndex}, but no Transform was created for that node (joint is not reachable from the active scene root).");
                }

                bones[i] = jointTransform;
            }

            return bones;
        }

        private static Transform ResolveRootBone(UMXGltfSkin skin, Transform[] nodeTransforms, Transform[] bones)
        {
            if (skin.skeleton >= 0 && skin.skeleton < nodeTransforms.Length && nodeTransforms[skin.skeleton] != null)
            {
                return nodeTransforms[skin.skeleton];
            }

            return bones.Length > 0 ? bones[0] : null;
        }

        private Matrix4x4[] ResolveBindposes(UMXGltfSkin skin, int boneCount)
        {
            if (skin.inverseBindMatrices < 0)
            {
                // glTF 스펙: inverseBindMatrices 가 생략되면 모든 본의 IBM 은 identity.
                var identityArray = new Matrix4x4[boneCount];
                for (var i = 0; i < boneCount; ++i)
                {
                    identityArray[i] = Matrix4x4.identity;
                }

                return identityArray;
            }

            var raw = UMXGltfAccessorReader.ReadMatrix4x4Array(document, skin.inverseBindMatrices);
            var converted = new Matrix4x4[raw.Length];
            for (var i = 0; i < raw.Length; ++i)
            {
                converted[i] = UMXGltfCoordinateUtility.ToUnityBindpose(raw[i]);
            }

            return converted;
        }

        private static void AttachSkinnedPrimitive(
            GameObject target,
            UMXGltfBuiltPrimitive primitive,
            Transform[] bones,
            Transform rootBone,
            Matrix4x4[] bindposes)
        {
            primitive.Mesh.bindposes = bindposes;

            var renderer = target.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = primitive.Mesh;
            renderer.sharedMaterial = primitive.Material;
            renderer.bones = bones;
            renderer.rootBone = rootBone;
        }
    }
}
