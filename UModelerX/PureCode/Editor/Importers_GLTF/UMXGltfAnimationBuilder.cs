using System;

using UnityEngine;

namespace Tripolygon.UModelerX.Editor.Importers.GLTF
{
    internal sealed class UMXGltfAnimationBuilder
    {
        private enum SamplerInterpolation
        {
            Linear,
            Step
        }

        private readonly UMXGltfAssetContext assetContext;
        private readonly UMXGltfLoadedDocument document;
        private readonly string[] nodePaths;

        public UMXGltfAnimationBuilder(
            UMXGltfLoadedDocument loadedDocument,
            UMXGltfAssetContext context,
            string[] paths)
        {
            document = loadedDocument;
            assetContext = context;
            nodePaths = paths ?? Array.Empty<string>();
        }

        public AnimationClip[] BuildAll()
        {
            var animations = document.Root.animations;
            if (animations == null || animations.Length == 0)
            {
                return Array.Empty<AnimationClip>();
            }

            var result = new AnimationClip[animations.Length];
            for (var i = 0; i < animations.Length; ++i)
            {
                result[i] = BuildAnimation(animations[i], i);
            }

            return result;
        }

        private AnimationClip BuildAnimation(UMXGltfAnimation animation, int animationIndex)
        {
            var clip = new AnimationClip
            {
                name = string.IsNullOrEmpty(animation?.name) ? $"GLTF_Animation_{animationIndex}" : animation.name,
                // 첫 마일스톤은 Animation 컴포넌트(legacy) 로 부착하므로 clip 도 legacy 모드.
                legacy = true
            };

            if (animation?.channels != null)
            {
                for (var c = 0; c < animation.channels.Length; ++c)
                {
                    BuildChannelCurves(clip, animation, animation.channels[c]);
                }
            }

            // 컴포넌트별 LERP 가 quaternion 의 부호 불연속에서 짧은 호 대신 긴 호로 도는 것을 방지.
            clip.EnsureQuaternionContinuity();

            assetContext.AddObject(clip.name, clip);
            return clip;
        }

        private void BuildChannelCurves(AnimationClip clip, UMXGltfAnimation animation, UMXGltfAnimationChannel channel)
        {
            if (channel?.target == null || channel.sampler < 0)
            {
                return;
            }

            if (channel.sampler >= (animation.samplers?.Length ?? 0))
            {
                return;
            }

            var nodeIndex = channel.target.node;
            if (nodeIndex < 0 || nodeIndex >= nodePaths.Length)
            {
                return;
            }

            var path = nodePaths[nodeIndex];
            // path == null 은 "scene 그래프에 도달 불가능한 노드를 애니메이트". 시각적 영향이
            // 없으므로 silent skip (validate 단계에서 path 자체는 거부하지 않는다).
            if (path == null)
            {
                return;
            }

            var sampler = animation.samplers[channel.sampler];
            if (sampler == null || sampler.input < 0 || sampler.output < 0)
            {
                return;
            }

            var times = UMXGltfAccessorReader.ReadFloatScalar(document, sampler.input);
            if (times.Length == 0)
            {
                return;
            }

            var interpolation = ParseInterpolation(sampler.interpolation);

            switch (channel.target.path)
            {
                case "translation":
                    BuildTranslationCurves(clip, path, times, sampler.output, interpolation);
                    break;
                case "rotation":
                    BuildRotationCurves(clip, path, times, sampler.output, interpolation);
                    break;
                case "scale":
                    BuildScaleCurves(clip, path, times, sampler.output, interpolation);
                    break;
            }
        }

        private void BuildTranslationCurves(AnimationClip clip, string path, float[] times, int outputAccessor, SamplerInterpolation interp)
        {
            var values = UMXGltfAccessorReader.ReadVector3(document, outputAccessor);
            if (values.Length != times.Length)
            {
                throw new InvalidOperationException(
                    $"Animation translation sampler output length ({values.Length}) does not match input length ({times.Length}).");
            }

            // glTF -> Unity 좌표계: z 부호 반전.
            var xs = new float[values.Length];
            var ys = new float[values.Length];
            var zs = new float[values.Length];
            for (var i = 0; i < values.Length; ++i)
            {
                var converted = UMXGltfCoordinateUtility.ToUnityPosition(values[i]);
                xs[i] = converted.x;
                ys[i] = converted.y;
                zs[i] = converted.z;
            }

            clip.SetCurve(path, typeof(Transform), "localPosition.x", BuildCurve(times, xs, interp));
            clip.SetCurve(path, typeof(Transform), "localPosition.y", BuildCurve(times, ys, interp));
            clip.SetCurve(path, typeof(Transform), "localPosition.z", BuildCurve(times, zs, interp));
        }

