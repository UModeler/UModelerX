Shader "UModelerX_TextureSpace"
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
			Blend One Zero, One Zero

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
                float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
                float4 position : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 worldposition : TEXCOORD2;
                float3 worldnormal : TEXCOORD3;
            };

            sampler2D _MainTex;
            float _MaskOnly;
            float4x4 _modelviewprojection;
            float4x4 _model;
            float3 _cameraposition;

            float4 _rect;
            float _DepthTest;
            sampler2D _DepthTex;
            float4 _DepthTex_TexelSize;

            float _MaskTest; // 마스크 사용 1 uv 마스크 2 알파에 mask 저장
            float _MaskSpace; // 0 uv 1 스크린
//            float _MaskResult;
            float _DepthBias;
            float _DepthNear;
            float _DepthFar;
            sampler2D _MaskTex;

			v2f vert (appdata v)
			{
				v2f o;
                o.vertex = float4(v.uv * float2(2, -2) + float2(-1, 1), 0.5, 1);
                o.position = mul(_modelviewprojection, float4(v.vertex.xyz, 1));
                o.worldposition = mul(_model, float4(v.vertex.xyz, 1));
                o.worldnormal = mul((float3x3)_model, v.normal.xyz);
                o.uv = v.uv;
				return o;
			}

            float CalcDepthFromZ(float z, float near, float far)
            {
                return 1.0 / (z * (1.0 / near - 1.0 / far) + 1.0 / far);
            }
			
            fixed4 frag(v2f i) : SV_Target
            {
                float3 pos = i.position.xyz / i.position.w;
                pos.y = -pos.y;
                float2 coord = pos.xy * 0.5 + 0.5;
                float4 mask = float4(1,1,1,1);
                
                // 캡쳐 영역 체크
                float2 localpos = (coord - _rect.xy) / _rect.zw;
                if (localpos.x < 0 || localpos.x > 1 || localpos.y < 0 || localpos.y > 1)
                {
                    if (_MaskOnly == 0)
                        discard;
                    return 0;
                }

                // 깊이 비교
                coord.y = 1 - coord.y;
                if (_DepthTest > 0)
                {
                    float3 worldpos = i.worldposition.xyz;
                    float3 x = ddx(worldpos);
                    float3 y = ddy(worldpos);
                    float3 normal = normalize(cross(y, x));

                    if (dot(normal, i.worldnormal) < 0)
                        normal = -normal;

                    if (dot(_cameraposition - worldpos, normal) < 0)
                    {
                        if (_MaskOnly == 0)
                            discard;
                        mask.gb = 0;
                    }
                    else
                    {
                        float depth = i.position.w;
                        float4 texsize = float4(1, 1, -1, -1) * _DepthTex_TexelSize.xyxy;
                        float depthbias = _DepthBias;

                        float depth0 = CalcDepthFromZ(tex2D(_DepthTex, coord).x, _DepthNear, _DepthFar);
                        float depth1 = CalcDepthFromZ(tex2D(_DepthTex, coord + texsize.xy).x, _DepthNear, _DepthFar);
                        float depth2 = CalcDepthFromZ(tex2D(_DepthTex, coord + texsize.xw).x, _DepthNear, _DepthFar);
                        float depth3 = CalcDepthFromZ(tex2D(_DepthTex, coord + texsize.zy).x, _DepthNear, _DepthFar);
                        float depth4 = CalcDepthFromZ(tex2D(_DepthTex, coord + texsize.zw).x, _DepthNear, _DepthFar);

                        float depthtest =
                            smoothstep(depthbias*0.25, depthbias, depth - depth0) +
                            smoothstep(depthbias*0.25, depthbias, depth - depth1) +
                            smoothstep(depthbias*0.25, depthbias, depth - depth2) +
                            smoothstep(depthbias*0.25, depthbias, depth - depth3) +
                            smoothstep(depthbias*0.25, depthbias, depth - depth4);

                        if (depthtest >= 3)
                        {
                            if (_MaskOnly == 0)
                                discard;
                            mask.gb = 0;
                        }
                    }
                }

                // 마스크만 적용 (화면좌표)
                float2 uv = saturate(localpos);
                uv.y = 1 - uv.y;

                if (_MaskTest == 1 && tex2D(_MaskTex, lerp(i.uv, uv, _MaskSpace)).r == 0)
                {
                    if (_MaskOnly == 0)
                        discard;
                    mask.b = 0;
                }

                // 마스크만 혹은 결과물 텍스처에서 읽기
                if (_MaskOnly != 0)
                    return mask;

                float4 result = tex2D(_MainTex, uv);

                if (_MaskTest == 2)
                    result.rgba *= tex2D(_MaskTex, lerp(i.uv, uv, _MaskSpace)).r;
                return result;
			}
			ENDCG
		}
    }
}
