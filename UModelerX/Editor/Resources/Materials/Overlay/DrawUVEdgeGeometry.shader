Shader "Hidden/UModelerX/DrawUVEdgeGeometry"
{
    Properties
    {
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode("CullMode", Float) = 0
	}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Lighting Off
        ZWrite On
        ZTest GEqual

        Cull Off
        Offset -1, -1

        Pass 
        {
	        Name "UVEdgeGeometry"

            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geo
            #include "UnityCG.cginc"
            #include "UModelerX.cginc"

            float4 _OrthoParams;
         
            struct appdata
			{
				float2 uvPos : TEXCOORD0;
                fixed4 color : COLOR;
                // x는 두께
                // y는 위로 올라오는 Z값
				float2 edgeData : TEXCOORD2;
			};

			struct v2g
			{
                float4 vertex : POSITION;
                fixed4 color : COLOR;
				float2 edgeData : TEXCOORD2;
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
                o.vertex.z -= v.edgeData.y;
                o.color = v.color;
                o.edgeData = v.edgeData;

                return o;
			}

            [maxvertexcount(4)]
            void geo(line v2g input[2], inout TriangleStream<g2f> triStream)
            {
                float4 p0 = input[0].vertex;
                float4 p1 = input[1].vertex;

                float2 direction = normalize(p1 - p0) * input[0].edgeData.x;

                g2f output;

                float4 offset = float4(-direction.y / _OrthoParams.x, direction.x / _OrthoParams.y, 0, 0);

                output.color = input[0].color;

                output.pos = p0 - offset;
                triStream.Append(output);

                output.pos = p0 + offset;
                triStream.Append(output);

                output.color = input[1].color;

                output.pos = p1 - offset;
                triStream.Append(output);

                output.pos = p1 + offset;
                triStream.Append(output);
            }

            float4 frag(g2f i) : SV_Target
            {
                return i.color;
			}
			ENDCG
        }
	}
}
