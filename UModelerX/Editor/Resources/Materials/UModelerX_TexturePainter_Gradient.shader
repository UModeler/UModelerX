Shader "Hidden/UModelerX_TexturePainter_Gradient"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 100

        Pass
        {
            Cull off
            ZTest always
            Blend One OneMinusSrcAlpha
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            static const int MAX_STOPS = 8;

            float2 StartUV;
            float2 EndUV;
            // 캔버스 픽셀 크기 (width, height). 비정사각형 캔버스에서 그라디언트 방향이
            // 사용자 드래그의 화면(픽셀) 각도와 일치하도록 UV 를 픽셀 공간으로 변환할 때 사용.
            float2 _TexSize;

            int ColorCount;
            float ColorTime[MAX_STOPS];
            float4 Color[MAX_STOPS];

            int AlphaCount;
            float AlphaTime[MAX_STOPS];
            float Alphas[MAX_STOPS];

            int BlendMode;

            float3 rgb2oklab(float3 c) {
                float3 lms = mul(float3x3(
                    0.4122214708, 0.5363325363, 0.0514459929,
                    0.2119034982, 0.6806995451, 0.1073969566,
                    0.0883024619, 0.2817188376, 0.6299787005), c);
                lms = pow(lms, 1.0 / 3.0);
                return mul(float3x3(
                    0.2104542553, 0.7936177850, -0.0040720468,
                    1.9779984951, -2.4285922050, 0.4505937099,
                    0.0259040371, 0.7827717662, -0.8086757660), lms);
            }
            float3 oklab2rgb(float3 c) {
                float3 lms = mul(float3x3(
                    1.0, 0.3963377774, 0.2158037573,
                    1.0, -0.1055613458, -0.0638541728,
                    1.0, -0.0894841775, -1.2914855480), c);
                lms = lms * lms * lms;
                return mul(float3x3(
                    4.0767416621, -3.3077115913, 0.2309699292,
                    -1.2684380046, 2.6097574011, -0.3413193965,
                    -0.0041960863, -0.7034186147, 1.7076147010), lms);
            }

            float3 LerpColor(float3 c0, float3 c1, float w)
            {
                if (BlendMode == 1)
                    return w < 0.5 ? c0 : c1;
                if (BlendMode == 2) 
                {
                    float3 lab0 = rgb2oklab(c0);
                    float3 lab1 = rgb2oklab(c1);
                    float3 lab = lerp(lab0, lab1, w);
                    return oklab2rgb(lab);
                }
                return lerp(c0, c1, w);
            }

            float4 EvaluateColor(float t)
            {
                if (ColorCount == 0)
                {
                    return float4(1, 1, 1, 1);
                }

                if (t <= ColorTime[0]) return Color[0];
                if (t >= ColorTime[ColorCount - 1]) return Color[ColorCount - 1];

                float w = fwidth(t);

                for (int i = 1; i < ColorCount; i++)
                {
                    float prevT = ColorTime[i - 1];
                    float nextT = ColorTime[i];
                    if (t >= prevT && t <= nextT)
                    {
                        float blend = smoothstep(prevT - w, nextT + w, t);

                        float3 c0 = Color[i - 1].rgb;
                        float3 c1 = Color[i].rgb;
                        float3 c = LerpColor(c0, c1, blend);

                        return float4(c, 1);
                    }
                }

                return Color[ColorCount - 1];
            }

            float EvaluateAlpha(float t)
            {
                if (AlphaCount == 0) return 1.0;

                if (t <= AlphaTime[0]) return Alphas[0];
                if (t >= AlphaTime[AlphaCount - 1]) return Alphas[AlphaCount - 1];

                for (int i = 1; i < AlphaCount; i++)
                {
                    float prevT = AlphaTime[i - 1];
                    float nextT = AlphaTime[i];
                    if (t >= prevT && t <= nextT)
                    {
                        float w = saturate((t - prevT) / (nextT - prevT + 1e-6));

                        if (BlendMode == 1)
                            return (w < 0.5 ? Alphas[i - 1] : Alphas[i]);
                        else
                            return lerp(Alphas[i - 1], Alphas[i], w);
                    }
                }

                return Alphas[AlphaCount - 1];
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 비정사각형 캔버스에서도 사용자 드래그의 픽셀 각도와 그라디언트 방향이
                // 일치하도록 UV → 픽셀 공간으로 변환한 후 투영한다.
                // _TexSize 가 (0, 0) 으로 들어온 경우(이전 데이터/누락)에는 (1, 1) 로 폴백 → 기존 동작.
                float2 texSize = (_TexSize.x > 0.0 && _TexSize.y > 0.0) ? _TexSize : float2(1.0, 1.0);
                float2 pixelPos = i.uv * texSize;
                float2 startPx = StartUV * texSize;
                float2 endPx = EndUV * texSize;
                float2 dir = endPx - startPx;
                float t = saturate(dot(pixelPos - startPx, dir) / (dot(dir, dir) + 1e-8));
                float3 rgb = EvaluateColor(t);
                float a = saturate(EvaluateAlpha(t));

                return float4(rgb * a, a);

                /*float4 baseCol = tex2D(_BaseTex, i.uv);
                float3 outRGB = rgb * a + baseCol.rgb * (1.0 - a);
                float outA = a + baseCol.a * (1.0 - a);

                return float4(outRGB, outA);*/
            }
            ENDCG
        }
    }
}
