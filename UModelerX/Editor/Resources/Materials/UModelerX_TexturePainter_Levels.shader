Shader "Hidden/UModelerX_TexturePainter_Levels"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        // RGB(composite) — 모든 채널에 동일하게 적용
        _BlackIn ("Black In", Float) = 0
        _WhiteIn ("White In", Float) = 1
        _Gamma ("Gamma", Float) = 1
        _BlackOut ("Black Out", Float) = 0
        _WhiteOut ("White Out", Float) = 1
        // Per-channel R
        _RBlackIn ("R Black In", Float) = 0
        _RWhiteIn ("R White In", Float) = 1
        _RGamma ("R Gamma", Float) = 1
        _RBlackOut ("R Black Out", Float) = 0
        _RWhiteOut ("R White Out", Float) = 1
        // Per-channel G
        _GBlackIn ("G Black In", Float) = 0
        _GWhiteIn ("G White In", Float) = 1
        _GGamma ("G Gamma", Float) = 1
        _GBlackOut ("G Black Out", Float) = 0
        _GWhiteOut ("G White Out", Float) = 1
        // Per-channel B
        _BBlackIn ("B Black In", Float) = 0
        _BWhiteIn ("B White In", Float) = 1
        _BGamma ("B Gamma", Float) = 1
        _BBlackOut ("B Black Out", Float) = 0
        _BWhiteOut ("B White Out", Float) = 1
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
            float _BlackIn,  _WhiteIn,  _Gamma,  _BlackOut,  _WhiteOut;
            float _RBlackIn, _RWhiteIn, _RGamma, _RBlackOut, _RWhiteOut;
            float _GBlackIn, _GWhiteIn, _GGamma, _GBlackOut, _GWhiteOut;
            float _BBlackIn, _BWhiteIn, _BGamma, _BBlackOut, _BWhiteOut;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float ApplyLevels(float v, float bI, float wI, float gm, float bO, float wO)
            {
                float range = max(1e-5, wI - bI);
                float r = saturate((v - bI) / range);
                r = pow(r, 1.0 / max(gm, 1e-4));
                return bO + r * (wO - bO);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                float alpha = max(0.00001, c.a);

                // premultiplied alpha를 unpremultiply 한 뒤 Levels 적용 (Brightness 셰이더와 동일 패턴)
                float3 color = c.rgb / alpha;

                // 1) RGB(composite): 모든 채널에 동일 변환
                color.r = ApplyLevels(color.r, _BlackIn, _WhiteIn, _Gamma, _BlackOut, _WhiteOut);
                color.g = ApplyLevels(color.g, _BlackIn, _WhiteIn, _Gamma, _BlackOut, _WhiteOut);
                color.b = ApplyLevels(color.b, _BlackIn, _WhiteIn, _Gamma, _BlackOut, _WhiteOut);

                // 2) Per-channel 추가 조정 (Photoshop Levels 처리 순서)
                color.r = ApplyLevels(color.r, _RBlackIn, _RWhiteIn, _RGamma, _RBlackOut, _RWhiteOut);
                color.g = ApplyLevels(color.g, _GBlackIn, _GWhiteIn, _GGamma, _GBlackOut, _GWhiteOut);
                color.b = ApplyLevels(color.b, _BBlackIn, _BWhiteIn, _BGamma, _BBlackOut, _BWhiteOut);

                return float4(color * c.a, c.a);
            }
            ENDCG
        }
    }
}
