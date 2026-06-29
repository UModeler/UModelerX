// Jump Flood Algorithm (JFA) 초기 시드 텍스쳐 생성.
// 출력: .rg = 시드 픽셀 좌표 (픽셀 단위). 시드가 아닌 픽셀은 sentinel (60000, 60000).
//   (음수를 쓰지 않는 이유: 무효 마커를 텍스쳐에 클리어할 때 fixed4 셰이더가 [0,1] saturate 해버림.
//    RGHalf 의 최대 표현 가능값은 65504 이므로 60000 은 안전. 거리 계산 시 50000 임계로 검사.)
// _SeedMode 0: α > 0 인 픽셀을 시드로 (Outside 거리 계산용 — 가장 가까운 페인티드 픽셀까지).
// _SeedMode 1: α ≤ 0 인 픽셀을 시드로 (Inside 거리 계산용 — 가장 가까운 빈 픽셀까지).
Shader "Hidden/UModelerX_TexturePainter_JFAInit"
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
            int       _SeedMode;

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
                float a = tex2D(_MainTex, i.uv).a;
                // threshold 0.001 — 페어더의 가장 약한 픽셀도 시드로 잡음 (소프트 브러시 호환).
                bool isSeed = (_SeedMode == 0) ? (a > 0.001) : (a <= 0.001);
                float2 pixelPos = i.uv * _MainTex_TexelSize.zw;
                return isSeed ? float4(pixelPos, 0, 1) : float4(60000.0, 60000.0, 0, 1);
            }
            ENDCG
        }
    }
}
