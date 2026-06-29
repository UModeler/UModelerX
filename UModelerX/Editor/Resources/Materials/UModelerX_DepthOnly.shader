Shader "UModelerX_DepthOnly"
{
    SubShader
    {
        Tags {
            "RenderType" = "Opaque"
            "LightMode" = "Always"
            "UModelerXDepth" = "Depth"
        }
        LOD 100

        Pass //"Depth"
        {
			Blend One Zero

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 position : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = o.position = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                float depth = i.position.z / i.position.w;
                return float4(saturate(depth.xxx), 1);
			}
			ENDCG
		}
    }
}
