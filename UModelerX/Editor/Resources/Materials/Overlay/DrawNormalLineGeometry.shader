Shader "Hidden/UModelerX/DrawNormalLineGeometry"
{
    Properties
    {
		[Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0
		_ZOffset("ZOffset", Float) = 0.00003
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode("CullMode", Float) = 0
	}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Blend One OneMinusSrcAlpha
        Lighting Off
        ZWrite Off
        ZTest[_ZTest]

        Cull[_CullMode]
        Offset -1, -1

        Pass 
        {
	        Name "NormalLineGeometry"

            CGPROGRAM
            #pragma target 4.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geo
            #include "UnityCG.cginc"

            float _ZOffset;
           
            struct appdata
			{
				float4 vertex : POSITION;
                float4 color : TEXCOORD2;
                // x는 두께 (픽셀 단위)
				float2 lineData : TEXCOORD0;
			};

			struct v2g
			{
                float4 vertex : POSITION;
                fixed4 color : COLOR;
				float2 lineData : TEXCOORD0;
			};

            struct g2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

			v2g vert (appdata v)
			{
				v2g o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.vertex.z += _ZOffset * o.vertex.w;
                o.color = v.color;
                o.lineData = v.lineData;

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

                float2 offset = perpendicular * input[0].lineData.x;
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

            float4 frag(g2f i) : SV_Target
            {
                return i.color;
			}
			ENDCG
        }
	}
}
