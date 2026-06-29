Shader "Unlit/UModelerX_Backface"
{
    Properties
    {
        _MainColor("Color", Color) = (0.42, 0.42, 0.42,1)
        _TransparentColor("TransparentColor", Color) = (0.21, 0.21, 0.21, 0.5)
        _Alpha ("Alpha", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode("CullMode", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest [_ZTest]
            Cull [_CullMode]

            Offset -1, -1

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : TEXCOORD2;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                // float soft
            };

            float4 _MainColor;
            float4 _TransparentColor;
            float _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 붉은 색 선택인 경우
                if (i.color.r > 0.8 && i.color.g < 0.0001 && i.color.b < 0.0001)
                    return i.color;
                // 초록색 (선택된 평면의 경우)
                if (i.color.r < 0.1 && i.color.g > 0.8 && i.color.b < 0.1)
                    return i.color;
                if (_Alpha == 0)
                    return _TransparentColor;
                return _MainColor;
            }
            ENDCG
        }
    }
}
