Shader "Hidden/UModelerX/VertexId_HDRP"
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
            #include "UnityCG.cginc"

            float _VertexSize;
            float3 _CameraPos;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 vertexRect : TEXCOORD0;
			};

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;

                float4 p = UnityObjectToClipPos(v.vertex);
                p.z += p.w * _VertexSize * 0.00002 ;
                o.color = v.color;
    
                float2 pixelOffset = float2(_VertexSize / _ScreenParams.x, _VertexSize / _ScreenParams.y) * p.w * v.vertexRect;
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
}
