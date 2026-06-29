Shader "Hidden/UModelerX_TexturePaintPreviewTexOnly_HDRP"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags 
        {
            "RenderType" = "Opaque"
            "LightMode" = "UModelerXPreview"
        }
        LOD 100

        Pass
        {
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
            float4 _MainTex_ST;
            float _Type;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                if (_Type == 3)
                {
                    c.rgb = c.rrr;
                }
                if (_Type == 4)
                {
                    float2 uv = saturate(i.uv.xy);
                    float2 w = step(uv, 1) * step(0, uv);
                    c.rgb = float3(uv, 1 - w.x * w.y);
                }
                if (_Type == 5)
                {
                    c.rgb = c.aaa;
                }
                return c;
            }
            ENDCG
        }
    }
        /*SubShader
        {
            Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }

            Pass
            {
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                struct Attributes
                {
                    float4 positionOS   : POSITION;
                    float2 uv           : TEXCOORD0;
                };

                struct Varyings
                {
                    float4 positionHCS  : SV_POSITION;
                    float2 uv           : TEXCOORD0;
                };

                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);

                CBUFFER_START(UnityPerMaterial)
                    float4 _MainTex_ST;
                CBUFFER_END

                Varyings vert(Attributes IN)
                {
                    Varyings OUT;
                    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                    OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                    return OUT;
                }

                half4 frag(Varyings IN) : SV_Target
                {
                    half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                    return color;
                }
                ENDHLSL
            }
        }*/
}
