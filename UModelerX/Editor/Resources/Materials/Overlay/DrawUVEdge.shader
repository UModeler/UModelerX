Shader "Hidden/UModelerX/DrawUVEdge"
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
            #include "UnityCG.cginc"
            #include "UModelerX.cginc"

            float4 _OrthoParams;
         
            struct appdata
			{
				float2 uvPos : TEXCOORD0;
				float3 uvNextPos : TEXCOORD2;
                fixed4 color : COLOR;
                // x는 두께
                // y는 위로 올라오는 Z값
				float2 edgeData : TEXCOORD3;
			};

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

			v2f vert (appdata v)
			{
				v2f o;

                float4 p0 = UVToPos(v.uvPos);
                float4 p1 = UVToPos(v.uvNextPos);
                float2 direction = normalize(p1 - p0) * v.edgeData.x;
                float4 offset = float4(-direction.y / _OrthoParams.x, direction.x / _OrthoParams.y, 0, 0);

                p0.z -= v.edgeData.y;
                o.pos = p0 + offset * v.uvNextPos.z;
                o.color = v.color;

                return o;
			}

            float4 frag(v2f i) : SV_Target
            {
                return i.color;
			}
			ENDCG
        }
	}
    FallBack "Diffuse"
}
