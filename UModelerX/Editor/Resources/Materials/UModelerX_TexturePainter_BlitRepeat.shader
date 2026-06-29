Shader "Hidden/UModelerX_TexturePainter_BlitRepeat"
{
    Properties
    {
        _MainTex ("", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" }
        LOD 100

        Pass
        {
            Cull Off
            ZTest Always
            // Canvas2DDisplay 와 동일한 premultiplied alpha 블렌드.
            Blend One OneMinusSrcAlpha
            ColorMask RGBA

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 입력은 이미 premultiplied (origRgb*a, a). 추가 곱 없이 그대로 통과.
                float4 c = tex2D(_MainTex, i.uv);
                c *= _Color.a;
                c.rgb *= _Color.rgb;
                return c;
            }
            ENDCG
        }
    }
}
