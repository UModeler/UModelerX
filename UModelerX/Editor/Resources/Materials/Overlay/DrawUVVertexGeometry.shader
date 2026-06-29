Shader "Hidden/UModelerX/DrawUVVertexGeometry"
{
    Properties
    {
        _VertexSize("VertexSize", float) = 0.5
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode("CullMode", Float) = 0
	}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Blend One OneMinusSrcAlpha
        Lighting Off
        ZWrite Off
        ZTest Always

        Cull Off
        Offset -1,-1

        Pass 
        {
	        Name "VertexRectGeometry"

            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geo
            #include "UnityCG.cginc"
            #include "UModelerX.cginc"

            float _VertexSize;
            float4 _OrthoParams;

            struct appdata
			{
                float4 vertex : POSITION;
				float2 uvPos : TEXCOORD0;
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
                float4 color : COLOR;
            };

			v2g vert (appdata v)
			{
				v2g o;

                o.vertex = UVToPos(v.uvPos);
                o.color = v.color;

                return o;
			}

            [maxvertexcount(4)]
            void geo(point v2g input[1], inout TriangleStream<g2f> triStream)
            {
                float4 p = input[0].vertex;
                float2 pixelOffset = float2(_VertexSize * 2.0f / _OrthoParams.x, _VertexSize * 2.0f / _OrthoParams.y);

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
