// Jump Flood Algorithm (JFA) 점프 스텝.
// 입력: 이전 패스의 시드 텍스쳐 (.rg = 시드 픽셀 좌표).
// _StepSize: 점프 거리 (픽셀 단위). 일반적으로 N/2 → N/4 → … → 1 의 시퀀스로 호출.
// 각 픽셀에서 자기 자신 + 8 방향 × _StepSize 의 시드 후보 중 가장 가까운 것을 선택.
Shader "Hidden/UModelerX_TexturePainter_JFAStep"
{
    Properties { }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;  // x=1/w, y=1/h, z=w, w=h
            float     _StepSize;            // 점프 거리 (픽셀)

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos    : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 myPx = i.uv * _MainTex_TexelSize.zw;
                float2 stepUV = _MainTex_TexelSize.xy * _StepSize;

                // 시드 무효 마커: seed.x > 50000.0 (JFAInit 의 60000 sentinel 과 일치).
                float2 bestSeed = tex2D(_MainTex, i.uv).rg;
                float  bestDist2 = (bestSeed.x > 50000.0) ? 1e30 : dot(bestSeed - myPx, bestSeed - myPx);

                [unroll] for (int dy = -1; dy <= 1; ++dy)
                {
                    [unroll] for (int dx = -1; dx <= 1; ++dx)
                    {
                        if (dx == 0 && dy == 0) continue;
                        float2 sampleUV = i.uv + float2(dx, dy) * stepUV;
                        if (sampleUV.x < 0.0 || sampleUV.x > 1.0 ||
                            sampleUV.y < 0.0 || sampleUV.y > 1.0) continue;
                        float2 seed = tex2D(_MainTex, sampleUV).rg;
                        if (seed.x > 50000.0) continue;
                        float2 d = seed - myPx;
                        float  d2 = dot(d, d);
                        if (d2 < bestDist2) { bestDist2 = d2; bestSeed = seed; }
                    }
                }
                return float4(bestSeed, 0, 1);
            }
            ENDCG
        }
    }
}
