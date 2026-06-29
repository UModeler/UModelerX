using System;
using UnityEngine;

namespace Tripolygon.UModelerX.Editor.Importers.GLTF
{
    /// <summary>
    /// GLB 바이트에서 메모리 전용으로 Mesh/Material/Animation 을 빌드한다.
    /// PureCode.Editor 어셈블리에 속하므로, Editor 어셈블리에서는 delegate를 통해 호출한다.
    /// </summary>
    internal static class UMXGltfMemoryLoader
    {
        /// <summary>
        /// GLB 바이트를 메모리에서 파싱하여 Mesh, Material, Rotation, 그리고 가능한 경우
        /// 애니메이션을 sample 하기 위한 root GameObject 와 <see cref="AnimationClip"/> 배열을 반환한다.
        /// 애니메이션이 없으면 <c>animatedPrefab</c>, <c>animationClips</c> 는 null.
        /// </summary>
        /// <returns>성공 시 (mesh, material, rotation, animatedPrefab?, animationClips?), 실패 시 모두 null.</returns>
        internal static (Mesh mesh, Material material, Quaternion rotation, GameObject animatedPrefab, AnimationClip[] animationClips) LoadFromGlbBytes(byte[] glbBytes)
        {
            try
            {
                var document = UMXGltfDocumentLoader.LoadFromBytes(glbBytes, "preview");
                var context = new UMXGltfAssetContext();
                var materialBuilder = new UMXGltfMaterialBuilder(document, context);
                var meshBuilder = new UMXGltfMeshBuilder(document, context, materialBuilder);
                var builtMeshes = meshBuilder.BuildAll();
                var nodeBuilder = new UMXGltfNodeBuilder(document, builtMeshes);
                var rootObject = nodeBuilder.BuildSceneRoot();
                rootObject.hideFlags = HideFlags.HideAndDontSave;

                // 애니메이션 빌드. preview 가 instantiate 한 clone 에 대해 직접 SampleAnimation 을
                // 호출하므로 root 에 Animation 컴포넌트를 부착하지는 않는다.
                var animationBuilder = new UMXGltfAnimationBuilder(document, context, nodeBuilder.NodePaths);
                var clips = animationBuilder.BuildAll();

                // 정적 미리보기용 mesh + material + 메쉬 노드의 누적 world rotation 추출.
                // meshXform.rotation 은 rootObject(identity) 기준 누적 회전이므로 glTF scene-graph
                // 상위 노드에 baked 된 axis 보정(Hunyuan 처럼 mesh node 에 270° X 가 들어가
                // Z-up→Y-up 으로 정규화하는 패턴)을 보존한다.
                Mesh mesh = null;
                Material material = null;
                Transform meshXform = null;
                var meshFilter = rootObject.GetComponentInChildren<MeshFilter>();
                if (meshFilter != null)
                {
                    mesh = meshFilter.sharedMesh;
                    meshXform = meshFilter.transform;
                    var renderer = rootObject.GetComponentInChildren<MeshRenderer>();
                    material = renderer != null ? renderer.sharedMaterial : null;
                }
                if (mesh == null)
                {
                    var skinned = rootObject.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (skinned != null)
                    {
                        mesh = skinned.sharedMesh;
                        material = skinned.sharedMaterial;
                        meshXform = skinned.transform;
                    }
                }

                if (mesh == null || material == null)
                {
                    UnityEngine.Object.DestroyImmediate(rootObject);
                    return (null, null, Quaternion.identity, null, null);
                }

                mesh.hideFlags = HideFlags.HideAndDontSave;
                material.hideFlags = HideFlags.HideAndDontSave;
                // rotation 의 의미: "raw mesh 를 정면(prefab-frame)으로 세우는 누적 회전".
                // 엔진별 정면 보정(Tripo 의 -X-facing 등)은 caller 가 별도로 곱한다.
                // 애니메이션 경로는 hierarchy 가 이 회전을 이미 포함하므로 caller 가
                // animation root 에 다시 적용하지 않는다.
                var rotation = meshXform != null ? meshXform.rotation : Quaternion.identity;

                // 애니메이션이 없으면 root 정리 (mesh/material 만 유지). 메모리 누수 방지.
                if (clips == null || clips.Length == 0)
                {
                    UnityEngine.Object.DestroyImmediate(rootObject);
                    return (mesh, material, rotation, null, null);
                }

                // 애니메이션이 있으면 root + 자식 hierarchy 를 보존해 caller 가 instantiate 해 sample
                // 할 수 있게 한다. clips 도 동일하게 HideAndDontSave 로 표시.
                SetHideFlagsRecursive(rootObject, HideFlags.HideAndDontSave);
                for (var i = 0; i < clips.Length; ++i)
                {
                    if (clips[i] != null)
                    {
                        clips[i].hideFlags = HideFlags.HideAndDontSave;
                    }
                }

                return (mesh, material, rotation, rootObject, clips);
            }
            catch (Exception)
            {
                return (null, null, Quaternion.identity, null, null);
            }
        }

        private static void SetHideFlagsRecursive(GameObject go, HideFlags flags)
        {
            go.hideFlags = flags;
            var transform = go.transform;
            for (var i = 0; i < transform.childCount; ++i)
            {
                SetHideFlagsRecursive(transform.GetChild(i).gameObject, flags);
            }
        }
    }
}
