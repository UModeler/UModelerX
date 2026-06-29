Shader "Hidden/UModelerX/DrawUVVertex"
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
            #include "UnityCG.cginc"
            #include "UModelerX.cginc"

            float _VertexSize;
            float4 _OrthoParams;

            struct appdata
			{
                float4 vertex : POSITION;
				float2 uvPos : TEXCOORD0;
                fixed4 color : COLOR;
                float2 vertexRect : TEXCOORD2;
			};

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

			v2f vert (appdata v)
			{
				v2f o;

                float4 p = UVToPos(v.uvPos);
                o.color = v.color;
                float2 pixelOffset = float2(_VertexSize * 2.0f / _OrthoParams.x, _VertexSize * 2.0f / _OrthoParams.y) * v.vertexRect;

                o.pos = p + float4(pixelOffset.x, pixelOffset.y, 0, 0);
                return o;
			}

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color;
			}
			ENDCG
        }
	}
    FallBack "Diffuse"
}
