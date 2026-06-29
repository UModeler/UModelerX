Shader "Hidden/UModelerX_TexturePainter_TextureCompositeTransparent"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
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
            sampler2D _BGTex;
            float4 _Mask;
            float _ChannelType;

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
                float4 background = tex2D(_BGTex, i.uv);
                if (_ChannelType == 1)
                    base = base.xxxx;
                if (base.a == 0) {
                    return fixed4(background.rgb * background.a, background.a);
                }

                return base;
            }
            ENDCG
        }
    }
}
