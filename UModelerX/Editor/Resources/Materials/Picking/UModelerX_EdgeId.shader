Shader "Hidden/UModelerX/EdgeId"
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
            "LightMode" = "Always" 
            "UModelerXPicker" = "EdgeID" 
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
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _EdgeSize;
     
            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                // 다음점. w는 노말 벡터의 크로스에 가중치.
                float4 vertexnext : TEXCOORD2;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                
                float4 p0 = UnityObjectToClipPos(v.vertex);
                float4 p1 = UnityObjectToClipPos(v.vertexnext);

                float2 screenPos0 = p0.xy / p0.w * 0.5 + 0.5;
                float2 screenPos1 = p1.xy / p1.w * 0.5 + 0.5;

                screenPos0 *= float2(_ScreenParams.x, _ScreenParams.y);
                screenPos1 *= float2(_ScreenParams.x, _ScreenParams.y);

                float2 direction = normalize(screenPos1 - screenPos0);
                float2 perpendicular = float2(-direction.y, direction.x);

                float2 offset = perpendicular * _EdgeSize;
                float4 offsetClip = float4(offset / float2(_ScreenParams.x, _ScreenParams.y) * p0.w, 0, 0);

                o.pos = p0 + offsetClip * v.vertexnext.w;
                o.pos.z += 0.00003 * o.pos.w;
                o.color = v.color;

                return o;
            }

            half4 frag (v2f i) : COLOR
            {
                return i.color;
            }

            ENDCG
        }
	}
}
