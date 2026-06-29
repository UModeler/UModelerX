Shader "Hidden/UModelerX_TexturePainter_SelectionOutline"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" }
        LOD 100

        Pass
        {
            Cull Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _SelectionMask;
            sampler2D _BackgroundTex;
            // world UV → mask UV 변환 행렬 (이동/회전/스케일 프리뷰 통합)
            float4x4 _MaskTransform;
            float _AnimationOffset;
            float _DashLength;
            float _DashPeriod;
            float _MaskWidth;
            float _MaskHeight;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 pixelPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.pixelPos = v.vertex.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 s = mul(_MaskTransform, float4(i.uv, 0, 1));
                float2 sampleUV = s.xy;

                float inRange = step(0.0, sampleUV.x) * step(sampleUV.x, 1.0)
                              * step(0.0, sampleUV.y) * step(sampleUV.y, 1.0);
                float4 maskSample = tex2D(_SelectionMask, sampleUV) * inRange;
                float alpha = max(maskSample.r, maskSample.a);

                // 스크린 픽셀 단위 스텝 (줌 레벨 무관하게 1px 외곽선)
                float2 pixelStep = fwidth(sampleUV);

                float2 uvLeft  = sampleUV + float2(-pixelStep.x, 0);
                float2 uvRight = sampleUV + float2( pixelStep.x, 0);
                float2 uvUp    = sampleUV + float2(0,  pixelStep.y);
                float2 uvDown  = sampleUV + float2(0, -pixelStep.y);

                float inRangeLeft  = step(0.0, uvLeft.x)  * step(uvLeft.x,  1.0) * step(0.0, uvLeft.y)  * step(uvLeft.y,  1.0);
                float inRangeRight = step(0.0, uvRight.x) * step(uvRight.x, 1.0) * step(0.0, uvRight.y) * step(uvRight.y, 1.0);
                float inRangeUp    = step(0.0, uvUp.x)    * step(uvUp.x,    1.0) * step(0.0, uvUp.y)    * step(uvUp.y,    1.0);
                float inRangeDown  = step(0.0, uvDown.x)  * step(uvDown.x,  1.0) * step(0.0, uvDown.y)  * step(uvDown.y,  1.0);

                float alphaLeft  = max(tex2D(_SelectionMask, uvLeft).r,  tex2D(_SelectionMask, uvLeft).a)  * inRangeLeft;
                float alphaRight = max(tex2D(_SelectionMask, uvRight).r, tex2D(_SelectionMask, uvRight).a) * inRangeRight;
                float alphaUp    = max(tex2D(_SelectionMask, uvUp).r,    tex2D(_SelectionMask, uvUp).a)    * inRangeUp;
                float alphaDown  = max(tex2D(_SelectionMask, uvDown).r,  tex2D(_SelectionMask, uvDown).a)  * inRangeDown;

                float centerSelected = step(0.5, alpha);
                float minAlpha5 = min(alpha, min(min(alphaLeft, alphaRight), min(alphaUp, alphaDown)));
                float maxAlpha5 = max(alpha, max(max(alphaLeft, alphaRight), max(alphaUp, alphaDown)));
                bool isContourEdge = (maxAlpha5 - minAlpha5) > 0.5;

                float2 distToUVEdge = min(sampleUV, 1.0 - sampleUV);
                float2 borderThreshold = pixelStep * 0.75;
                float onEdgeX = step(distToUVEdge.x, borderThreshold.x);
                float onEdgeY = step(distToUVEdge.y, borderThreshold.y);
                bool isUVEdge = (onEdgeX + onEdgeY > 0) && (centerSelected > 0.5);

                if (!isContourEdge && !isUVEdge)
                {
                    return fixed4(0, 0, 0, 0);
                }

                float2 grad = float2(alphaRight - alphaLeft, alphaUp - alphaDown);
                float gradLen = length(grad);

                if (isUVEdge && gradLen <= 0.0001)
                {
                    grad = float2(onEdgeX, onEdgeY);
                    gradLen = length(grad);
                }

                if (gradLen <= 0.0001)
                {
                    return fixed4(0, 0, 0, 0);
                }

                float2 tangent = normalize(float2(grad.y, -grad.x));
                // Use UV space for phase so dash pattern is correct when canvas is rotated (pixelPos would mix screen/UV spaces)
                float phaseCoord = dot(i.uv, tangent) * _MaskWidth;

                float dashPhase = fmod(phaseCoord + _AnimationOffset, _DashPeriod);
                if (dashPhase < 0) dashPhase += _DashPeriod;
                if (dashPhase > _DashLength) return fixed4(0, 0, 0, 0);

                float3 lineColor = float3(0, 0, 0);
                if (i.uv.x >= 0.0 && i.uv.x <= 1.0 && i.uv.y >= 0.0 && i.uv.y <= 1.0)
                {
                    float4 bg = tex2D(_BackgroundTex, i.uv);
                    float luminance = dot(bg.rgb, float3(0.2126, 0.7152, 0.0722));
                    // 투명할수록 실제로는 밝은 캔버스 배경(체커보드)이 보이므로 밝은 값으로 합성
                    float effectiveLum = lerp(1.0, luminance, saturate(bg.a));
                    lineColor = (effectiveLum < 0.5) ? float3(1, 1, 1) : float3(0, 0, 0);
                }

                return fixed4(lineColor, 1.0);
            }
            ENDCG
        }
    }
}
