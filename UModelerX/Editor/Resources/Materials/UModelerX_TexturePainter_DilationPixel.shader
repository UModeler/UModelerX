Shader "Hidden/UModelerX_TexturePainter_DilationPixel"
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
            float4 _MainTex_TexelSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                if (c.a < 1)
                {
                    float4 c1 =
                        tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * float2(-1, -1)) +
                        tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * float2(0, -1)) +
                        tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * float2(1, -1)) +

                        tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * float2(-1, 0)) +
                        tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * float2(1, 0)) +

                        tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * float2(-1, 1)) +
                        tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * float2(0, 1)) +
                        tex2D(_MainTex, i.uv + _MainTex_TexelSize.xy * float2(1, 1));

                    if (c1.a > 0.1f)
                        return float4(c1.rgb / c1.a, 1);
                    else
                        return 0;
                }
                return c;
            }
            ENDCG
        }
    }
}
