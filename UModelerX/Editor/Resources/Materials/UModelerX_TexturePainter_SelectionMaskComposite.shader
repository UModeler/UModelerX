Shader "Hidden/UModelerX_TexturePainter_SelectionMaskComposite"
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

            sampler2D _BaseTex;
            sampler2D _OverlayTex;
            sampler2D _MaskTex;
            float2 _MaskOffset;
            float _UseOverlayAlpha;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 sampleUV = i.uv;
                float4 baseColor = tex2D(_BaseTex, sampleUV);
                float4 overlayColor = tex2D(_OverlayTex, sampleUV);
                float4 maskColor = tex2D(_MaskTex, sampleUV - _MaskOffset);

                if (_UseOverlayAlpha > 0.5)
                {
                    if (maskColor.r < 0.5)
                    {
                        return baseColor;
                    }
                    if (overlayColor.a < 0.00392156862)
                    {
                        return baseColor;
                    }
                    if (baseColor.a < 0.00392156862)
                    {
                        return overlayColor;
                    }
                    float4 composite;
                    float oa = overlayColor.a;
                    composite.rgb = overlayColor.rgb * oa + baseColor.rgb * (1.0 - oa);
                    composite.a = oa + baseColor.a * (1.0 - oa);
                    return composite;
                }

                float blendFactor = maskColor.r;
                return lerp(baseColor, overlayColor, blendFactor);
            }
            ENDCG
        }
    }
}
