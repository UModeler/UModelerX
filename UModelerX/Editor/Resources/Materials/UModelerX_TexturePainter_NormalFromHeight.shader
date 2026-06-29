Shader "Hidden/UModelerX_TexturePainter_NormalFromHeight"
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Strength;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = float4(v.uv * float2(2,-2) + float2(-1,1), 1, 1);// UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3x3 sobelX = float3x3(-1, 0, 1, -2, 0, 2, -1, 0, 1);
                float3x3 sobelY = float3x3(-1, -2, -1, 0, 0, 0, 1, 2, 1);

                float dx = 0.0;
                float dy = 0.0;
                float3 normal;
                float4 color;

                // Calculate gradient in x and y using sobel operator
                for (int j = -1; j <= 1; j++)
                {
                    for (int k = -1; k <= 1; k++)
                    {
                        color = tex2D(_MainTex, i.uv + float2(k, j) * _MainTex_TexelSize.xy);
                        dx += sobelX[j + 1][k + 1] * color.r;
                        dy += sobelY[j + 1][k + 1] * color.r;
                    }
                }

                // Normalize the vector and convert to normal map format
                normal.xy = float2(dx, dy) * _Strength;
                normal.z = sqrt(1.0 - min(1, dot(normal.xy, normal.xy)));
                //normal = normalize(normal);

                return float4(normal * 0.5 + 0.5, 1.0);
            }
            ENDCG
        }
    }
}        