Shader "Hidden/UModelerX/EdgeGeometryId_HDRP"
{
    Properties
    {
        _EdgeSize("EdgeSize", float) = 2.0
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

            float _EdgeSize;
      
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
                o.vertex.z += 0.00003 * o.vertex.w;
                o.color = v.color;

                return o;
			}

            [maxvertexcount(4)]
            void geo(line v2g input[2], inout TriangleStream<g2f> triStream)
            {
                float4 p0 = input[0].vertex;
                float4 p1 = input[1].vertex;

                float2 screenPos0 = p0.xy / p0.w * 0.5 + 0.5;
                float2 screenPos1 = p1.xy / p1.w * 0.5 + 0.5;
                screenPos0 *= float2(_ScreenParams.x, _ScreenParams.y);
                screenPos1 *= float2(_ScreenParams.x, _ScreenParams.y);

                float2 direction = normalize(screenPos1 - screenPos0);
                float2 perpendicular = float2(-direction.y, direction.x);

                g2f output;

                float2 offset = perpendicular * _EdgeSize;
                float4 offsetClip0 = float4(offset / float2(_ScreenParams.x, _ScreenParams.y) * p0.w, 0, 0);
                float4 offsetClip1 = float4(offset / float2(_ScreenParams.x, _ScreenParams.y) * p1.w, 0, 0);

                output.color = input[0].color;

                output.pos = p0 - offsetClip0;
                triStream.Append(output);

                output.pos = p0 + offsetClip0;
                triStream.Append(output);

                output.color = input[1].color;

                output.pos = p1 - offsetClip1;
                triStream.Append(output);

                output.pos = p1 + offsetClip1;
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