Shader "Hidden/UModelerX/VertexGeometryId_HDRP"
{
    Properties
    {
        _VertexSize("VertexSize", float) = 2.5
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode("CullMode", Float) = 0
	}
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "LightMode" = "UModelerXPicker"
        }

        LOD 100

        Blend One OneMinusSrcAlpha
        Lighting Off
        ZWrite On
        ZTest On

        Cull Off
        Offset -1, -1

        Pass 
        {
            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geo
            #include "UnityCG.cginc"

            float _VertexSize;

            struct appdata
			{
				float4 vertex : POSITION;
                fixed4 color : COLOR;
			};

			struct v2g
			{
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };


			v2g vert (appdata v)
			{
				v2g o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.vertex.z += o.vertex.w * _VertexSize * 0.00002;

                o.color = v.color;

                return o;
			}


            [maxvertexcount(4)]
            void geo(point v2g input[1], inout TriangleStream<g2f> triStream)
            {
                float4 p = input[0].vertex;
                float2 pixelOffset = float2(_VertexSize * 10 / _ScreenParams.x, _VertexSize * 10 / _ScreenParams.y) * p.w;

                g2f output;

                output.color = input[0].color;
                output.pos = p + float4(-pixelOffset.x, -pixelOffset.y, 0, 0);
                triStream.Append(output);
                output.pos = p + float4(pixelOffset.x, -pixelOffset.y, 0, 0);
                triStream.Append(output);
                output.pos = p + float4(-pixelOffset.x, pixelOffset.y, 0, 0);
                triStream.Append(output);
                output.pos = p + float4(pixelOffset.x, pixelOffset.y, 0, 0);
                triStream.Append(output);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                return i.color;
			}
			ENDCG
        }
	}
}
