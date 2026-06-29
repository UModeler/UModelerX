using System;

using UnityEditor.AssetImporters;
using UnityEngine;

namespace Tripolygon.UModelerX.Editor.Importers.GLTF
{
#if !GLTFAST_PRESENT
    [ScriptedImporter(1, new[] { "gltf", "glb" })]
    internal sealed class UMXGltfImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            try
            {
                var document = UMXGltfDocumentLoader.Load(ctx.assetPath);
                var assetContext = new UMXGltfAssetContext(ctx);
                var materialBuilder = new UMXGltfMaterialBuilder(document, assetContext);
                var meshBuilder = new UMXGltfMeshBuilder(document, assetContext, materialBuilder);
                var builtMeshes = meshBuilder.BuildAll();
                var nodeBuilder = new UMXGltfNodeBuilder(document, builtMeshes);
                var rootObject = nodeBuilder.BuildSceneRoot();

                var animationBuilder = new UMXGltfAnimationBuilder(document, assetContext, nodeBuilder.NodePaths);
                var animationClips = animationBuilder.BuildAll();
                AttachAnimationComponent(rootObject, animationClips);

                assetContext.AddObject("main", rootObject);
                ctx.SetMainObject(rootObject);
            }
            catch (Exception exception)
            {
                var errorObject = new GameObject("GLTF Import Failed");
                var assetContext = new UMXGltfAssetContext(ctx);
                assetContext.AddObject("main_error", errorObject);
                ctx.LogImportError(exception.ToString());
                ctx.SetMainObject(errorObject);
            }
        }

        // 첫 마일스톤은 AnimatorController 생성 부담을 피하기 위해 legacy Animation 컴포넌트 사용.
        // Animation 컴포넌트의 path 기반 curve binding 은 glTF channel 의 (node, path) 모델과 자연스럽게
        // 매칭된다. clip.legacy = true 인 clip 만 AddClip 가능 — AnimationBuilder 에서 보장.
        private static void AttachAnimationComponent(GameObject rootObject, AnimationClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            var animationComponent = rootObject.AddComponent<Animation>();
            foreach (var clip in clips)
            {
                if (clip == null)
                {
                    continue;
                }

                animationComponent.AddClip(clip, clip.name);
            }

            animationComponent.clip = clips[0];
            animationComponent.playAutomatically = false;
        }
    }
#endif
}
