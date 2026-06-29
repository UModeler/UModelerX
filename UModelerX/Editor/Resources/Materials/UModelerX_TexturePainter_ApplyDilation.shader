Shader "Hidden/UModelerX_TexturePainter_ApplyDilation"
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

            sampler2D _MaskTex;
            sampler2D _DilationTex;

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
                float4 m = tex2D(_MaskTex, i.uv);
                float4 c = tex2D(_MainTex, i.uv);
                if (m.r < 1)
                {
                    float4 d = tex2D(_DilationTex, i.uv);
                    return d;
                }
                return c;
            }
            ENDCG
        }
    }
}
