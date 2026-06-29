Shader "Hidden/UModeler_CheckerShader"
{
    Properties
    {

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZWrite Off Cull Off Blend One Zero

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            float2 _RectMin;
            float2 _RectSize;
            float  _CellSize;
            float2 _ViewPivotPix;
            float3 _ColorA;
            float3 _ColorB;

            struct appdata {
                float4 vertex : POSITION;
            };
            struct v2f {
                float4 pos : SV_Position;
                float2 screenXY : TEXCOORD0;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenXY = v.vertex.xy;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float2 localPix = i.screenXY - _RectMin;
                float2 scrolledPix = localPix - _ViewPivotPix;

                float2 cell = scrolledPix / max(_CellSize, 1e-5);
                int parity = ((int)floor(cell.x) + (int)floor(cell.y)) & 1;

                float3 c = (parity == 0) ? _ColorA : _ColorB;
                return float4(c, 1);
            }
            ENDCG
        }
    }
}
