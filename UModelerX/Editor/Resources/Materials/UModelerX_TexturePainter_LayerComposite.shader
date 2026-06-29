Shader "Hidden/UModelerX_TexturePainter_LayerComposite"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent"}
        LOD 100

        Pass
        {
            Cull off
            ZWrite Off
            ZTest always
            Blend One Zero
            Colormask RGBA

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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0; // 회전된 uv
                float2 originalUV : TEXCOORD1; // 회전 전 uv
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float4 _Color;
            float4 _TilingOffset;
            float _MaskTexType;
            float _Rotation;

            sampler2D _BaseTex;
            float _BlendMode;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // 1. 기본 UV에 tiling & offset 적용
                float2 uv = v.uv * _TilingOffset.xy + _TilingOffset.zw;

                // 2. 회전 중심을 (0.5, 0.5)로 이동
                uv -= 0.5;

                // 3. 회전 행렬 적용
                float rad = radians(_Rotation); // _Rotation 은 float 타입의 도 단위 회전값
                float cosR = cos(rad);
                float sinR = sin(rad);
                float2x2 rot = float2x2(cosR, -sinR, sinR, cosR);
                uv = mul(rot, uv);

                // 4. 다시 중심 복귀
                uv += 0.5;

                o.uv = uv;
                o.originalUV = v.uv;

                return o;
            }

            float3 Darken(float3 base, float3 blend)
            {
                return min(blend, base);
            }

            // 필터/보간으로 rgb > alpha 가 되는 비정상 premul을 채널별로 a로 클램프 후 언프리멀.
            // raw rgb/a는 페더에서 스트레이트가 폭주해 Screen·Overlay 등 테두리 아티팩트가 난다.
            float3 UnpremulStraightSafe(float3 p, float a)
            {
                float aa = max(a, 1e-5);
                float3 pc = float3(min(p.r, aa), min(p.g, aa), min(p.b, aa));
                return saturate(pc / aa);
            }

            // 블렌드 결과는 straight RGB. 레이어 불투명도는 frag 끝에서 base와 동일 패턴으로 한 번만 적용한다.
            float3 MultiplyBlend(float3 base, float3 blend)
            {
                return base * blend;
            }

            float3 ColorBurn(float3 base, float3 blend)
            {
                float3 result = 1 - (1 - base) / (blend + 1e-6);
                result = saturate(result); // 0과 1 사이로 클램핑
                result = lerp(float3(0.0, 0.0, 0.0), result, step(0.0, blend));
                return result;
            }

            float3 LinearBurn(float3 base, float3 blend)
            {
                return saturate(base + blend - 1);
            }

            float3 Lighten(float3 base, float3 blend)
            {
                return max(base, blend);
            }

            // Lighten: 스트레이트 색 max는 페더(낮은 알파)에서 rgb/alpha 언프리멀 시 채도 폭주 → 테두리 후광/깨진 가장자리.
            // 프리멀티플라이드끼리 max 후 union 알파로 나눠 스트레이트 근사(가장자리 안정화).
            float3 LightenFromPremul(float3 baseP, float baseA, float3 blendP, float blendA)
            {
                float3 m = max(baseP, blendP);
                float a = max(max(baseA, blendA), 1e-5);
                return m / a;
            }
            
            float3 Screen(float3 base, float3 blend)
            {
                return 1 - (1 - base) * (1 - blend);
            }
            
            float3 ColorDodge(float3 base, float3 blend)
            {
                float3 result = base / (1 - blend + 1e-6);
                return result;
                result = saturate(result); // 0과 1 사이로 클램핑
                result = lerp(result, float3(1.0, 1.0, 1.0), step(1.0 - 1e-6, blend));
                return result;
            }
            
            float3 LinearDodge(float3 base, float3 blend)
            {
                return min(base + blend, 1);
            }
            
            float3 Overlay(float3 base, float3 blend)
            {
                float3 result;
                result.r = base.r <= 0.5 ? 2 * base.r * blend.r : 1 - 2 * (1 - base.r) * (1 - blend.r);
                result.g = base.g <= 0.5 ? 2 * base.g * blend.g : 1 - 2 * (1 - base.g) * (1 - blend.g);
                result.b = base.b <= 0.5 ? 2 * base.b * blend.b : 1 - 2 * (1 - base.b) * (1 - blend.b);
                return result;
            }
            
            float3 SoftLight(float3 base, float3 blend)
            {
                float3 result;
                if(blend.r <= 0.5) result.r = base.r - (1 - 2 * blend.r) * base.r * (1 - base.r);
                else result.r = base.r + (2 * blend.r - 1) * (sqrt(base.r) - base.r);
                if(blend.g <= 0.5) result.g = base.g - (1 - 2 * blend.g) * base.g * (1 - base.g);
                else result.g = base.g + (2 * blend.g - 1) * (sqrt(base.g) - base.g);
                if(blend.b <= 0.5) result.b = base.b - (1 - 2 * blend.b) * base.b * (1 - base.b);
                else result.b = base.b + (2 * blend.b - 1) * (sqrt(base.b) - base.b);

                return result;
            }
            
            float3 HardLight(float3 base, float3 blend)
            {
                float3 result;
                if(blend.r <= 0.5) result.r = 2 * base.r * blend.r;
                else result.r = 1 - 2 * (1 - base.r) * (1 - blend.r);
                if(blend.g <= 0.5) result.g = 2 * base.g * blend.g;
                else result.g = 1 - 2 * (1 - base.g) * (1 - blend.g);
                if(blend.b <= 0.5) result.b = 2 * base.b * blend.b;
                else result.b = 1 - 2 * (1 - base.b) * (1 - blend.b);
                return result;
            }
            
            float3 VividLight(float3 base, float3 blend)
            {
                const float epsilon = 1e-6;
                float3 result;

                if(blend.r <= 0.5) result.r = 1 - (1 - base.r) / (2 * blend.r + epsilon);
                else result.r = base.r / (2 * (1 - blend.r) + epsilon);
                if(blend.g <= 0.5) result.g = 1 - (1 - base.g) / (2 * blend.g + epsilon);
                else result.g = base.g / (2 * (1 - blend.g) + epsilon);
                if(blend.b <= 0.5) result.b = 1 - (1 - base.b) / (2 * blend.b + epsilon);
                else result.b = base.b / (2 * (1 - blend.b) + epsilon);

                return result;
            }

            float3 LinearLight(float3 base, float3 blend)
            {
                return saturate(base + 2 * blend - 1);
            }

            float3 PinLight(float3 base, float3 blend)
            {
                float3 result;

                if(blend.r <= 0.5) result.r = min(base.r, 2 * blend.r);
                else result.r = max(base.r, 2 * blend.r - 1);
                if(blend.g <= 0.5) result.g = min(base.g, 2 * blend.g);
                else result.g = max(base.g, 2 * blend.g - 1);
                if(blend.b <= 0.5) result.b = min(base.b, 2 * blend.b);
                else result.b = max(base.b, 2 * blend.b - 1);

                return result;
            }
            
            float3 Difference(float3 base, float3 blend)
            {
                return abs(base - blend);
            }
            
            float3 Exclusion(float3 base, float3 blend)
            {
                return base + blend - 2 * base * blend;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // _BaseTex(아래 레이어 합성본) 와 _MaskTex(레이어 마스크) 는 캔버스에 1:1 매핑되어야 한다.
                // tiling/rotation 이 적용된 i.uv 로 샘플링하면 base 가 캔버스 내에서 늘어나거나 회전됨.
                // _MainTex(fill 패턴) 만 tiling/rotation 적용된 uv 로 샘플링하고,
                // 또한 frac() 로 직접 wrap 해서 원본 텍스처 에셋의 wrapMode(Clamp 기본) 와 무관하게 tile 되도록 함.
                float4 base = tex2Dlod(_BaseTex, float4(i.originalUV, 0, 0));
                float maskValue = _MaskTexType == 1 ? tex2D(_MaskTex, i.originalUV).r : 1;

                float2 tiledUV = frac(i.uv);
                float4 tex = tex2Dlod(_MainTex, float4(tiledUV, 0, 0));
                float4 blend = tex * _Color * maskValue;
                float blend_alpha = blend.a;

                if (_BlendMode != 0 && blend_alpha > 0)
                {
                    float base_alpha = base.a;
                    if (base_alpha > 0)
                    {
                        float3 blend_rgb = UnpremulStraightSafe(blend.rgb, blend_alpha);
                        float3 base_rgb = UnpremulStraightSafe(base.rgb, base_alpha);
                        
                        float3 dest_rgb = 0;

                        if (_BlendMode == 10)
                        {
                            dest_rgb = Darken(base_rgb, blend_rgb);
                        }
                        else if (_BlendMode == 11)
                        {
                            dest_rgb = MultiplyBlend(base_rgb, blend_rgb);
                        }
                        else if (_BlendMode == 12)
                        {
                            dest_rgb = ColorBurn(base_rgb, blend_rgb);
                        }
                        else if (_BlendMode == 13)
                        {
                            dest_rgb = LinearBurn(base_rgb, blend_rgb);
                        }
                        else if (_BlendMode == 20)
                        {
                            dest_rgb = LightenFromPremul(base.rgb, base.a, blend.rgb, blend_alpha);
                        }
                        else if (_BlendMode == 21)
                        {
                            dest_rgb = Screen(base_rgb, blend_rgb);
                        }
                        else if (_BlendMode == 22)
                        {
                            dest_rgb = ColorDodge(base_rgb, blend_rgb);
                        }
                        else if (_BlendMode == 23)
                        {
                            dest_rgb = LinearDodge(base_rgb, blend_rgb);
                        }
                        else if (_BlendMode == 30)
                        {
                            dest_rgb = Overlay(base_rgb, blend_rgb);
                        }
                        else if (_BlendMode == 31)
                        {
                            dest_rgb = SoftLight(base_rgb, blend_rgb);
                        }
                        else if (_BlendMode == 32)
                        {
                            dest_rgb = HardLight(base_rgb, blend_rgb);
                        }
                        else if (_BlendMode == 33)
                        {
                            dest_rgb = VividLight(base_rgb, blend_rgb);
                        }
                        else if (_BlendMode == 34)
                        {
                            dest_rgb = LinearLight(base_rgb, blend_rgb);
                        }
                        else if (_BlendMode == 35)
                        {
                            dest_rgb = PinLight(base_rgb, blend_rgb);
                        }
                        else if (_BlendMode == 40)
                        {
                            dest_rgb = Difference(base_rgb, blend_rgb);
                        }
                        else if (_BlendMode == 41)
                        {
                            dest_rgb = Exclusion(base_rgb, blend_rgb);
                        }

                        // W3C Compositing: Cm = Cs·(1−αb) + B(Cb,Cs)·αb — 백드롭 페더(αb<1)에서 B만 쓰면 Multiply 등이 과하게 어두워짐.
                        float3 B = dest_rgb;
                        float3 Cs = blend_rgb;
                        float ab = base.a;
                        float3 Cm = saturate(Cs * (1.0 - ab) + B * ab);

                        // 소스 알파로 premul 오버 (Normal 패스와 동일 구조, 색은 Cm 사용).
                        float finalAlpha = blend_alpha + base.a * (1.0 - blend_alpha);
                        float3 finalColor = Cm * blend_alpha + base.rgb * (1.0 - blend_alpha);
                        return float4(finalColor, finalAlpha);
                    }

                    return blend;
                }
                else // normal blend
                {
                    if (blend.a == 0)
                    {
                        return base;
                    }

                    float3 finalColor = blend.rgb + base.rgb * (1.0 - blend.a);
                    float finalAlpha = blend.a + base.a * (1.0 - blend.a);
                    return float4(finalColor, finalAlpha);
                }
            }
            ENDCG
        }
    }
}
