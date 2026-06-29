Shader "Hidden/UModelerX/DrawUVEdgeGeometryPainting"
{
    Properties
    {
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode("CullMode", Float) = 0
        _ClipPlane("Clip Plane", Vector) = (0, 0.5, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Lighting Off
        ZWrite Off
        ZTest Always

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

            float2 _OrthoParams;
            float4 _EdgeWireframeColor;
            float _EdgeThickness;
            float4 _ClipPlane;

            struct appdata
			{
				float2 uvPos : TEXCOORD0;
			};

			struct v2g
			{
                float4 vertex : POSITION;
			};

            struct g2f
            {
                float4 pos : SV_POSITION;
            };

			v2g vert (appdata v)
			{
				v2g o;

                o.vertex = UnityObjectToClipPos(float4(v.uvPos, 0, 1));

                return o;
			}

            [maxvertexcount(4)]
            void geo(line v2g input[2], inout TriangleStream<g2f> triStream)
            {
                float4 p0 = input[0].vertex;
                float4 p1 = input[1].vertex;

                float2 direction = normalize(p1 - p0) * _EdgeThickness;

                g2f output;

                float4 offset = float4(-direction.y / _OrthoParams.x, direction.x / _OrthoParams.y, 0, 0);

                output.pos = p0 - offset;
                triStream.Append(output);

                output.pos = p0 + offset;
                triStream.Append(output);

                output.pos = p1 - offset;
                triStream.Append(output);

                output.pos = p1 + offset;
                triStream.Append(output);
            }

            float4 frag(g2f i) : SV_Target
            {
                if (i.pos.y < _ClipPlane.y) {
                    return float4(0, 0, 0, 0);
                }
                else
                {
                    return _EdgeWireframeColor;
                }
			}
			ENDCG
        }
	}
}
