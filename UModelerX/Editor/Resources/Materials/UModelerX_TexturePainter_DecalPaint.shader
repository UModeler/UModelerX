Shader "Hidden/UModelerX_TexturePainter_DecalPaint"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull off
            ZTest always
            Blend [_SrcBlend] OneMinusSrcAlpha, [_SrcAlphaBlend] OneMinusSrcAlpha
            Colormask RGBA

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float3 worldpos : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            float3 _BrushPosition;
            float3 _BrushTangent;
            float3 _BrushBinormal;
            float3 _BrushNormal;
            float4 _BrushColor;
            float _FrontOnly;
            sampler2D _MaskTex;
            float _MaskTexType;
            float _UVSpace;

            float3 _CameraPosition;
            float3 _CameraDirection;
            float _Iso;

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = float4(v.uv * float2(2,-2) + float2(-1,1), 1, 1);
                o.uv = v.uv;
                o.worldpos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 worldpos = i.worldpos;
                float3 uvw;
                float front = 1;

                if (_UVSpace == 0)
                {
                    float3 normal = -normalize(cross(ddx(worldpos), ddy(worldpos)));
                    float3 cameradir = lerp(_CameraPosition - worldpos, -_CameraDirection, _Iso);
                    front = dot(cameradir, normal) >= 0 ? 1 : 0;

                    float3 v = worldpos.xyz - _BrushPosition;
                    uvw = float3(dot(v, _BrushTangent), dot(v, _BrushBinormal), dot(v, _BrushNormal));
                }
                else
                {
                    float2 p = i.uv - _BrushPosition.xy;
                    uvw = float3(dot(p, _BrushTangent.xy), dot(p, _BrushBinormal.xy), 0);
                }

                //uvw.y *= _MainTex_TexelSize.z / _MainTex_TexelSize.w;

                if (uvw.x >= -1 && uvw.x <= 1.0f && uvw.y >= -1 && uvw.y <= 1.0f)
                {
                    fixed4 col = tex2D(_MainTex, uvw.xy * float2(-0.5, 0.5) + 0.5) * _BrushColor;
                    col.a *= 1 - saturate((abs(uvw.z) - 0.6) / 0.4f);
                    col.a *= lerp(1, front, _FrontOnly);

                    col.a *= _MaskTexType == 1 ? tex2D(_MaskTex, i.uv).r : 1;
                    return float4(col.rgb*col.a, col.a);
                }
                return 0;
            }
            ENDCG
        }
    }
}
