Shader "Hidden/UModelerX_TexturePainter_CopyValue"
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
            ZTest always
            Blend One OneMinusSrcAlpha, One Zero

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
            float _AlphaType;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                float channelType = _AlphaType;
                if (channelType == 2)
                    return float4(c.aaa, 1);
                if (channelType == 1)
                    return float4(c.rrr, 1);
                if (channelType == 3)
                    return float4(c.rrr * c.a, 1);
                if (channelType == 4)
                    return c.a == 0 ? float4(0, 0, 0, 0) : float4(saturate(c.rgb / c.a), c.a);
                if (channelType == 5)
                    return c.a == 0 ? float4(1, 1, 1, 0) : float4(saturate(c.rgb / c.a), c.a);
                if (channelType == 100)
                {
                    return float4(c.rgb, c.a);     
                }

                return float4(c.rgb, 1);
            }
            ENDCG
        }
    }
}
