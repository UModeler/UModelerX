Shader "Hidden/UModelerX_TexturePainter_GradientScreen"
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
            // Premultiplied alpha "over" blend: source already has color*alpha,
            // and the target (existing layer content) is composited under it.
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
                float3 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            static const int MAX_STOPS = 8;
            static const int MAX_CURVE_POINTS = 64;

            // 메쉬의 object→world 변환. unity_ObjectToWorld 에 의존하지 않고 명시적으로 받아서 사용.
            float4x4 _Model;

            // 그라디언트 polyline (world 공간). xyz = 점 위치, w = 시작점부터의 누적 arclength.
            // Mouse/Axis 모드는 2-point (= 직선), CurveMesh 모드는 N-point.
            float4 _CurvePoints[MAX_CURVE_POINTS];
            int _CurvePointCount;
            float _CurveTotalLength;

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
                // 메쉬를 UV 공간으로 렌더한다: 정점의 클립 좌표가 (UV*2-1, ...) 이 되도록.
                // y 축은 RT 좌표계에 맞춰 뒤집는다 (UModelerX_TextureSpace.shader 와 동일).
                o.vertex = float4(v.uv * float2(2, -2) + float2(-1, 1), 0.5, 1);
                o.worldPos = mul(_Model, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 점이 부족하면 그리지 않음.
                if (_CurvePointCount < 2)
                    return float4(0, 0, 0, 0);

                // 가장 가까운 segment 위로 fragment 를 투영, 그 segment 의 누적 arclength 로 t 산출.
                // 같은 알고리즘으로 직선(2-point) 과 N-point polyline 을 통일.
                float bestT = 0.0;
                float bestDist2 = 1e30;
                int n = _CurvePointCount;
                for (int seg = 0; seg < n - 1; seg++)
                {
                    float3 a = _CurvePoints[seg].xyz;
                    float3 b = _CurvePoints[seg + 1].xyz;
                    float3 ab = b - a;
                    float ab2 = dot(ab, ab);
                    float segT = saturate(dot(i.worldPos - a, ab) / (ab2 + 1e-8));
                    float3 proj = a + ab * segT;
                    float3 diff = i.worldPos - proj;
                    float d2 = dot(diff, diff);
                    if (d2 < bestDist2)
                    {
                        bestDist2 = d2;
                        float segLen = sqrt(ab2);
                        float cumLen = _CurvePoints[seg].w + segT * segLen;
                        bestT = cumLen / max(_CurveTotalLength, 1e-8);
                    }
                }
                float t = saturate(bestT);

                float3 rgb = EvaluateColor(t).rgb;
                float a = saturate(EvaluateAlpha(t));

                // Premultiplied alpha — 외부의 Blend One OneMinusSrcAlpha 가 기존 RT 위에 over 합성.
                return float4(rgb * a, a);
            }
            ENDCG
        }
    }
}
