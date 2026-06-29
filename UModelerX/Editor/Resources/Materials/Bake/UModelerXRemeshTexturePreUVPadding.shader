Shader "Hidden/UModelerX_Remesh_TexturePreUVPadding"
{
    Properties
    {
        _SourceTex("Source Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off

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

            sampler2D _SourceTex;
            float4 _SourceTex_TexelSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 texelSize = _SourceTex_TexelSize.xy;
                int searchRadius = 3;

                fixed4 finalColor = fixed4(0, 0, 0, 0);
                float maxAlpha = 0.0;

                float2 centerUV = i.uv;

                for (int y = -searchRadius; y <= searchRadius; y++)
                {
                    for (int x = -searchRadius; x <= searchRadius; x++)
                    {
                        float2 sampleOffset = float2(x, y) * texelSize;
                        float2 sampleUV = centerUV + sampleOffset;

                        sampleUV = clamp(sampleUV, 0.0, 1.0);
                        fixed4 sampledColor = tex2Dlod(_SourceTex, fixed4( sampleUV, 0, 0));

                        if (sampledColor.a > maxAlpha)
                        {
                            finalColor = sampledColor;
                            maxAlpha = sampledColor.a;

                            if (maxAlpha >= 0.999) break;
                        }
                    }
                    if (maxAlpha >= 0.999) break;
                }

                // 현재 픽셀 자체가 유효한 색상을 가지고 있다면 그 색상을 우선 사용
                fixed4 ownColor = tex2Dlod(_SourceTex, fixed4( i.uv, 0, 0));
                if (ownColor.a > 0.0) {
                    finalColor = ownColor;
                }

                return finalColor;
            }
            ENDCG
        }
    }
}
