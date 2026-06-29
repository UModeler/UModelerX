using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

namespace Tripolygon.UModelerX.Editor.Importers.GLTF
{
    internal sealed class UMXGltfMaterialBuilder
    {
        private readonly UMXGltfAssetContext assetContext;
        private readonly UMXGltfLoadedDocument document;
        private readonly Material defaultMaterial;
        private readonly Material[] materials;
        private readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

        public UMXGltfMaterialBuilder(UMXGltfLoadedDocument loadedDocument, UMXGltfAssetContext context)
        {
            document = loadedDocument;
            assetContext = context;
            defaultMaterial = CreateDefaultMaterial();
            materials = BuildMaterials();
        }

        public Material GetMaterial(int materialIndex)
        {
            if (materialIndex < 0 || materials.Length == 0 || materialIndex >= materials.Length)
            {
                return defaultMaterial;
            }

            return materials[materialIndex] ?? defaultMaterial;
        }

        private Material[] BuildMaterials()
        {
            if (document.Root.materials == null || document.Root.materials.Length == 0)
            {
                assetContext.AddObject("material_default", defaultMaterial);
                return Array.Empty<Material>();
            }

            var result = new Material[document.Root.materials.Length];
            for (var i = 0; i < result.Length; ++i)
            {
                result[i] = CreateMaterial(document.Root.materials[i], i);
            }

            assetContext.AddObject("material_default", defaultMaterial);
            return result;
        }

        private Material CreateDefaultMaterial()
        {
            var shader = ResolveShader();
            var material = new Material(shader);
            material.name = "GLTF_Default";
            SetColorIfPresent(material, new[] { "_BaseColor", "_Color" }, Color.white);
            return material;
        }

        private Material CreateMaterial(UMXGltfMaterial source, int index)
        {
            var shader = ResolveShader();
            var material = new Material(shader);
            material.name = string.IsNullOrEmpty(source?.name) ? $"GLTF_Material_{index}" : source.name;

            if (source == null)
            {
                assetContext.AddObject($"material_{index}", material);
                return material;
            }

            var pbr = source.pbrMetallicRoughness ?? new UMXGltfPbrMetallicRoughness();

            var baseColor = ReadColorFactor(pbr.baseColorFactor, Color.white);
            SetColorIfPresent(material, new[] { "_BaseColor", "_Color" }, baseColor);

            if (pbr.baseColorTexture != null && pbr.baseColorTexture.index >= 0)
            {
                var albedoTexture = GetTextureFromTextureInfo(pbr.baseColorTexture, false, $"basecolor_{index}");
                SetTextureIfPresent(material, new[] { "_BaseMap", "_MainTex", "_BaseColorMap" }, albedoTexture);
            }

            SetFloatIfPresent(material, new[] { "_Metallic" }, pbr.metallicFactor);
            SetFloatIfPresent(material, new[] { "_Smoothness", "_Glossiness" }, 1.0f - pbr.roughnessFactor);

            if ((pbr.metallicRoughnessTexture != null && pbr.metallicRoughnessTexture.index >= 0) ||
                (source.occlusionTexture != null && source.occlusionTexture.index >= 0))
            {
                var maskTextures = BuildPackedMaskTextures(
                    pbr.metallicRoughnessTexture,
                    source.occlusionTexture,
                    $"mask_{index}");

                if (maskTextures.metallicGloss != null)
                {
                    SetTextureIfPresent(material, new[] { "_MetallicGlossMap", "_MaskMap" }, maskTextures.metallicGloss);
                    material.EnableKeyword("_METALLICGLOSSMAP");
                }

                if (maskTextures.occlusion != null)
                {
                    SetTextureIfPresent(material, new[] { "_OcclusionMap" }, maskTextures.occlusion);
                    SetFloatIfPresent(material, new[] { "_OcclusionStrength" }, source.occlusionTexture?.strength ?? 1.0f);
                    material.EnableKeyword("_OCCLUSIONMAP");
                }
            }

            if (source.normalTexture != null && source.normalTexture.index >= 0)
            {
                var normalTexture = GetTextureFromTextureInfo(source.normalTexture, true, $"normal_{index}");
                SetTextureIfPresent(material, new[] { "_BumpMap", "_NormalMap" }, normalTexture);
                SetFloatIfPresent(material, new[] { "_BumpScale", "_NormalScale" }, source.normalTexture.scale);
                material.EnableKeyword("_NORMALMAP");
            }

            var emissiveColor = ReadColorFactor(source.emissiveFactor, Color.black);
            if (emissiveColor.maxColorComponent > 0.0f || (source.emissiveTexture != null && source.emissiveTexture.index >= 0))
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                SetColorIfPresent(material, new[] { "_EmissionColor", "_EmissiveColor" }, emissiveColor);
            }

            if (source.emissiveTexture != null && source.emissiveTexture.index >= 0)
            {
                var emissiveTexture = GetTextureFromTextureInfo(source.emissiveTexture, false, $"emissive_{index}");
                SetTextureIfPresent(material, new[] { "_EmissionMap", "_EmissiveColorMap" }, emissiveTexture);
            }

