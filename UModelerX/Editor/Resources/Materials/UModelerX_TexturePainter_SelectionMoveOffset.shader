Shader "Hidden/UModelerX_TexturePainter_SelectionMoveOffset"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull off
            ZTest always
            Blend One Zero
            Colormask RGBA

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float2 _Offset;
            float2 _SelectionOffset;
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
                float4 originalColor = tex2D(_MainTex, i.uv);
                float2 offsetUv = i.uv - _Offset - _SelectionOffset;
                float4 offsetColor;
                if (offsetUv.x < 0.0 || offsetUv.x > 1.0 || offsetUv.y < 0.0 || offsetUv.y > 1.0)
                {
                    offsetColor = float4(0, 0, 0, 0);
                }
                else
                {
                    offsetColor = tex2D(_MainTex, offsetUv);
                }
                float4 m = tex2D(_MaskTex, i.uv);
                return lerp(originalColor, offsetColor, m.r);
            }
            ENDCG
        }
    }
}
