Shader "Hidden/UModelerX_TexturePainter_BlendPreview"
{
    Properties
    {
        
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" }
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
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _BlendColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 base = tex2D(_MainTex, i.uv);
                if (base.a == 0) {
                    return _BlendColor;
                }

                float3 finalColor = base.rgb + _BlendColor.rgb * (1 - base.a);
                float finalAlpha = base.a + _BlendColor.a * (1.0 - base.a);

                return float4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }
}