        private void BuildRotationCurves(AnimationClip clip, string path, float[] times, int outputAccessor, SamplerInterpolation interp)
        {
            var values = UMXGltfAccessorReader.ReadVector4(document, outputAccessor);
            if (values.Length != times.Length)
            {
                throw new InvalidOperationException(
                    $"Animation rotation sampler output length ({values.Length}) does not match input length ({times.Length}).");
            }

            // glTF rotation (x,y,z,w) -> Unity (-x,-y,z,w). ToUnityRotation 과 동일 변환을 component
            // 단위로 적용한다.
            var xs = new float[values.Length];
            var ys = new float[values.Length];
            var zs = new float[values.Length];
            var ws = new float[values.Length];
            for (var i = 0; i < values.Length; ++i)
            {
                xs[i] = -values[i].x;
                ys[i] = -values[i].y;
                zs[i] = values[i].z;
                ws[i] = values[i].w;
            }

            clip.SetCurve(path, typeof(Transform), "localRotation.x", BuildCurve(times, xs, interp));
            clip.SetCurve(path, typeof(Transform), "localRotation.y", BuildCurve(times, ys, interp));
            clip.SetCurve(path, typeof(Transform), "localRotation.z", BuildCurve(times, zs, interp));
            clip.SetCurve(path, typeof(Transform), "localRotation.w", BuildCurve(times, ws, interp));
        }

        private void BuildScaleCurves(AnimationClip clip, string path, float[] times, int outputAccessor, SamplerInterpolation interp)
        {
            var values = UMXGltfAccessorReader.ReadVector3(document, outputAccessor);
            if (values.Length != times.Length)
            {
                throw new InvalidOperationException(
                    $"Animation scale sampler output length ({values.Length}) does not match input length ({times.Length}).");
            }

            // scale 은 좌표계 변환 영향을 받지 않는다 (ToUnityScale 도 부호 반전 없음).
            var xs = new float[values.Length];
            var ys = new float[values.Length];
            var zs = new float[values.Length];
            for (var i = 0; i < values.Length; ++i)
            {
                xs[i] = values[i].x;
                ys[i] = values[i].y;
                zs[i] = values[i].z;
            }

            clip.SetCurve(path, typeof(Transform), "localScale.x", BuildCurve(times, xs, interp));
            clip.SetCurve(path, typeof(Transform), "localScale.y", BuildCurve(times, ys, interp));
            clip.SetCurve(path, typeof(Transform), "localScale.z", BuildCurve(times, zs, interp));
        }

        /// <summary>
        /// keyframe 의 tangent 를 보간 방식에 맞춰 직접 계산해 <see cref="AnimationCurve"/> 를 만든다.
        /// AnimationUtility 의존을 피하기 위해 <see cref="Keyframe.inTangent"/> / <see cref="Keyframe.outTangent"/>
        /// 를 직접 채운다.
        /// - LINEAR: 인접 keyframe 의 (Δvalue / Δtime) 을 양쪽 tangent 로 공유.
        /// - STEP: 양쪽 tangent 를 +∞ 로 두어 segment 내부에서 값이 변하지 않게 한다.
        /// 양 끝단의 inTangent/outTangent 는 0 으로 둔다(clip 양 끝의 outside 보간은 무관).
        /// </summary>
        private static AnimationCurve BuildCurve(float[] times, float[] values, SamplerInterpolation interp)
        {
            var keys = new Keyframe[times.Length];
            for (var i = 0; i < times.Length; ++i)
            {
                keys[i] = new Keyframe(times[i], values[i]);
            }

            for (var i = 0; i + 1 < keys.Length; ++i)
            {
                if (interp == SamplerInterpolation.Linear)
                {
                    var dt = keys[i + 1].time - keys[i].time;
                    if (dt > 0f)
                    {
                        var slope = (keys[i + 1].value - keys[i].value) / dt;
                        keys[i].outTangent = slope;
                        keys[i + 1].inTangent = slope;
                    }
                }
                else
                {
                    keys[i].outTangent = float.PositiveInfinity;
                    keys[i + 1].inTangent = float.PositiveInfinity;
                }
            }

            return new AnimationCurve(keys);
        }

        private static SamplerInterpolation ParseInterpolation(string value)
        {
            // CUBICSPLINE 은 SupportedSubset 에서 거부됨. 알 수 없는 값은 LINEAR 로 fallback.
            if (string.Equals(value, "STEP", StringComparison.Ordinal))
            {
                return SamplerInterpolation.Step;
            }

            return SamplerInterpolation.Linear;
        }
    }
}
