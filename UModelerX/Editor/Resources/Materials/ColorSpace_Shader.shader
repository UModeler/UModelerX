Shader "Custom/ColorSpace" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        [HideInInspector][Toggle] _ColorSpaceGamma ("Color Space Gamma", Float) = 0
        [HideInInspector][Toggle] _EnableAlphaChannel ("Enable Alpha Channel", Float) = 1
    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float _ColorSpaceGamma;
            float _EnableAlphaChannel;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 color = tex2D(_MainTex, i.uv);

                if (_ColorSpaceGamma == 1)
                {
                    color.rgb = pow(color.rgb, 1.0 / 2.2);
                }

                if (_EnableAlphaChannel == 0)
                {
                    color.a = 1;
                }

                return color;
            }
            ENDCG
        }
    }
}