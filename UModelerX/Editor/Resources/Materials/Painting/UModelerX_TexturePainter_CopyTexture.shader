Shader "Hidden/UModelerX_TexturePainter_CopyTexture"
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
            ZTest always
            Blend One Zero

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
            float _ColorSpaceLinear;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv);
                return color;
            }
            ENDCG
        }
    }
}
