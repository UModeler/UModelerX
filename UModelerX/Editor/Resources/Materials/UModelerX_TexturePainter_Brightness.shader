Shader "Hidden/UModelerX_TexturePainter_Brightness"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Brightness ("Brightness", Float) = 0
        _Contrast ("Contrast", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull off
            ZTest always
            Blend One Zero, One Zero

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
            float _Brightness;
            float _Contrast;

            float3 AdjustBrightness(float3 color, float brightness)
            {
                if (brightness > 0.0)
                {
                    return color + (1.0 - color) * brightness;
                }
                else
                {
                    return color + color * brightness;
                }
            }

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
                float alpha = max(0.00001, c.a);
                
                float3 color = c.rgb / alpha;
                
                float contrast = _Contrast + 1.0;
                color = (color - 0.5f) * contrast + 0.5f;
                color = AdjustBrightness(color, _Brightness);
                return float4(color * c.a, c.a);
            }
            ENDCG
        }
    }
}
