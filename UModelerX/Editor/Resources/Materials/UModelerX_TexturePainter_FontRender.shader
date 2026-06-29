Shader "Hidden/UModelerX_TexturePainter_FontRender"
{
    Properties
    {
        _FontTex ("Font Atlas", 2D) = "white" {}
        _Color ("Text Color", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" }
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha  // premultiplied alpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _FontTex;
            float4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float glyphA = tex2D(_FontTex, i.uv).a;
                float alpha = step(0.5, glyphA) * _Color.a;
                return float4(_Color.rgb * alpha, alpha);  // premultiplied alpha
            }
            ENDCG
        }
    }
}
