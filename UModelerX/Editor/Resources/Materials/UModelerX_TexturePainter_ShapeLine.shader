Shader "Hidden/UModelerX_TexturePainter_ShapeLine"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Cull Off
            ZTest Always
            Blend One OneMinusSrcAlpha
            ColorMask RGBA

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float2 _LineStart;    // in UV space [0..1]
            float2 _LineEnd;      // in UV space [0..1]
            float _StrokeWidth;   // in pixel space
            float4 _StrokeColor;  // premultiplied alpha
            int _StrokeStyle;     // 0=Solid, 1=Dashed, 2=Dotted
            float _DashLen;       // in pixel space
            float _GapLen;        // in pixel space
            float2 _TexSize;      // (texWidth, texHeight)
            int _LineCap;         // 0=Round, 1=SquareExtend, 2=SquareFlush

            float periodicSignedOffset(float value, float pattern, float phase)
            {
                if (pattern <= 0.0)
                {
                    return 0.0;
                }

                float shifted = value - phase;
                return shifted - floor(shifted / pattern + 0.5) * pattern;
            }

            float intervalMask(float value, float startValue, float endValue, float feather)
            {
                float centerValue = (startValue + endValue) * 0.5;
                float halfExtent = max(endValue - startValue, 0.0) * 0.5;
                float edgeDistance = abs(value - centerValue) - halfExtent;
                return saturate(-edgeDistance / max(feather, 0.001) + 0.5);
            }

            float resolveOpenDashGap(float totalLength, float dashLen, float preferredGap)
            {
                if (totalLength <= dashLen || dashLen <= 0.0)
                {
                    return 0.0;
                }

                float preferredSpacing = max(dashLen + preferredGap, dashLen);
                float segmentCount = max(round((totalLength - dashLen) / preferredSpacing) + 1.0, 2.0);
                float actualSpacing = (totalLength - dashLen) / max(segmentCount - 1.0, 1.0);
                return max(actualSpacing - dashLen, 0.0);
            }

            float resolveOpenDotGap(float totalLength, float dotDiameter, float preferredGap)
            {
                if (totalLength <= 0.0 || dotDiameter <= 0.0)
                {
                    return max(preferredGap, 0.0);
                }

                float preferredSpacing = max(dotDiameter + preferredGap, dotDiameter);
                float dotCount = max(round(totalLength / preferredSpacing) + 1.0, 2.0);
                float actualSpacing = totalLength / max(dotCount - 1.0, 1.0);
                return max(actualSpacing - dotDiameter, 0.0);
            }

            float dashedMaskOpen(float arcLen, float totalLen, float dashLen, float gapLen, float capExtension, float feather)
            {
                float pattern = dashLen + gapLen;
                if (pattern <= 0.0 || dashLen <= 0.0)
                {
                    return 1.0;
                }

                float centerOffset = periodicSignedOffset(arcLen, pattern, dashLen * 0.5);
                float edgeDistance = abs(centerOffset) - dashLen * 0.5;
                float repeatedMask = saturate(-edgeDistance / max(feather, 0.001) + 0.5);
                float startMask = intervalMask(arcLen, -capExtension, dashLen, feather);
                float endMask = intervalMask(arcLen, totalLen - dashLen, totalLen + capExtension, feather);
                return max(repeatedMask, max(startMask, endMask));
            }

            float dotMaskFromCoordinates(float arcLen, float signedDistance, float dotDiameter, float gapLen, float feather, float phase)
            {
                float pattern = dotDiameter + gapLen;
                if (pattern <= 0.0 || dotDiameter <= 0.0)
                {
                    return 1.0;
                }

                float centerOffset = periodicSignedOffset(arcLen, pattern, phase);
                float radius = dotDiameter * 0.5;
                float dotDistance = length(float2(centerOffset, signedDistance));
                return saturate((radius - dotDistance) / max(feather, 0.001) + 0.5);
            }

            float dottedMaskOpen(float arcLen, float totalLen, float signedDistance, float dotDiameter, float gapLen, float feather)
            {
                float repeatedMask = dotMaskFromCoordinates(arcLen, signedDistance, dotDiameter, gapLen, feather, 0.0);
                float radius = dotDiameter * 0.5;
                float endDistance = length(float2(arcLen - totalLen, signedDistance));
                float endMask = saturate((radius - endDistance) / max(feather, 0.001) + 0.5);
                return max(repeatedMask, endMask);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // sub-pixel 위치 p 에서 선분까지 SDF 거리 (Round=양수 거리, Square=박스 SDF, 음수=내부).
            float ComputeLineDistance(float2 p, float2 a, float2 ba, float segLen, float segLenSq, float2 dir, float2 perp, float halfStroke,
                                      out float along, out float signedAcross)
            {
                float2 pa = p - a;
                along = dot(pa, dir);
                signedAcross = dot(pa, perp);

                if (_LineCap == 0)
                {
                    float tRaw = dot(pa, ba) / max(segLenSq, 0.001);
                    float tClamped = saturate(tRaw);
                    return length(pa - ba * tClamped);
                }
                else
                {
                    float extension = (_LineCap == 1) ? halfStroke : 0.0;
                    float halfLen = segLen * 0.5 + extension;
                    float2 localP = float2(along - segLen * 0.5, abs(signedAcross));
                    float2 d = abs(localP) - float2(halfLen, halfStroke);
                    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
                }
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 pixelPos = i.uv * _TexSize;

                float2 a = _LineStart * _TexSize;
                float2 b = _LineEnd * _TexSize;

                float2 ba = b - a;
                float segLenSq = dot(ba, ba);
                float segLen = sqrt(segLenSq);

                float halfStroke = _StrokeWidth * 0.5;
                float2 dir = ba / max(segLen, 0.001);
                float2 perp = float2(-dir.y, dir.x);

                float along, signedAcross;
                float dist = ComputeLineDistance(pixelPos, a, ba, segLen, segLenSq, dir, perp, halfStroke, along, signedAcross);

                // Hard cutoff. 안티앨리어싱/feather 없이 dist ≤ halfStroke 인 픽셀만 풀 알파.
                // 주변으로 색이 번지는 것을 방지하기 위해 부분 알파 픽셀을 만들지 않는다.
                float strokeMask;
                if (_LineCap == 0)
                {
                    strokeMask = step(dist, halfStroke);
                }
                else
                {
                    // Square cap: 박스 SDF 에서 dist ≤ 0 이 내부.
                    strokeMask = step(dist, 0.0);
                }

                float capExtension = _LineCap == 2 ? 0.0 : halfStroke;
                float resolvedDashGap = resolveOpenDashGap(segLen, _DashLen, _GapLen);
                float resolvedDotGap = resolveOpenDotGap(segLen, _DashLen, _GapLen);

                // Dash/Dot 도 hard cutoff 로 변환 (feather=극소값으로 step 효과).
                const float kHardFeather = 1e-4;
                if (_StrokeStyle == 1 && strokeMask > 0.0)
                {
                    float dMask = dashedMaskOpen(along, segLen, _DashLen, resolvedDashGap, capExtension, kHardFeather);
                    strokeMask *= step(0.5, dMask);
                }
                else if (_StrokeStyle == 2 && strokeMask > 0.0)
                {
                    float dMask = dottedMaskOpen(along, segLen, signedAcross, _DashLen, resolvedDotGap, kHardFeather);
                    strokeMask *= step(0.5, dMask);
                }

                float4 col = _StrokeColor * strokeMask;
                return col;
            }
            ENDCG
        }
    }
}
