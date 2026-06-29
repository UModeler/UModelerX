Shader "Hidden/UModelerX_TexturePaintPreview"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}

        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { 
            "RenderType"="Opaque" 
            "LightMode" = "Always" 
            "UModelerXPreview" = "Preview"
        }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows
        #pragma multi_compile __ _USE_MASKTEXTURE        

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        sampler2D _MetallicTex;
        sampler2D _SmoothnessTex;
        sampler2D _OcclusionTex;
        sampler2D _EmissionTex;

        sampler2D _NormalTex;
        sampler2D _HeightTex;

        sampler2D _TexOnly;

        float4 _Mask; // metallic, smoothness, Occlusion, Emission
        float4 _Mask2; // normal, Height
        float _Type;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;

        //fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            //int mipmap = 0;

            if (_Type == 1)
            {
                o.Albedo = tex2D(_MainTex, IN.uv_MainTex);
                o.Metallic = lerp(_Metallic, tex2D(_MetallicTex, IN.uv_MainTex).r, _Mask.r);
                o.Smoothness = lerp(_Glossiness, tex2D(_SmoothnessTex, IN.uv_MainTex).r, _Mask.g);
                o.Occlusion = lerp(1, tex2D(_OcclusionTex, IN.uv_MainTex).r, _Mask.b);
                o.Emission = lerp(float3(0, 0, 0), tex2D(_EmissionTex, IN.uv_MainTex).rgb, _Mask.a);

                if (_Mask2.r > 0)
                    o.Normal = tex2D(_NormalTex, IN.uv_MainTex) * 2 - 1;
            }
            else if (_Type == 2)
            {
                //float4 c = mipmap >= 0 ? tex2Dlod(_TexOnly, float4(IN.uv_MainTex, 0, mipmap)) : tex2D(_TexOnly, IN.uv_MainTex);
                float4 c = tex2D(_TexOnly, IN.uv_MainTex);
                o.Albedo = 0;
                o.Emission = c.rgb;
            }
            else if (_Type == 3)
            {
                //float4 c = mipmap >= 0 ? tex2Dlod(_TexOnly, float4(IN.uv_MainTex, 0, mipmap)) : tex2D(_TexOnly, IN.uv_MainTex);
                float4 c = tex2D(_TexOnly, IN.uv_MainTex);
                o.Albedo = 0;
                o.Emission = c.rrr;
            }
            else if (_Type == 4)
            {
                float2 uv = saturate(IN.uv_MainTex.xy);
                float2 w = step(uv, 1) * step(0, uv);
                o.Emission = float3(uv, 1 - w.x * w.y);
                //o.Emission = lerp(float3(uv,0), float3(0,0,1), 1 - w.x * w.y);
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