            ConfigureAlphaMode(material, source.alphaMode, source.alphaCutoff);
            material.doubleSidedGI = source.doubleSided;
            SetIntIfPresent(material, new[] { "_Cull", "_CullMode" }, source.doubleSided ? (int)CullMode.Off : (int)CullMode.Back);

            assetContext.AddObject($"material_{index}", material);
            return material;
        }

        private struct PackedMaskResult
        {
            public Texture2D metallicGloss;
            public Texture2D occlusion;
        }

        private PackedMaskResult BuildPackedMaskTextures(
            UMXGltfTextureInfo metallicRoughnessTextureInfo,
            UMXGltfOcclusionTextureInfo occlusionTextureInfo,
            string cacheKey)
        {
            var result = new PackedMaskResult();

            var metallicTexture = metallicRoughnessTextureInfo != null && metallicRoughnessTextureInfo.index >= 0
                ? GetTextureFromTextureInfo(metallicRoughnessTextureInfo, true, $"{cacheKey}_metalrough")
                : null;
            var occlusionTexture = occlusionTextureInfo != null && occlusionTextureInfo.index >= 0
                ? GetTextureFromTextureInfo(occlusionTextureInfo, true, $"{cacheKey}_occlusion")
                : null;

            // MetallicGlossMap: R=Metallic(glTF B), A=Smoothness(1-glTF G)
            if (metallicTexture != null)
            {
                var mgKey = $"{cacheKey}_metallicgloss";
                if (textureCache.TryGetValue(mgKey, out var cachedMG))
                {
                    result.metallicGloss = cachedMG;
                }
                else
                {
                    var width = metallicTexture.width;
                    var height = metallicTexture.height;
                    var mgTex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                    var srcPixels = metallicTexture.GetPixels32();
                    var mgPixels = new Color32[srcPixels.Length];

                    for (var i = 0; i < srcPixels.Length; ++i)
                    {
                        var metallic = srcPixels[i].b;
                        var smoothness = (byte)(255 - srcPixels[i].g);
                        mgPixels[i] = new Color32(metallic, metallic, metallic, smoothness);
                    }

                    mgTex.SetPixels32(mgPixels);
                    mgTex.Apply(false, false);
                    mgTex.name = mgKey;
                    textureCache[mgKey] = mgTex;
                    assetContext.AddObject(mgKey, mgTex);
                    result.metallicGloss = mgTex;
                }
            }

            // OcclusionMap: 별도 occlusionTexture가 명시된 경우에만 생성.
            // metallicRoughnessTexture의 R 채널은 glTF 규격상 undefined이므로 fallback하지 않는다.
            if (occlusionTexture != null)
            {
                var occKey = $"{cacheKey}_occ";
                if (textureCache.TryGetValue(occKey, out var cachedOcc))
                {
                    result.occlusion = cachedOcc;
                }
                else
                {
                    var width = occlusionTexture.width;
                    var height = occlusionTexture.height;
                    var occTex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                    var srcPixels = occlusionTexture.GetPixels32();
                    var occPixels = new Color32[srcPixels.Length];

                    for (var i = 0; i < srcPixels.Length; ++i)
                    {
                        var occ = srcPixels[i].r;
                        occPixels[i] = new Color32(occ, occ, occ, 255);
                    }

                    occTex.SetPixels32(occPixels);
                    occTex.Apply(false, false);
                    occTex.name = occKey;
                    textureCache[occKey] = occTex;
                    assetContext.AddObject(occKey, occTex);
                    result.occlusion = occTex;
                }
            }

            return result;
        }

        private Texture2D GetTextureFromTextureInfo(UMXGltfTextureInfo textureInfo, bool linear, string cacheKey)
        {
            if (textureInfo == null || textureInfo.index < 0)
            {
                return null;
            }

            var linearSuffix = linear ? "_linear" : "_srgb";
            var finalKey = $"{cacheKey}{linearSuffix}";
            if (textureCache.TryGetValue(finalKey, out var cached))
            {
                return cached;
            }

            var root = document.Root;
            if (root.textures == null || textureInfo.index >= root.textures.Length)
            {
                return null;
            }

            var texture = root.textures[textureInfo.index];
            if (texture == null || texture.source < 0 || root.images == null || texture.source >= root.images.Length)
            {
                return null;
            }

            var imageBytes = document.GetImageBytes(texture.source);
            var unityTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear);
            unityTexture.name = string.IsNullOrEmpty(texture.name) ? finalKey : texture.name;
            if (!unityTexture.LoadImage(imageBytes, false))
            {
                UnityEngine.Object.DestroyImmediate(unityTexture);
                return null;
            }

