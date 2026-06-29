Shader "Hidden/UModelerX_TexturePainter_Blur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Radius ("Blur Radius", Range(0, 20)) = 1.0
        _SampleCount ("Sample Count", Range(3, 15)) = 7
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Name "HorizontalBlur"
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
            float4 _MainTex_TexelSize;
            float _Radius;
            int _SampleCount;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float Gaussian(float x, float sigma)
            {
                return exp(-(x * x) / (2.0 * sigma * sigma)) / (sqrt(6.2831853) * sigma);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 finalColor = float4(0, 0, 0, 0);
                float totalWeight = 0;

                float2 baseOffset = float2(_MainTex_TexelSize.x, 0.0);
                int radius = ceil(3.0 * _Radius);
                float center = (_SampleCount - 1) * 0.5;

                for (int j = -radius; j <= radius; j++)
                {
                    float weight = Gaussian((float)j, _Radius);
                    float2 sampleUV = i.uv + baseOffset * j;

                    finalColor += tex2D(_MainTex, sampleUV) * weight;
                    totalWeight += weight;
                }

                return finalColor / totalWeight;
            }
            ENDCG
        }

        Pass
        {
            Name "VerticlaBlur"
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
            float4 _MainTex_TexelSize;
            float _Radius;
            int _SampleCount;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float Gaussian(float x, float sigma)
            {
                return exp(-(x * x) / (2.0 * sigma * sigma)) / (sqrt(6.2831853) * sigma);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float totalWeight = 0.0;
                float4 finalColor = 0.0;

                float2 baseOffset = float2(0.0, _MainTex_TexelSize.y);
                int radius = ceil(3.0 * _Radius);
                float center = (_SampleCount - 1) * 0.5;

                for (int j = -radius; j <= radius; j++)
                {
                    float weight = Gaussian((float)j, _Radius);
                    float2 sampleUV = i.uv + baseOffset * j;

                    finalColor += tex2D(_MainTex, sampleUV) * weight;
                    totalWeight += weight;
                }

                return finalColor / totalWeight;
            }
            ENDCG
        }
    }
}
