Shader "Hidden/UModelerX_TexturePainter_BitMaskFillRect"
{
    Properties
    {
        _Color("Color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        // Pass 0 — Draw: premultiplied RGB, Blend One OneMinusSrcAlpha (PaintBrushArray 2D / Canvas2DDisplay 규약)
        Pass
        {
            Cull off
            ZTest always
            Blend [_SrcBlend] OneMinusSrcAlpha
            Colormask RGBA

            CGPROGRAM
            #pragma vertex vert_draw
            #pragma fragment frag_draw

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float alpha : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _BitMaskTex;
            float4 _BitMaskTex_TexelSize;

            float4 _Color;

            v2f vert_draw (appdata v)
            {
                v2f o;
                o.vertex = float4(v.uv.xy*float2(2,-2)+float2(-1,1), 1, 1);

                int3 index = floor(v.uv2.xxx * float3(1/32.0f, 1/8.0f, 1));

                float2 uv = floor(index.xx * float2(1, 1.0f / _BitMaskTex_TexelSize.z)) / _BitMaskTex_TexelSize.zw;
                float4 bitmask = tex2Dlod(_BitMaskTex, float4(frac(uv), 0, 0));

                int index1 = (index.y&3);
                int index2 = (index.z&7);
                int bitmaks8 = (int)((index1 < 2 ? (index1 == 0 ? bitmask.r : bitmask.g) : (index1 == 2 ? bitmask.b : bitmask.a)) * 255);
                o.alpha = (bitmaks8 & (1 << index2)) != 0 ? 1 : 0;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag_draw(v2f i) : SV_Target
            {
                float a = _Color.a * i.alpha;
                return float4(_Color.rgb * a, a);
            }
            ENDCG
        }

        // Pass 1 — Erase: straight alpha + RGB/A 분리 블렌드 (기존 단일 패스와 동일)
        Pass
        {
            Cull off
            ZTest always
            Blend [_SrcBlend] OneMinusSrcAlpha,[_SrcAlphaBlend] OneMinusSrcAlpha
            Colormask RGBA

            CGPROGRAM
            #pragma vertex vert_erase
            #pragma fragment frag_erase

            #include "UnityCG.cginc"

            struct appdata_e
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float3 normal : NORMAL;
            };

            struct v2f_e
            {
                float alpha : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _BitMaskTex;
            float4 _BitMaskTex_TexelSize;

            float4 _Color;

            v2f_e vert_erase (appdata_e v)
            {
                v2f_e o;
                o.vertex = float4(v.uv.xy*float2(2,-2)+float2(-1,1), 1, 1);

                int3 index = floor(v.uv2.xxx * float3(1/32.0f, 1/8.0f, 1));

                float2 uv = floor(index.xx * float2(1, 1.0f / _BitMaskTex_TexelSize.z)) / _BitMaskTex_TexelSize.zw;
                float4 bitmask = tex2Dlod(_BitMaskTex, float4(frac(uv), 0, 0));

                int index1 = (index.y&3);
                int index2 = (index.z&7);
                int bitmaks8 = (int)((index1 < 2 ? (index1 == 0 ? bitmask.r : bitmask.g) : (index1 == 2 ? bitmask.b : bitmask.a)) * 255);
                o.alpha = (bitmaks8 & (1 << index2)) != 0 ? 1 : 0;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag_erase(v2f_e i) : SV_Target
            {
                return float4(_Color.rgb, _Color.a * i.alpha);
            }
            ENDCG
        }
    }
}
