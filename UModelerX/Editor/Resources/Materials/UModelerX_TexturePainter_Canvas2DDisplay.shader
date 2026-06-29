Shader "Hidden/UModelerX_TexturePainter_Canvas2DDisplay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            // 페인트 파이프라인(ShapeRect/ShapeEllipse/ShapeLine/Gradient/FontRender 등)이
            // premultiplied alpha 로 레이어 RT 에 저장하므로 표시도 premultiplied 로 블렌드.
            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                // _Color.a = 레이어 opacity. premultiplied 데이터는 RGB·A 에 동일 스케일을 곱해야
                // invariant (rgb = origRgb * a) 가 유지됨.
                c *= _Color.a;
                // _Color.rgb 는 선택적 RGB 틴트 — 알파에는 영향 없음.
                c.rgb *= _Color.rgb;
                // 페인트 RT는 linear 값을 저장하고 있으며, Unity 에디터 GUI 의 framebuffer 쓰기에서
                // 필요한 변환은 Unity 가 처리한다. 추가 감마 변환을 적용하면 mid-tone 이 과도하게
                // 어두워지므로 여기선 conversion 없이 통과시킨다.
                return c;
            }
            ENDCG
        }
    }
}
