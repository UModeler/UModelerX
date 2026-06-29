Shader "Hidden/UModelerX/DrawUVEdgePainting"
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
        ZWrite On
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
            #include "UnityCG.cginc"
            #include "UModelerX.cginc"

            float4 _OrthoParams;
            float4 _EdgeWireframeColor;
            float _EdgeThickness;
            float4 _ClipPlane;

            struct appdata
			{
				float2 uvPos : TEXCOORD0;
				float3 uvNextPos : TEXCOORD2;
			};

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

			v2f vert (appdata v)
			{
                v2f o;

                float4 p0 = UnityObjectToClipPos(float4(v.uvPos, 0, 1));
                float4 p1 = UnityObjectToClipPos(float4(v.uvNextPos.xy, 0, 1));
                float2 direction = normalize(p1 - p0) * _EdgeThickness;
                float4 offset = float4(-direction.y / _OrthoParams.x, direction.x / _OrthoParams.y, 0, 0);

                o.pos = p0 + offset * v.uvNextPos.z;

                return o;
			}

            float4 frag(v2f i) : SV_Target
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
    FallBack "Diffuse"
}