            ApplySampler(texture, unityTexture);
            textureCache[finalKey] = unityTexture;
            assetContext.AddObject(finalKey, unityTexture);
            return unityTexture;
        }

        private void ApplySampler(UMXGltfTexture texture, Texture2D unityTexture)
        {
            if (texture == null || unityTexture == null)
            {
                return;
            }

            var root = document.Root;
            if (root.samplers == null || texture.sampler < 0 || texture.sampler >= root.samplers.Length)
            {
                return;
            }

            var sampler = root.samplers[texture.sampler];
            if (sampler == null)
            {
                return;
            }

            unityTexture.wrapModeU = ConvertWrapMode(sampler.wrapS);
            unityTexture.wrapModeV = ConvertWrapMode(sampler.wrapT);
            unityTexture.filterMode = ConvertFilterMode(sampler.minFilter, sampler.magFilter);
        }

        private Shader ResolveShader()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                return Shader.Find("Standard");
            }

            var candidates = new[]
            {
                "Universal Render Pipeline/Lit",
                "HDRP/Lit",
                "Standard"
            };

            for (var i = 0; i < candidates.Length; ++i)
            {
                var shader = Shader.Find(candidates[i]);
                if (shader != null)
                {
                    return shader;
                }
            }

            throw new InvalidOperationException("A compatible Lit shader could not be resolved for glTF material import.");
        }

        private static TextureWrapMode ConvertWrapMode(int wrapMode)
        {
            return wrapMode switch
            {
                33071 => TextureWrapMode.Clamp,
                33648 => TextureWrapMode.Mirror,
                _ => TextureWrapMode.Repeat
            };
        }

        private static FilterMode ConvertFilterMode(int minFilter, int magFilter)
        {
            if (minFilter == 9728 || magFilter == 9728)
            {
                return FilterMode.Point;
            }

            if (minFilter == 9729 || magFilter == 9729)
            {
                return FilterMode.Bilinear;
            }

            return FilterMode.Trilinear;
        }

        private static Color ReadColorFactor(float[] values, Color fallback)
        {
            if (values == null || values.Length == 0)
            {
                return fallback;
            }

            if (values.Length == 3)
            {
                return new Color(values[0], values[1], values[2], fallback.a);
            }

            if (values.Length >= 4)
            {
                return new Color(values[0], values[1], values[2], values[3]);
            }

            return fallback;
        }

        private static void ConfigureAlphaMode(Material material, string alphaMode, float alphaCutoff)
        {
            var mode = string.IsNullOrEmpty(alphaMode) ? "OPAQUE" : alphaMode.ToUpperInvariant();

            if (mode == "MASK")
            {
                SetFloatIfPresent(material, new[] { "_AlphaClip", "_AlphaCutoffEnable" }, 1.0f);
                SetFloatIfPresent(material, new[] { "_Cutoff", "_AlphaCutoff" }, alphaCutoff);
                material.EnableKeyword("_ALPHATEST_ON");
                return;
            }

            if (mode == "BLEND")
            {
                SetFloatIfPresent(material, new[] { "_Surface", "_SurfaceType" }, 1.0f);
                SetIntIfPresent(material, new[] { "_SrcBlend" }, (int)BlendMode.SrcAlpha);
                SetIntIfPresent(material, new[] { "_DstBlend" }, (int)BlendMode.OneMinusSrcAlpha);
                SetIntIfPresent(material, new[] { "_ZWrite" }, 0);
                material.renderQueue = (int)RenderQueue.Transparent;
                return;
            }

            SetFloatIfPresent(material, new[] { "_Surface", "_SurfaceType" }, 0.0f);
            SetIntIfPresent(material, new[] { "_SrcBlend" }, (int)BlendMode.One);
            SetIntIfPresent(material, new[] { "_DstBlend" }, (int)BlendMode.Zero);
            SetIntIfPresent(material, new[] { "_ZWrite" }, 1);
        }

        private static void SetColorIfPresent(Material material, string[] propertyNames, Color value)
        {
            for (var i = 0; i < propertyNames.Length; ++i)
            {
                var propertyName = propertyNames[i];
                if (material.HasProperty(propertyName))
                {
                    material.SetColor(propertyName, value);
                    return;
                }
            }
        }

        private static void SetFloatIfPresent(Material material, string[] propertyNames, float value)
        {
            for (var i = 0; i < propertyNames.Length; ++i)
            {
                var propertyName = propertyNames[i];
                if (material.HasProperty(propertyName))
                {
                    material.SetFloat(propertyName, value);
                }
            }
        }

        private static void SetIntIfPresent(Material material, string[] propertyNames, int value)
        {
            for (var i = 0; i < propertyNames.Length; ++i)
            {
                var propertyName = propertyNames[i];
                if (material.HasProperty(propertyName))
                {
                    material.SetInt(propertyName, value);
                }
            }
        }

        private static void SetTextureIfPresent(Material material, string[] propertyNames, Texture texture)
        {
            if (texture == null)
            {
                return;
            }

            for (var i = 0; i < propertyNames.Length; ++i)
            {
                var propertyName = propertyNames[i];
                if (material.HasProperty(propertyName))
                {
                    material.SetTexture(propertyName, texture);
                }
            }
        }
    }
}
