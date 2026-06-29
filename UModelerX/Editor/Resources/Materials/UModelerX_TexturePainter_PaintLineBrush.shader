Shader "Hidden/UModelerX_TexturePainter_PaintLineBrush"
{
    Properties
    {
        _BrushColor("Brush Color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent"}
        LOD 100

        Pass
        {
            Cull off
            ZWrite Off
            ZTest always
            // Blend[_SrcBlend] OneMinusSrcAlpha,[_SrcAlphaBlend] OneMinusSrcAlpha
            Blend[_SrcBlend] OneMinusSrcAlpha
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

            sampler2D _SourceTex;

            float3 _BrushPositionArray[64];
            float _BrushRadiusArray[64];
            float4 _BrushColorArray[64];
            int _BrushCount;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.uv = v.uv;
                o.vertex = float4(v.uv * float2(2,-2) + float2(-1,1), 1, 1);
                o.worldnormal = mul((float3x3)unity_ObjectToWorld, v.normal);
                if (_UVSpace == 1)
                    o.worldpos = float3(v.uv * _TextureSpace, 0);
                else
                    o.worldpos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            float BrushIntensity(float distance, float hardness, float brushradius)
            {
                if (hardness >= 1.0 - 1e-5)
                    return (distance < brushradius) ? 1.0 : 0.0;

                // --- 포토샵식 퍼짐 세팅 ---
                float coreRatio = lerp(0.0, 1.0, hardness);        // 0→0.25, 0.5→0.75, 1→1.0
                float edgeFalloff = max(1e-5, 1.0 - coreRatio);
                float outerRange = lerp(0.30, 0.00, hardness);       // 0→15% 퍼짐, 0.5→7.5%, 1→0
                float effectiveRadius = brushradius * (1.0 + outerRange);

                // --- 완전 바깥은 0 ---
                if (distance >= effectiveRadius)
                    return 0.0;

                // --- 정규화 거리 ---
                float w = distance / brushradius;

                // --- 에지 감쇠 비율 (0~1) ---
                float t = saturate((w - coreRatio) / edgeFalloff);
                float tExp = lerp(1.40, 1.00, hardness);
                t = pow(t, tExp);

                // --- 가우시안 강도 ---
                // float sigma = lerp(0.60, 0.10, pow(hardness, 1.2));
                float g1_soft = 0.035;
                float g1_hard = 0.0;
                float g1_target = lerp(g1_soft, g1_hard, hardness);
                g1_target = saturate(g1_target);
                float sigma = sqrt(1.0 / max(1e-6, 2.0 * log(1.0 / max(1e-6, g1_target))));

                float g = exp(-(t * t) / (2.0 * sigma * sigma));
                float g1 = exp(-(1.0) / (2.0 * sigma * sigma));

                // --- 반경 안 ---
                if (distance <= brushradius)
                    return g;

                // --- 반경 밖(테일): 포토샵식 옅은 꼬리 ---
                float u = (distance - brushradius) / (brushradius * outerRange + 1e-6);
                float k = lerp(2.0, 10.0, hardness);
                float tail = g1 * exp(-k * u);

                return saturate(tail);

                /*float w = distance / brushradius;
                float r1 = 0.3 + 0.7 * hardness;
                return 1-saturate((w - r1) / (1-r1));*/
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 worldpos = i.worldpos;
                float3 normal = i.worldnormal;
                float3 cameradir = lerp(_CameraPosition - worldpos, -_CameraDirection, _Iso);
                float front = dot(cameradir, normal) >= 0 ? 1 : 0;
                float3 rawRight = normalize(cross(_CameraUp, -_CameraDirection));

                float3 right, up;

                if (_UVSpace == 1)
                {
                    right = float3(1, 0, 0);
                    up = float3(0, 1, 0);
                }
                else
                {
                    right = rawRight - dot(rawRight, normal) * normal;

                    right = normalize(right + 1e-5);
                    up = normalize(cross(normal, right));
                }

                float alpha = 0;
                float3 color = 0;

                for (int j = 0; j < _BrushCount; j+=2)
                {
                    float3 v0 = _BrushPositionArray[j].xyz;
                    float brushradius0 = _BrushRadiusArray[j];
                    float4 c0 = _BrushColorArray[j];
    
                    float3 v1 = _BrushPositionArray[j+1].xyz;
                    float brushradius1 = _BrushRadiusArray[j+1];
                    float4 c1 = _BrushColorArray[j+1];

                    float t = saturate(dot(worldpos.xyz - v0, v1 - v0) / dot(v1 - v0, v1 - v0));
                    float3 v = lerp(v0, v1, t) - worldpos.xyz;
                    float brushradius = lerp(brushradius0, brushradius1, t);
                    float4 c = lerp(c0, c1, t);
                    float a;

                    if (_BrushShape == 1) // 사각형
                    {
                        float3 forward = cross(up, right);
                        float depthFade = saturate(1 - (abs(dot(forward, v)) / brushradius - 0.7) / 0.3);
                        float w = max(abs(dot(right, v)), abs(dot(up, v)));
                        a = BrushIntensity(w, _BrushHardness, brushradius) * depthFade;
                    }
                    else
                    {
                        float w = length(v);
                        a = BrushIntensity(w, _BrushHardness, brushradius);
                    }
                    a *= c.a;
                    if (a > alpha)
                    {
                        color = c.rgb;
                        alpha = a;
                    }
                }
                float4 blend = float4(color, alpha) * lerp(1, front, _FrontOnly);
                /*float4 base = tex2Dlod(_SourceTex, float4(i.uv, 0, 0));
                float3 baseRGB = base.a == 0 ? 0 : base.rgb / base.a;

                float3 finalColor = blend.rgb + base.rgb * (1 - blend.a);
                float finalAlpha = blend.a + base.a * (1 - blend.a);
                return fixed4(finalColor, finalAlpha);*/

                return fixed4(blend.rgb * blend.a, blend.a);
            }
            ENDCG
        }
    }
}
