Shader "Hidden/UModelerX_TexturePainter_OverlayAlphaCompositeWithSelectionTransform"
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
            float4x4 _WorldToContent;

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
                float2 outputUV = i.uv;
                float4 baseColor = tex2D(_BaseTex, outputUV);

                float4 srcH = mul(_WorldToContent, float4(outputUV, 0, 1));
                float2 contentUV = srcH.xy / srcH.w;

                if (contentUV.x < 0.0 || contentUV.x > 1.0 || contentUV.y < 0.0 || contentUV.y > 1.0)
                {
                    return baseColor;
                }

                float4 overlayColor = tex2D(_OverlayTex, contentUV);
                if (overlayColor.a < 0.00392156862)
                {
                    return baseColor;
                }
                if (baseColor.a < 0.00392156862)
                {
                    return overlayColor;
                }
                float oa = overlayColor.a;
                float4 composite;
                composite.rgb = overlayColor.rgb * oa + baseColor.rgb * (1.0 - oa);
                composite.a = oa + baseColor.a * (1.0 - oa);
                return composite;
            }
            ENDCG
        }
    }
}
