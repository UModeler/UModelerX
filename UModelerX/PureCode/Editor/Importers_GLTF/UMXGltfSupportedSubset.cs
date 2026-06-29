using System;
using System.Collections.Generic;

namespace Tripolygon.UModelerX.Editor.Importers.GLTF
{
    internal static class UMXGltfSupportedSubset
    {
        public const int FloatComponentType = 5126;
        public const int UnsignedByteComponentType = 5121;
        public const int UnsignedShortComponentType = 5123;
        public const int UnsignedIntComponentType = 5125;

        public const int TrianglesMode = 4;

        public static readonly string[] UnsupportedFeatures =
        {
            "morph targets",
            "Draco compression",
            "KTX2",
            "sparse accessors",
            "glTF extensions",
            "CUBICSPLINE animation interpolation",
            "morph weight animation"
        };

        public static void ValidateOrThrow(UMXGltfRoot root)
        {
            if (root == null)
            {
                throw new InvalidOperationException("The glTF document could not be parsed.");
            }

            var issues = CollectIssues(root);
            if (issues.Count > 0)
            {
                throw new NotSupportedException(string.Join(Environment.NewLine, issues));
            }
        }

        public static List<string> CollectIssues(UMXGltfRoot root)
        {
            var issues = new List<string>();

            // glTF 2.0 스펙: extensionsRequired 에 있는 확장만 클라이언트가 반드시 지원해야 하고,
            // extensionsUsed 에만 있는 옵셔널 확장은 무시해도 된다. 이 importer 는 확장 데이터를
            // 전혀 읽지 않으므로 옵셔널 확장은 그대로 통과시킨다.
            if (root.extensionsRequired != null && root.extensionsRequired.Length > 0)
            {
                issues.Add("Required glTF extensions are not supported in the first importer milestone.");
            }

            if (root.accessors != null)
            {
                for (var i = 0; i < root.accessors.Length; ++i)
                {
                    var accessor = root.accessors[i];
                    if (accessor == null)
                    {
                        continue;
                    }

                    if (accessor.sparse != null && accessor.sparse.count > 0)
                    {
                        issues.Add($"Accessor {i} uses sparse data, which is not supported.");
                    }
                }
            }

            // node.matrix 는 UMXGltfNodeBuilder 에서 TRS 로 분해해 적용하므로 거부 대상이 아니다.

            if (root.meshes != null)
            {
                for (var meshIndex = 0; meshIndex < root.meshes.Length; ++meshIndex)
                {
                    var mesh = root.meshes[meshIndex];
                    if (mesh?.primitives == null)
                    {
                        continue;
                    }

                    for (var primitiveIndex = 0; primitiveIndex < mesh.primitives.Length; ++primitiveIndex)
                    {
                        var primitive = mesh.primitives[primitiveIndex];
                        if (primitive == null)
                        {
                            continue;
                        }

                        if (primitive.mode != TrianglesMode)
                        {
                            issues.Add($"Mesh {meshIndex} primitive {primitiveIndex} uses draw mode {primitive.mode}, but only triangles are supported.");
                        }

                        if (primitive.targets != null && primitive.targets.Length > 0)
                        {
                            issues.Add($"Mesh {meshIndex} primitive {primitiveIndex} uses morph targets, which are not supported.");
                        }

                        if (primitive.attributes == null || primitive.attributes.POSITION < 0)
                        {
                            issues.Add($"Mesh {meshIndex} primitive {primitiveIndex} is missing POSITION.");
                        }
                        else
                        {
                            // JOINTS_0 / WEIGHTS_0 는 짝으로 존재해야 한다 — 한쪽만 있으면 정의되지 않은 상태.
                            var hasJoints = primitive.attributes.JOINTS_0 >= 0;
                            var hasWeights = primitive.attributes.WEIGHTS_0 >= 0;
                            if (hasJoints != hasWeights)
                            {
                                issues.Add($"Mesh {meshIndex} primitive {primitiveIndex} has only one of JOINTS_0/WEIGHTS_0; both must be present together.");
                            }
                        }
                    }
                }
            }

            // node.skin 참조 무결성 검증.
            if (root.nodes != null)
            {
                for (var i = 0; i < root.nodes.Length; ++i)
                {
                    var node = root.nodes[i];
                    if (node == null || node.skin < 0)
                    {
                        continue;
                    }

                    if (root.skins == null || node.skin >= root.skins.Length || root.skins[node.skin] == null)
                    {
                        issues.Add($"Node {i} references skin {node.skin}, but no matching skin is defined.");
                        continue;
                    }

                    var skin = root.skins[node.skin];
                    if (skin.joints == null || skin.joints.Length == 0)
                    {
                        issues.Add($"Skin {node.skin} (referenced by node {i}) has no joints.");
                        continue;
                    }

                    var nodeCount = root.nodes.Length;
                    for (var j = 0; j < skin.joints.Length; ++j)
                    {
                        var jointIndex = skin.joints[j];
                        if (jointIndex < 0 || jointIndex >= nodeCount)
                        {
                            issues.Add($"Skin {node.skin} joint[{j}] references node {jointIndex}, which is out of range [0, {nodeCount}).");
                        }
                    }
                }
            }

            ValidateAnimations(root, issues);

            return issues;
        }

        // 첫 마일스톤: LINEAR/STEP 만 지원. CUBICSPLINE 은 in/out tangent 데이터 형식이 다르고
        // rotation 의 경우 정규화까지 필요해 별도 처리 — 추후 지원. morph weights 채널은 morph
        // targets 자체가 미지원이므로 함께 거부한다.
        private static void ValidateAnimations(UMXGltfRoot root, List<string> issues)
        {
            if (root.animations == null || root.animations.Length == 0)
            {
                return;
            }

            var nodeCount = root.nodes?.Length ?? 0;

            for (var a = 0; a < root.animations.Length; ++a)
            {
                var animation = root.animations[a];
                if (animation == null)
                {
                    continue;
                }

                var samplerCount = animation.samplers?.Length ?? 0;

                if (animation.samplers != null)
                {
                    for (var s = 0; s < animation.samplers.Length; ++s)
                    {
                        var sampler = animation.samplers[s];
                        if (sampler == null)
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(sampler.interpolation)
                            && sampler.interpolation != "LINEAR"
                            && sampler.interpolation != "STEP")
                        {
                            issues.Add($"Animation {a} sampler {s} uses interpolation '{sampler.interpolation}', which is not supported (only LINEAR/STEP).");
                        }
                    }
                }

                if (animation.channels == null)
                {
                    continue;
                }

                for (var c = 0; c < animation.channels.Length; ++c)
                {
                    var channel = animation.channels[c];
                    if (channel == null || channel.target == null)
                    {
                        continue;
                    }

                    if (channel.sampler < 0 || channel.sampler >= samplerCount)
                    {
                        issues.Add($"Animation {a} channel {c} references sampler {channel.sampler}, which is out of range [0, {samplerCount}).");
                    }

                    if (channel.target.node < 0 || channel.target.node >= nodeCount)
                    {
                        issues.Add($"Animation {a} channel {c} targets node {channel.target.node}, which is out of range [0, {nodeCount}).");
                    }

                    if (channel.target.path != "translation"
                        && channel.target.path != "rotation"
                        && channel.target.path != "scale")
                    {
                        issues.Add($"Animation {a} channel {c} targets path '{channel.target.path}', which is not supported (only translation/rotation/scale).");
                    }
                }
            }
        }
    }
}
