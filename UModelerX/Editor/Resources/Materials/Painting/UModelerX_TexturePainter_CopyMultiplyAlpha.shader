Shader "Hidden/UModelerX_TexturePainter_CopyMultiplyAlpha"
{
    Properties
    {
        [HideInInspector] [Toggle] _ColorSpaceLinear("Color Space Linear", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" }
        LOD 100

        Pass
        {
            Cull off
            ZTest always
            Blend SrcAlpha OneMinusSrcAlpha, One Zero

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

                if (_ColorSpaceLinear == 1)
                {
                    color.rgb = pow(color.rgb, 1.0 / 2.2);
                }
                return color;
            }
            ENDCG
        }
    }
}
