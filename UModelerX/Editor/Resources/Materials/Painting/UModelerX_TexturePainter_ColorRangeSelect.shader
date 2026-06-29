Shader "Hidden/UModelerX_TexturePainter_ColorRangeSelect"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One Zero

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            // xyz = sampled rgb, w = used flag (1 = active, 0 = ignored).
            float4 _Samples[16];
            // xy = sampled uv (0..1), z = used flag.
            float4 _SamplePos[16];
            int _SampleCount;
            float _Fuzziness;
            float _Range;
            int _Localized;
            int _Mode;
            float _AlphaCutoff;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float hueBandWeight(float3 rgb, float hueTargetDeg, float fuzziness)
            {
                float3 hsv = rgb2hsv(rgb);
                float h = hsv.x * 360.0;
                float diff = abs(h - hueTargetDeg);
                diff = min(diff, 360.0 - diff);
                float halfBand = lerp(15.0, 90.0, saturate(fuzziness));
                float w = saturate(1.0 - diff / halfBand);
                w *= smoothstep(0.05, 0.15, hsv.y);
                w *= smoothstep(0.05, 0.15, hsv.z);
                return w;
            }

            float lumaRec709(float3 rgb)
            {
                return dot(rgb, float3(0.2126, 0.7152, 0.0722));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                if (col.a < _AlphaCutoff)
                {
                    return float4(0, 0, 0, 1);
                }

                float weight = 0.0;

                if (_Mode == 0)
                {
                    float fuzz = max(_Fuzziness, 0.001);
                    float rangeWidth = max(_Range, 0.001);
                    [unroll]
                    for (int s = 0; s < 16; s++)
                    {
                        if (_Samples[s].w < 0.5) continue;
                        float d = distance(col.rgb, _Samples[s].xyz);
                        float w = saturate(1.0 - d / fuzz);
                        if (_Localized != 0)
                        {
                            float du = distance(i.uv, _SamplePos[s].xy);
                            w *= saturate(1.0 - du / rangeWidth);
                        }
                        weight = max(weight, w);
                    }
                }
                else if (_Mode == 1) weight = hueBandWeight(col.rgb,   0.0, _Fuzziness);
                else if (_Mode == 2) weight = hueBandWeight(col.rgb,  60.0, _Fuzziness);
                else if (_Mode == 3) weight = hueBandWeight(col.rgb, 120.0, _Fuzziness);
                else if (_Mode == 4) weight = hueBandWeight(col.rgb, 180.0, _Fuzziness);
                else if (_Mode == 5) weight = hueBandWeight(col.rgb, 240.0, _Fuzziness);
                else if (_Mode == 6) weight = hueBandWeight(col.rgb, 300.0, _Fuzziness);
                else if (_Mode == 7)
                {
                    float Y  = lumaRec709(col.rgb);
                    float Cb = -0.168736 * col.r - 0.331264 * col.g + 0.500000 * col.b + 0.5;
                    float Cr =  0.500000 * col.r - 0.418688 * col.g - 0.081312 * col.b + 0.5;
                    float cbCenter = 0.40;
                    float crCenter = 0.60;
                    float halfRange = lerp(0.04, 0.20, saturate(_Fuzziness));
                    float wCb = saturate(1.0 - abs(Cb - cbCenter) / halfRange);
                    float wCr = saturate(1.0 - abs(Cr - crCenter) / halfRange);
                    float yGate = smoothstep(0.10, 0.20, Y) * (1.0 - smoothstep(0.92, 1.00, Y));
                    weight = wCb * wCr * yGate;
                }
                else if (_Mode == 8)
                {
                    float Y = lumaRec709(col.rgb);
                    float center = 0.78;
                    float halfBand = lerp(0.12, 0.40, saturate(_Fuzziness));
                    weight = saturate(1.0 - max(center - Y, 0.0) / halfBand);
                }
                else if (_Mode == 9)
                {
                    float Y = lumaRec709(col.rgb);
                    float center = 0.5;
                    float halfBand = lerp(0.15, 0.45, saturate(_Fuzziness));
                    weight = saturate(1.0 - abs(Y - center) / halfBand);
                }
                else if (_Mode == 10)
                {
                    float Y = lumaRec709(col.rgb);
                    float center = 0.22;
                    float halfBand = lerp(0.12, 0.40, saturate(_Fuzziness));
                    weight = saturate(1.0 - max(Y - center, 0.0) / halfBand);
                }

                weight = saturate(weight);
                return float4(weight, weight, weight, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
