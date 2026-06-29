Shader "Hidden/UModelerX_TexturePainter_ShapeRect"
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
            float _CornerRadius;  // in pixel space
            float _StrokeWidth;   // in pixel space
            float4 _StrokeColor;  // premultiplied alpha
            float4 _FillColor;    // premultiplied alpha
            int _StrokeStyle;     // 0=Solid, 1=Dashed, 2=Dotted
            float _DashLen;       // in pixel space
            float _GapLen;        // in pixel space
            float2 _TexSize;      // (texWidth, texHeight)

            // Signed distance to a rounded box centered at origin
            // b = half-size, r = corner radius
            float sdRoundedBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            // Compute arc-length along the rounded rectangle perimeter
            // for dash/dot pattern. Origin at top-left corner, clockwise.
            // Uses SDF zone logic (q vector) to correctly identify the nearest
            // perimeter segment for pixels inside the stroke, not just on the boundary.
            float roundedRectArcLength(float2 p, float2 center, float2 halfSize, float cr)
            {
                float2 rel = p - center;
                float2 inner = halfSize - cr;

                float topLen    = inner.x * 2.0;
                float rightLen  = inner.y * 2.0;
                float bottomLen = inner.x * 2.0;
                float leftLen   = inner.y * 2.0;
                float arcLen90  = cr * 1.5707963; // cr * PI/2

                // SDF zone: q determines which perimeter segment is nearest
                float2 absRel = abs(rel);
                float2 q = absRel - inner;

                if (q.x > 0 && q.y > 0)
                {
                    // Corner arc zone
                    if (rel.x >= 0 && rel.y <= 0)
                    {
                        // Top-right corner
                        float2 ac = float2(inner.x, -inner.y);
                        float angle = atan2(rel.y - ac.y, rel.x - ac.x);
                        float t = saturate((angle + 1.5707963) / 1.5707963);
                        return topLen + t * arcLen90;
                    }
                    else if (rel.x >= 0 && rel.y > 0)
                    {
                        // Bottom-right corner
                        float2 ac = float2(inner.x, inner.y);
                        float angle = atan2(rel.y - ac.y, rel.x - ac.x);
                        float t = saturate(angle / 1.5707963);
                        return topLen + arcLen90 + rightLen + t * arcLen90;
                    }
                    else if (rel.x < 0 && rel.y > 0)
                    {
                        // Bottom-left corner
                        float2 ac = float2(-inner.x, inner.y);
                        float angle = atan2(rel.y - ac.y, rel.x - ac.x);
                        float t = saturate((angle - 1.5707963) / 1.5707963);
                        return topLen + arcLen90 + rightLen + arcLen90 + bottomLen + t * arcLen90;
                    }
                    else
                    {
                        // Top-left corner
                        float2 ac = float2(-inner.x, -inner.y);
                        float angle = atan2(rel.y - ac.y, rel.x - ac.x);
                        if (angle < 0) angle += 6.2831853;
                        float t = saturate((angle - 3.1415926) / 1.5707963);
                        return topLen + arcLen90 + rightLen + arcLen90 + bottomLen + arcLen90 + leftLen + t * arcLen90;
                    }
                }
                else if (q.y >= q.x)
                {
                    // Nearest to horizontal edge (top or bottom)
                    float cx = clamp(rel.x, -inner.x, inner.x);
                    if (rel.y <= 0)
                    {
                        return cx + inner.x;
                    }
                    else
                    {
                        return topLen + arcLen90 + rightLen + arcLen90 + (inner.x - cx);
                    }
                }
                else
                {
                    // Nearest to vertical edge (right or left)
                    float cy = clamp(rel.y, -inner.y, inner.y);
                    if (rel.x >= 0)
                    {
                        return topLen + arcLen90 + (cy + inner.y);
                    }
                    else
                    {
                        return topLen + arcLen90 + rightLen + arcLen90 + bottomLen + arcLen90 + (inner.y - cy);
                    }
                }
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

            // Pixel-accurate: dash segment is on when abs(centerOffset) <= dashLen/2 (no AA feather).
            float pixelDashedMask(float arcLen, float dashLen, float gapLen)
            {
                float pattern = dashLen + gapLen;
                if (pattern <= 0.0 || dashLen <= 0.0)
                {
                    return 1.0;
                }

                float centerOffset = periodicSignedOffset(arcLen, pattern, dashLen * 0.5);
                float halfDash = dashLen * 0.5;
                return step(abs(centerOffset), halfDash);
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

            float pixelDotMaskFromCoordinates(float arcLen, float signedDistance, float dotDiameter, float gapLen, float phase)
            {
                float pattern = dotDiameter + gapLen;
                if (pattern <= 0.0 || dotDiameter <= 0.0)
                {
                    return 1.0;
                }

                float centerOffset = periodicSignedOffset(arcLen, pattern, phase);
                float radius = dotDiameter * 0.5;
                float dotDistance = length(float2(centerOffset, signedDistance));
                return step(dotDistance, radius);
            }

            float pixelDottedMask(float arcLen, float signedDistance, float dotDiameter, float gapLen)
            {
                return pixelDotMaskFromCoordinates(arcLen, signedDistance, dotDiameter, gapLen, dotDiameter * 0.5);
            }

            float pixelBoxMask(float2 localPos, float2 halfExtents)
            {
                float2 d = abs(localPos) - halfExtents;
                return step(max(d.x, d.y), 0.0);
            }

            float sharpRectCornerDashMask(float2 relPos, float2 centerHalfSize, float halfStroke, float dashLen)
            {
                float segmentLen = max(dashLen, halfStroke * 2.0);
                float mask = 0.0;

                float2 tl = relPos - float2(-centerHalfSize.x, -centerHalfSize.y);
                mask = max(mask, pixelBoxMask(tl - float2(segmentLen * 0.5, 0.0), float2(segmentLen * 0.5, halfStroke)));
                mask = max(mask, pixelBoxMask(tl - float2(0.0, segmentLen * 0.5), float2(halfStroke, segmentLen * 0.5)));

                float2 tr = float2(centerHalfSize.x - relPos.x, relPos.y + centerHalfSize.y);
                mask = max(mask, pixelBoxMask(tr - float2(segmentLen * 0.5, 0.0), float2(segmentLen * 0.5, halfStroke)));
                mask = max(mask, pixelBoxMask(tr - float2(0.0, segmentLen * 0.5), float2(halfStroke, segmentLen * 0.5)));

                float2 br = float2(centerHalfSize.x - relPos.x, centerHalfSize.y - relPos.y);
                mask = max(mask, pixelBoxMask(br - float2(segmentLen * 0.5, 0.0), float2(segmentLen * 0.5, halfStroke)));
                mask = max(mask, pixelBoxMask(br - float2(0.0, segmentLen * 0.5), float2(halfStroke, segmentLen * 0.5)));

                float2 bl = float2(relPos.x + centerHalfSize.x, centerHalfSize.y - relPos.y);
                mask = max(mask, pixelBoxMask(bl - float2(segmentLen * 0.5, 0.0), float2(segmentLen * 0.5, halfStroke)));
                mask = max(mask, pixelBoxMask(bl - float2(0.0, segmentLen * 0.5), float2(halfStroke, segmentLen * 0.5)));

                return mask;
            }

            float pixelCircleMask(float2 localPos, float radius)
            {
                return step(length(localPos), radius);
            }

            float sharpRectCornerDotMask(float2 relPos, float2 centerHalfSize, float dotDiameter)
            {
                float radius = dotDiameter * 0.5;
                float mask = 0.0;
                mask = max(mask, pixelCircleMask(relPos - float2(-centerHalfSize.x, -centerHalfSize.y), radius));
                mask = max(mask, pixelCircleMask(relPos - float2(centerHalfSize.x, -centerHalfSize.y), radius));
                mask = max(mask, pixelCircleMask(relPos - float2(centerHalfSize.x, centerHalfSize.y), radius));
                mask = max(mask, pixelCircleMask(relPos - float2(-centerHalfSize.x, centerHalfSize.y), radius));
                return mask;
            }

            float sharpRectInteriorEdgeDotMask(float along, float across, float edgeLength, float dotDiameter, float gapLen)
            {
                if (edgeLength <= 0.0 || dotDiameter <= 0.0)
                {
                    return 0.0;
                }

                float preferredSpacing = max(dotDiameter + gapLen, 0.001);
                float intervalCount = max(round(edgeLength / preferredSpacing), 1.0);
                if (intervalCount <= 1.0)
                {
                    return 0.0;
                }

                float actualSpacing = edgeLength / intervalCount;
                float alongClamped = clamp(along, 0.0, edgeLength);
                float nearestIndex = round(alongClamped / max(actualSpacing, 0.001));
                nearestIndex = clamp(nearestIndex, 1.0, intervalCount - 1.0);
                float dotCenterAlong = nearestIndex * actualSpacing;
                float localAlong = alongClamped - dotCenterAlong;
                return pixelCircleMask(float2(localAlong, across), dotDiameter * 0.5);
            }

            float sharpRectDottedMask(float2 relPos, float2 centerHalfSize, float dotDiameter, float gapLen)
            {
                float topLen = centerHalfSize.x * 2.0;
                float rightLen = centerHalfSize.y * 2.0;
                float cornerMask = sharpRectCornerDotMask(relPos, centerHalfSize, dotDiameter);

                float topMask = sharpRectInteriorEdgeDotMask(
                    relPos.x + centerHalfSize.x,
                    relPos.y + centerHalfSize.y,
                    topLen,
                    dotDiameter,
                    gapLen);

                float rightMask = sharpRectInteriorEdgeDotMask(
                    relPos.y + centerHalfSize.y,
                    relPos.x - centerHalfSize.x,
                    rightLen,
                    dotDiameter,
                    gapLen);

                float bottomMask = sharpRectInteriorEdgeDotMask(
                    centerHalfSize.x - relPos.x,
                    relPos.y - centerHalfSize.y,
                    topLen,
                    dotDiameter,
                    gapLen);

                float leftMask = sharpRectInteriorEdgeDotMask(
                    centerHalfSize.y - relPos.y,
                    relPos.x + centerHalfSize.x,
                    rightLen,
                    dotDiameter,
                    gapLen);

                float edgeMask = max(max(topMask, rightMask), max(bottomMask, leftMask));
                return max(cornerMask, edgeMask);
            }

            float roundedRectPerimeter(float2 halfSize, float cr)
            {
                float2 inner = halfSize - cr;
                return inner.x * 4.0 + inner.y * 4.0 + cr * 6.2831853;
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
                float2 halfSize = rectSize * 0.5;

                // Clamp corner radius
                float cr = min(_CornerRadius, min(halfSize.x, halfSize.y));
                float halfStroke = _StrokeWidth * 0.5;
                float2 centerHalfSize = max(halfSize - halfStroke, float2(0.001, 0.001));
                float centerCr = min(max(cr - halfStroke, 0.0), min(centerHalfSize.x, centerHalfSize.y));

                // SDF distance
                float d = sdRoundedBox(pixelPos - center, halfSize, cr);
                float2 relPos = pixelPos - center;
                float centerlineDistance = sdRoundedBox(relPos, centerHalfSize, centerCr);
                float patternArcLength = roundedRectArcLength(pixelPos, center, centerHalfSize, centerCr);
                float totalPerimeter = roundedRectPerimeter(centerHalfSize, centerCr);
                float resolvedGap = resolveClosedGap(totalPerimeter, _DashLen, _GapLen);

                // Fill: inside shape (d <= 0), pixel binary
                float fillMask = step(0.0, -d);
                float4 fill = _FillColor * fillMask;

                // Inside-aligned stroke: d ∈ [-_StrokeWidth, 0], exact width in pixels (no AA)
                float strokeMask = 0.0;
                if (_StrokeWidth >= 0.5)
                {
                    strokeMask = step(-_StrokeWidth, d) * step(0.0, -d);
                }

                if (_StrokeStyle == 1 && strokeMask > 0.0)
                {
                    float dMask = pixelDashedMask(patternArcLength, _DashLen, resolvedGap);
                    strokeMask *= dMask;
                }
                else if (_StrokeStyle == 2 && strokeMask > 0.0)
                {
                    if (centerCr <= 0.001)
                    {
                        float dotMask = sharpRectDottedMask(relPos, centerHalfSize, _DashLen, resolvedGap);
                        strokeMask *= dotMask;
                    }
                    else
                    {
                        float dMask = pixelDottedMask(patternArcLength, centerlineDistance, _DashLen, resolvedGap);
                        strokeMask *= dMask;
                    }
                }

                if (cr <= 0.001 && strokeMask > 0.0)
                {
                    if (_StrokeStyle == 1)
                    {
                        float cornerMask = sharpRectCornerDashMask(relPos, centerHalfSize, halfStroke, _DashLen);
                        strokeMask = max(strokeMask, cornerMask);
                    }
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
