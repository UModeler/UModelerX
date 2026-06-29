Shader "Hidden/UModelerX_TexturePainter_ShapeEllipse"
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

            float4 _ShapeRect;    // (x, y, w, h) in UV space [0..1]
            float _StrokeWidth;   // in pixel space
            float4 _StrokeColor;  // premultiplied alpha
            float4 _FillColor;    // premultiplied alpha
            int _StrokeStyle;     // 0=Solid, 1=Dashed, 2=Dotted
            float _DashLen;       // in pixel space
            float _GapLen;        // in pixel space
            float2 _TexSize;      // (texWidth, texHeight)

            float2 nearestPointOnEllipse(float2 p, float2 ab)
            {
                if (abs(ab.x - ab.y) < 0.5)
                {
                    float radius = (ab.x + ab.y) * 0.5;
                    float len = length(p);
                    if (len < 0.0001)
                    {
                        return float2(radius, 0.0);
                    }

                    return p * (radius / len);
                }

                float2 signP = sign(p);
                signP = float2(signP.x == 0.0 ? 1.0 : signP.x, signP.y == 0.0 ? 1.0 : signP.y);
                float2 absP = abs(p);
                float2 workP = absP;
                float2 workAb = ab;
                bool swapped = false;
                if (workP.x > workP.y)
                {
                    workP = workP.yx;
                    workAb = workAb.yx;
                    swapped = true;
                }

                float l = workAb.y * workAb.y - workAb.x * workAb.x;
                float m = workAb.x * workP.x / l;
                float m2 = m * m;
                float n = workAb.y * workP.y / l;
                float n2 = n * n;
                float c = (m2 + n2 - 1.0) / 3.0;
                float c3 = c * c * c;
                float q = c3 + m2 * n2 * 2.0;
                float d = c3 + m2 * n2;
                float g = m + m * n2;

                float co;
                if (d < 0.0)
                {
                    float h = acos(q / c3) / 3.0;
                    float s = cos(h);
                    float t = sin(h) * 1.7320508;
                    float rx = sqrt(-c * (s + t + 2.0) + m2);
                    float ry = sqrt(-c * (s - t + 2.0) + m2);
                    co = (ry + sign(l) * rx + abs(g) / (rx * ry) - m) / 2.0;
                }
                else
                {
                    float h = 2.0 * m * n * sqrt(d);
                    float s = sign(q + h) * pow(abs(q + h), 1.0 / 3.0);
                    float u = sign(q - h) * pow(abs(q - h), 1.0 / 3.0);
                    float rx = -s - u - c * 4.0 + 2.0 * m2;
                    float ry = (s - u) * 1.7320508;
                    float rm = sqrt(rx * rx + ry * ry);
                    co = (ry / sqrt(rm - rx) + 2.0 * g / rm - m) / 2.0;
                }

                float2 nearest = workAb * float2(co, sqrt(1.0 - co * co));
                if (swapped)
                {
                    nearest = nearest.yx;
                }

                return nearest * signP;
            }

            // Signed distance to an ellipse (Inigo Quilez - 3 Newton iterations)
            // p = query point, ab = (semi-axis x, semi-axis y)
            float sdEllipse(float2 p, float2 ab)
            {
                float2 nearest = nearestPointOnEllipse(p, ab);
                return length(nearest - p) * sign(length(p) - length(nearest));
            }

            float ellipseArcLengthIntegral(float angle, float2 ab)
            {
                const int SampleCount = 12;
                float clampedAngle = clamp(angle, 0.0, 6.2831853);
                float stepAngle = clampedAngle / SampleCount;
                float2 prev = float2(ab.x, 0.0);
                float arcLength = 0.0;

                [unroll]
                for (int sampleIndex = 1; sampleIndex <= SampleCount; ++sampleIndex)
                {
                    float sampleAngle = stepAngle * sampleIndex;
                    float2 current = float2(cos(sampleAngle) * ab.x, sin(sampleAngle) * ab.y);
                    arcLength += length(current - prev);
                    prev = current;
                }

                return arcLength;
            }

            float ellipseArcLength(float2 ellipsePoint, float2 center, float2 ab)
            {
                float2 rel = ellipsePoint - center;
                float angle = atan2(rel.y / max(ab.y, 0.001), rel.x / max(ab.x, 0.001));
                if (angle < 0.0) angle += 6.2831853; // 2*PI
                return ellipseArcLengthIntegral(angle, ab);
            }

            float periodicSignedOffset(float value, float pattern, float phase)
            {
                if (pattern <= 0.0)
                {
                    return 0.0;
                }

                float shifted = value - phase;
                return shifted - floor(shifted / pattern + 0.5) * pattern;
            }

            float dashedMask(float arcLen, float dashLen, float gapLen, float feather)
            {
                float pattern = dashLen + gapLen;
                if (pattern <= 0.0 || dashLen <= 0.0)
                {
                    return 1.0;
                }

                float centerOffset = periodicSignedOffset(arcLen, pattern, dashLen * 0.5);
                float edgeDistance = abs(centerOffset) - dashLen * 0.5;
                return saturate(-edgeDistance / max(feather, 0.001) + 0.5);
            }

            float resolveClosedGap(float totalLength, float filledLength, float preferredGap)
            {
                if (totalLength <= 0.0 || filledLength <= 0.0)
                {
                    return max(preferredGap, 0.0);
                }

                float preferredPattern = max(filledLength + preferredGap, filledLength);
                float maxRepeatCount = max(floor(totalLength / filledLength), 1.0);
                float repeatCount = clamp(round(totalLength / preferredPattern), 1.0, maxRepeatCount);
                float actualPattern = totalLength / repeatCount;
                return max(actualPattern - filledLength, 0.0);
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

            float dottedMask(float arcLen, float signedDistance, float dotDiameter, float gapLen, float feather)
            {
                return dotMaskFromCoordinates(arcLen, signedDistance, dotDiameter, gapLen, feather, dotDiameter * 0.5);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Convert UV to pixel coordinates
                float2 pixelPos = i.uv * _TexSize;

                // Shape rect in pixel space (C# 측에서 이미 픽셀 스냅 완료)
                float2 rectMin = _ShapeRect.xy * _TexSize;
                float2 rectSize = _ShapeRect.zw * _TexSize;
                float2 center = rectMin + rectSize * 0.5;
                float2 ab = rectSize * 0.5; // semi-axes
                float halfStroke = _StrokeWidth * 0.5;
                float2 centerAb = max(ab - halfStroke, float2(0.001, 0.001));

                // SDF distance
                float2 relPos = pixelPos - center;
                float d = sdEllipse(relPos, ab);
                float centerlineDistance = sdEllipse(relPos, centerAb);
                float2 nearestCenterPoint = nearestPointOnEllipse(relPos, centerAb);
                float patternArcLength = ellipseArcLength(center + nearestCenterPoint, center, centerAb);
                float totalPerimeter = ellipseArcLengthIntegral(6.2831853, centerAb);
                float resolvedGap = resolveClosedGap(totalPerimeter, _DashLen, _GapLen);

                // 텍스처 RT에 1:1 렌더이므로 상수 사용
                float fw = 1.0;

                // Fill: d < 0 이 shape 내부 (step으로 픽셀 단위 양자화)
                float fillMask = step(0.5, saturate(-d / fw + 0.5));
                float4 fill = _FillColor * fillMask;

                // Inside-aligned stroke: d ∈ [-_StrokeWidth, 0]
                float outerMask = saturate(-d / fw + 0.5);
                float innerMask = saturate((d + _StrokeWidth) / fw + 0.5);
                float strokeMask = saturate(min(outerMask, innerMask));

                if (_StrokeStyle == 1 && strokeMask > 0.0)
                {
                    float dMask = dashedMask(patternArcLength, _DashLen, resolvedGap, fw);
                    strokeMask *= dMask;
                }
                else if (_StrokeStyle == 2 && strokeMask > 0.0)
                {
                    float dMask = dottedMask(patternArcLength, centerlineDistance, _DashLen, resolvedGap, fw);
                    strokeMask *= dMask;
                }

                float4 stroke = _StrokeColor * strokeMask;

                // Composite: stroke over fill (premultiplied alpha)
                float4 col = stroke + fill * (1.0 - stroke.a);
                return col;
            }
            ENDCG
        }
    }
}
