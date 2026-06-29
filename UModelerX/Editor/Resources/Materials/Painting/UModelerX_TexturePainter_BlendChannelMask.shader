Shader "Hidden/UModelerX_TexturePainter_BlendChannelMask"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
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
            sampler2D _BlendTex;
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
                float4 blend = tex2D(_BlendTex, i.uv);

                if (_ChannelType == 1)
                    base = base.xxxx;

                float4 final = lerp(base, blend, 1 - _Mask);
                if (_Mask.a == 0)
                {
                    return float4(final.rgb, 1);
                }
                return final;
            }
            ENDCG
        }
    }
}
