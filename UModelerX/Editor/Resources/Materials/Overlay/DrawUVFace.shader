Shader "Hidden/UModelerX/DrawUVFace"
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
        ZWrite Off
        ZTest Always

        Cull Off
        Offset -1, -1

        Pass
        {
            Name "Face"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
            #include "UModelerX.cginc"

			struct appdata
			{
				float2 uvPos : TEXCOORD0;
                fixed4 color : COLOR;
			};

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

			v2f vert (appdata v)
			{
				v2f o;

                o.pos = UVToPos(v.uvPos);
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
}
