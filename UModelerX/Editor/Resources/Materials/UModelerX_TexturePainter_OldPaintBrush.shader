Shader "Hidden/UModelerX_TexturePainter_OldPaintBrush"
{
    Properties
    {
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float3 worldpos : TEXCOORD0;
                float3 worldnormal : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            float3 _BrushPosition;
            float _BrushRadius;
            float _BrushHardness;
            float4 _BrushColor;
			float _FrontOnly;
            float _BrushShape; // 0 sphere 1 square

            float _UVSpace;
            float2 _TextureSpace;

            float3 _CameraPosition;
            float3 _CameraDirection;
            float _Iso;
            float3 _CameraUp;

            v2f vert (appdata v)
            {
                v2f o;
                o.uv = v.uv;
                o.vertex = float4(v.uv*float2(2,-2)+float2(-1,1), 1, 1);
                o.worldnormal = mul((float3x3)unity_ObjectToWorld, v.normal);
                if (_UVSpace == 1)
                    o.worldpos = float3(v.uv * _TextureSpace, 0);
                else
                    o.worldpos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 v = _BrushPosition.xyz - i.worldpos.xyz;
                float3 worldpos = i.worldpos;
                float3 normal = i.worldnormal;
                float3 cameradir = lerp(_CameraPosition - worldpos, -_CameraDirection, _Iso);
                float front = dot(cameradir, normal) >= 0 ? 1 : 0;
                float3 right = normalize(cross(_CameraUp, cameradir));
                float3 up = normalize(cross(cameradir, right));

                if (_UVSpace == 1)
                {
                    right = float3(1, 0, 0);
                    up = float3(0, 1, 0);
                }

                float alpha;

                if (_BrushShape == 1)
                {
                    float3 forward = cross(up, right);
                    float w = max(abs(dot(right, v)), abs(dot(up, v))) / _BrushRadius;
                    alpha = saturate(1 - lerp(w, max(0, (w - 0.8) / 0.2), _BrushHardness)) *saturate(1 - (abs(dot(forward, v)) / _BrushRadius - 0.7) / 0.3);
                }
                else
                {
                    float w = length(v) / _BrushRadius;
                    alpha = saturate(1 - lerp(w, max(0, (w - 0.8) / 0.2), _BrushHardness)) * saturate(1 - (abs(dot(normal, v)) / _BrushRadius - 0.7) / 0.3);
                }
                
                float4 c = float4(_BrushColor.rgb, _BrushColor.a * alpha * lerp(1, front, _FrontOnly));
                return c;
            }
            ENDCG
        }
    }
}
