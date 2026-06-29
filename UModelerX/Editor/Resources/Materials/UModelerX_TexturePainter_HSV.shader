Shader "Hidden/UModelerX_TexturePainter_HSV"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Hue ("Hue", Range(-0.5, 0.5)) = 0
        _Saturation ("Saturation", Range(0, 2)) = 1
        _Lightness ("Lightness", Range(0, 2)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull off
            ZTest always
            Blend One Zero, One Zero

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
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _Hue;
            float _Saturation;
            float _Lightness;

            float3 RGBtoHSL(float3 color)
            {
                float maxc = max(max(color.r, color.g), color.b);
                float minc = min(min(color.r, color.g), color.b);
                float delta = maxc - minc;
                
                float h = 0;
                float s = 0;
                float l = maxc;
                
                s = (maxc > 0.00001) ? (delta / maxc) : 0.0;

                if (delta > 0.000001)
                {
                    if (color.r >= color.g && color.r >= color.b) {
                        h = (color.g - color.b) / delta + (color.g < color.b ? 6.0 : 0.0);
                    }
                    else if (color.g >= color.r && color.g >= color.b) {
                        h = (color.b - color.r) / delta + 2.0;
                    }   
                    else {
                        h = (color.r - color.g) / delta + 4.0;
                    }
                        
                    h /= 6.0;
                }
                
                return float3(h, s, l);
            }

            float3 HSLtoRGB(float3 hsl)
            {
                float h = hsl.x;
                float s = hsl.y;
                float l = hsl.z;
                
                float3 rgb;
                
                if (s <= 0.0)
                {
                    rgb = float3(l, l, l);
                }
                else
                {
                    h = h * 6.0;
                    int i = (int)floor(h);
                    float f = h - i;

                    float p = l * (1.0 - s);
                    float q = l * (1.0 - s * f);
                    float t = l * (1.0 - s * (1.0 - f));
                    
                    if (i == 0)
                        rgb = float3(l, t, p);
                    else if (i == 1)
                        rgb = float3(q, l, p);
                    else if (i == 2)
                        rgb = float3(p, l, t);
                    else if (i == 3)
                        rgb = float3(p, q, l);
                    else if (i == 4)
                        rgb = float3(t, p, l);
                    else // i == 5
                        rgb = float3(l, p, q);
                }
                
                return rgb;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                float alpha = max(0.00001, c.a);
                
                float3 color = c.rgb / alpha;
                float3 hsl = RGBtoHSL(color);
                
                hsl.x = frac(hsl.x + _Hue + 1);
                hsl.y = saturate(hsl.y * _Saturation);
                hsl.z = saturate(hsl.z * _Lightness);
                
                color = HSLtoRGB(hsl);
                return float4(color * alpha, alpha);
            }
            ENDCG
        }
    }
}
