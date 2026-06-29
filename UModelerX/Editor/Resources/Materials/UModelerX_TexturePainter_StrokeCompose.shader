// Stroke 효과 합성 — JFA 거리 필드 기반.
//   _SeedA: 가장 가까운 α>0 픽셀의 좌표 (Outside 거리 계산용).
//   _SeedB: 가장 가까운 α=0 픽셀의 좌표 (Inside  거리 계산용).
//   stroke_w_out = (distA <= _SizeOutside)             ? 1 : 0
//   stroke_w_in  = (srcA > 0 && distB <= _SizeInside)  ? 1 : 0
//   stroke_w 는 reach 안에서 1, 밖에서 0 (sharp boundary, 인공 ring 없음).
// Compose (Normal blend):
//   - Outside: source-over with stroke BELOW src.
//              (1 - src.α) 가 blend ratio → 코어(α=1) 보존, 페어더는 자연 blend, 빈 영역은 풀 stroke.
//   - Inside : src 색을 stroke 로 lerp, alpha 는 src.α 로 clipping (painted 안쪽만).
//   - Center : outside-band 와 inside-band 우세에 따라 각각 분기.
// Non-Normal blend: stroke_w 를 stroke 강도로 사용해 기존 blend mode 합성.
Shader "Hidden/UModelerX_TexturePainter_StrokeCompose"
{
    Properties { }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Cull Off ZWrite Off ZTest Always
            Blend One Zero
            Colormask RGBA

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos    : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;            // 원본 layer Albedo (premultiplied)
            float4    _MainTex_TexelSize;  // x=1/w, y=1/h, z=w, w=h
            sampler2D _SeedA;              // .rg = 시드 픽셀 좌표 (Outside 거리). sentinel=60000.
            sampler2D _SeedB;              // .rg = 시드 픽셀 좌표 (Inside  거리). sentinel=60000.

            float  _Size;                  // 사용자 입력 Size (호환용)
            float  _SizeOutside;           // Outside band 거리 임계 (픽셀)
            float  _SizeInside;            // Inside  band 거리 임계 (픽셀)
            int    _HasOutside;            // 1: SeedA 유효
            int    _HasInside;             // 1: SeedB 유효
            int    _Position;              // 0=Outside, 1=Inside, 2=Center
            int    _BlendMode;
            float  _Opacity;
            int    _FillType;              // 0=Color, 1=Gradient
            float4 _StrokeColor;

            static const int MAX_STOPS = 8;
            int    ColorCount;
            float  ColorTime[MAX_STOPS];
            float4 Color[MAX_STOPS];
            int    AlphaCount;
            float  AlphaTime[MAX_STOPS];
            float  Alphas[MAX_STOPS];
            int    GradientBlendMode;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            //
            // ── Gradient 평가 ──
            //
            float3 rgb2oklab(float3 c) {
                float3 lms = mul(float3x3(
                    0.4122214708, 0.5363325363, 0.0514459929,
                    0.2119034982, 0.6806995451, 0.1073969566,
                    0.0883024619, 0.2817188376, 0.6299787005), c);
                lms = pow(lms, 1.0 / 3.0);
                return mul(float3x3(
                    0.2104542553, 0.7936177850, -0.0040720468,
                    1.9779984951, -2.4285922050, 0.4505937099,
                    0.0259040371, 0.7827717662, -0.8086757660), lms);
            }
            float3 oklab2rgb(float3 c) {
                float3 lms = mul(float3x3(
                    1.0, 0.3963377774, 0.2158037573,
                    1.0, -0.1055613458, -0.0638541728,
                    1.0, -0.0894841775, -1.2914855480), c);
                lms = lms * lms * lms;
                return mul(float3x3(
                    4.0767416621, -3.3077115913, 0.2309699292,
                    -1.2684380046, 2.6097574011, -0.3413193965,
                    -0.0041960863, -0.7034186147, 1.7076147010), lms);
            }
            float3 LerpColor_(float3 c0, float3 c1, float w)
            {
                if (GradientBlendMode == 1) return w < 0.5 ? c0 : c1;
                if (GradientBlendMode == 2)
                {
                    float3 lab0 = rgb2oklab(c0);
                    float3 lab1 = rgb2oklab(c1);
                    return oklab2rgb(lerp(lab0, lab1, w));
                }
                return lerp(c0, c1, w);
            }
            float4 EvaluateGradientColor(float t)
            {
                if (ColorCount == 0) return float4(1, 1, 1, 1);
                if (t <= ColorTime[0]) return Color[0];
                if (t >= ColorTime[ColorCount - 1]) return Color[ColorCount - 1];
                for (int i = 1; i < ColorCount; ++i)
                {
                    float prevT = ColorTime[i - 1];
                    float nextT = ColorTime[i];
                    if (t >= prevT && t <= nextT)
                    {
                        float w = saturate((t - prevT) / (nextT - prevT + 1e-6));
                        float3 c = LerpColor_(Color[i - 1].rgb, Color[i].rgb, w);
                        return float4(c, 1);
                    }
                }
                return Color[ColorCount - 1];
            }
            float EvaluateGradientAlpha(float t)
            {
                if (AlphaCount == 0) return 1.0;
                if (t <= AlphaTime[0]) return Alphas[0];
                if (t >= AlphaTime[AlphaCount - 1]) return Alphas[AlphaCount - 1];
                for (int i = 1; i < AlphaCount; ++i)
                {
                    float prevT = AlphaTime[i - 1];
                    float nextT = AlphaTime[i];
                    if (t >= prevT && t <= nextT)
                    {
                        float w = saturate((t - prevT) / (nextT - prevT + 1e-6));
                        if (GradientBlendMode == 1)
                            return (w < 0.5 ? Alphas[i - 1] : Alphas[i]);
                        return lerp(Alphas[i - 1], Alphas[i], w);
                    }
                }
                return Alphas[AlphaCount - 1];
            }

            //
            // ── BlendMode 함수 (LayerComposite 와 동일) ──
            //
            float3 UnpremulStraightSafe(float3 p, float a)
            {
                float aa = max(a, 1e-5);
                float3 pc = float3(min(p.r, aa), min(p.g, aa), min(p.b, aa));
                return saturate(pc / aa);
            }
            float3 Darken(float3 b, float3 s) { return min(s, b); }
            float3 MultiplyBlend(float3 b, float3 s) { return b * s; }
            float3 ColorBurn(float3 b, float3 s)
            {
                float3 r = 1 - (1 - b) / (s + 1e-6);
                r = saturate(r);
                r = lerp(float3(0,0,0), r, step(0, s));
                return r;
            }
            float3 LinearBurn(float3 b, float3 s) { return saturate(b + s - 1); }
            float3 LightenFromPremul(float3 baseP, float baseA, float3 blendP, float blendA)
            {
                float3 m = max(baseP, blendP);
                float a = max(max(baseA, blendA), 1e-5);
                return m / a;
            }
            float3 Screen(float3 b, float3 s) { return 1 - (1 - b) * (1 - s); }
            float3 ColorDodge(float3 b, float3 s) { return b / (1 - s + 1e-6); }
            float3 LinearDodge(float3 b, float3 s) { return min(b + s, 1); }
            float3 Overlay(float3 b, float3 s)
            {
                float3 r;
                r.r = b.r <= 0.5 ? 2 * b.r * s.r : 1 - 2 * (1 - b.r) * (1 - s.r);
                r.g = b.g <= 0.5 ? 2 * b.g * s.g : 1 - 2 * (1 - b.g) * (1 - s.g);
                r.b = b.b <= 0.5 ? 2 * b.b * s.b : 1 - 2 * (1 - b.b) * (1 - s.b);
                return r;
            }
            float3 SoftLight(float3 b, float3 s)
            {
                float3 r;
                if (s.r <= 0.5) r.r = b.r - (1 - 2 * s.r) * b.r * (1 - b.r);
                else            r.r = b.r + (2 * s.r - 1) * (sqrt(b.r) - b.r);
                if (s.g <= 0.5) r.g = b.g - (1 - 2 * s.g) * b.g * (1 - b.g);
                else            r.g = b.g + (2 * s.g - 1) * (sqrt(b.g) - b.g);
                if (s.b <= 0.5) r.b = b.b - (1 - 2 * s.b) * b.b * (1 - b.b);
                else            r.b = b.b + (2 * s.b - 1) * (sqrt(b.b) - b.b);
                return r;
            }
            float3 HardLight(float3 b, float3 s)
            {
                float3 r;
                if (s.r <= 0.5) r.r = 2 * b.r * s.r; else r.r = 1 - 2 * (1 - b.r) * (1 - s.r);
                if (s.g <= 0.5) r.g = 2 * b.g * s.g; else r.g = 1 - 2 * (1 - b.g) * (1 - s.g);
                if (s.b <= 0.5) r.b = 2 * b.b * s.b; else r.b = 1 - 2 * (1 - b.b) * (1 - s.b);
                return r;
            }
            float3 VividLight(float3 b, float3 s)
            {
                const float e = 1e-6;
                float3 r;
                if (s.r <= 0.5) r.r = 1 - (1 - b.r) / (2 * s.r + e); else r.r = b.r / (2 * (1 - s.r) + e);
                if (s.g <= 0.5) r.g = 1 - (1 - b.g) / (2 * s.g + e); else r.g = b.g / (2 * (1 - s.g) + e);
                if (s.b <= 0.5) r.b = 1 - (1 - b.b) / (2 * s.b + e); else r.b = b.b / (2 * (1 - s.b) + e);
                return r;
            }
            float3 LinearLight(float3 b, float3 s) { return saturate(b + 2 * s - 1); }
            float3 PinLight(float3 b, float3 s)
            {
                float3 r;
                if (s.r <= 0.5) r.r = min(b.r, 2 * s.r); else r.r = max(b.r, 2 * s.r - 1);
                if (s.g <= 0.5) r.g = min(b.g, 2 * s.g); else r.g = max(b.g, 2 * s.g - 1);
                if (s.b <= 0.5) r.b = min(b.b, 2 * s.b); else r.b = max(b.b, 2 * s.b - 1);
                return r;
            }
            float3 Difference(float3 b, float3 s) { return abs(b - s); }
            float3 Exclusion(float3 b, float3 s)  { return b + s - 2 * b * s; }

            float3 ApplyBlendMode(int mode, float3 base_rgb, float3 blend_rgb, float3 baseP, float baseA, float3 blendP, float blendA)
            {
                if (mode == 10) return Darken(base_rgb, blend_rgb);
                if (mode == 11) return MultiplyBlend(base_rgb, blend_rgb);
                if (mode == 12) return ColorBurn(base_rgb, blend_rgb);
                if (mode == 13) return LinearBurn(base_rgb, blend_rgb);
                if (mode == 20) return LightenFromPremul(baseP, baseA, blendP, blendA);
                if (mode == 21) return Screen(base_rgb, blend_rgb);
                if (mode == 22) return ColorDodge(base_rgb, blend_rgb);
                if (mode == 23) return LinearDodge(base_rgb, blend_rgb);
                if (mode == 30) return Overlay(base_rgb, blend_rgb);
                if (mode == 31) return SoftLight(base_rgb, blend_rgb);
                if (mode == 32) return HardLight(base_rgb, blend_rgb);
                if (mode == 33) return VividLight(base_rgb, blend_rgb);
                if (mode == 34) return LinearLight(base_rgb, blend_rgb);
                if (mode == 35) return PinLight(base_rgb, blend_rgb);
                if (mode == 40) return Difference(base_rgb, blend_rgb);
                if (mode == 41) return Exclusion(base_rgb, blend_rgb);
                return blend_rgb;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 src = tex2D(_MainTex, i.uv);   // premultiplied
                float  srcA = src.a;

                // 현재 픽셀의 픽셀 좌표 (시드 좌표와 같은 단위).
                float2 pixelPos = i.uv * _MainTex_TexelSize.zw;

                // JFA 거리 — 시드 무효 마커(seed.x > 50000) 면 거리 무한대.
                float distA = 1e30;
                if (_HasOutside == 1)
                {
                    float2 seedA = tex2D(_SeedA, i.uv).rg;
                    if (seedA.x <= 50000.0)
                    {
                        float2 dA = seedA - pixelPos;
                        distA = sqrt(dot(dA, dA));
                    }
                }
                float distB = 1e30;
                if (_HasInside == 1)
                {
                    float2 seedB = tex2D(_SeedB, i.uv).rg;
                    if (seedB.x <= 50000.0)
                    {
                        float2 dB = seedB - pixelPos;
                        distB = sqrt(dot(dB, dB));
                    }
                }

                // Stroke band — 거리 임계로 binarize (1px 페더는 (1 - saturate(d - size)) 형태로 가능하지만
                // 기존 동작과 일치시키기 위해 sharp step 사용).
                float stroke_w_out = (_HasOutside == 1 && distA <= _SizeOutside) ? 1.0 : 0.0;
                float stroke_w_in  = (_HasInside  == 1 && srcA > 0.0 && distB <= _SizeInside) ? 1.0 : 0.0;

                float stroke_w;
                float t; // gradient progression (0 = 안쪽, 1 = 바깥)
                if (_Position == 0)      { stroke_w = stroke_w_out; t = saturate(distA / max(_SizeOutside, 1e-5)); }
                else if (_Position == 1) { stroke_w = stroke_w_in;  t = saturate(distB / max(_SizeInside,  1e-5)); }
                else
                {
                    stroke_w = saturate(stroke_w_out + stroke_w_in);
                    float tOut = saturate(distA / max(_SizeOutside, 1e-5));
                    float tIn  = saturate(distB / max(_SizeInside,  1e-5));
                    t = (stroke_w_out >= stroke_w_in) ? tOut : tIn;
                }

                if (stroke_w <= 0.0) return src;

                // Stroke 색 결정
                float3 strokeRGB;
                float  strokeA;
                if (_FillType == 1)
                {
                    float4 g = EvaluateGradientColor(t);
                    strokeRGB = g.rgb;
                    strokeA   = saturate(EvaluateGradientAlpha(t));
                }
                else
                {
                    strokeRGB = _StrokeColor.rgb;
                    strokeA   = saturate(_StrokeColor.a);
                }

                float strokeAeff = saturate(stroke_w * strokeA * saturate(_Opacity));
                if (strokeAeff <= 0.0) return src;

                //
                // ── Normal blend (Photoshop 스타일) ──
                //   Outside: stroke 를 src 아래에 source-over.
                //            (1 - src.α) 가 blend ratio 가 되어 코어는 자동 보존, 페어더는 자연 blend.
                //            외곽 경계는 stroke_w_out (binarize 된 dilA) 의 sharp drop.
                //   Inside : 안쪽 boundary band 에서 src 색을 stroke 색으로 lerp, alpha 는 src.α 로 clipping.
                //   Center : outside-band 와 inside-band 각각 처리.
                //
                if (_BlendMode == 0)
                {
                    if (_Position == 0)
                    {
                        // Outside: source-over with stroke BELOW src.
                        //   stroke_w_out 은 binarize 된 dilA → stroke 는 dilation reach 안에서 항상 풀 강도,
                        //   reach 경계에서 sharp 하게 0. (1-srcA) 가 내부 blend 처리.
                        //   - α=1 코어: (1-α)=0 → stroke 기여 0 → 원본 보존
                        //   - α=0.5 페어더: 50:50 blend
                        //   - α=0 (reach 내): 100% stroke
                        //   - reach 밖: 0 (transparent)
                        float strokeAeff_o = stroke_w_out * strokeA * saturate(_Opacity);
                        float fill = strokeAeff_o * (1.0 - srcA);
                        float new_a   = saturate(srcA + fill);
                        float3 new_rgb = src.rgb + strokeRGB * fill;
                        return float4(new_rgb, new_a);
                    }
                    if (_Position == 1)
                    {
                        // Inside: stroke 가 src 색을 lerp, alpha 는 src.α 로 clipping.
                        // stroke_w_in 가 sharp 한 inside band 마스크.
                        float3 src_straight = srcA > 1e-5 ? src.rgb / srcA : float3(0, 0, 0);
                        float3 new_straight = lerp(src_straight, strokeRGB, strokeAeff);
                        return float4(new_straight * srcA, srcA);
                    }

                    // Center: outside-band 와 inside-band 각각 처리. stroke_w_* 는 이미 binarize 됨.
                    float strokeAeff_co = saturate(stroke_w_out * strokeA * saturate(_Opacity));
                    float strokeAeff_ci = saturate(stroke_w_in  * strokeA * saturate(_Opacity));
                    if (stroke_w_out >= stroke_w_in)
                    {
                        float fill = strokeAeff_co * (1.0 - srcA);
                        float new_a = saturate(srcA + fill);
                        float3 new_rgb = src.rgb + strokeRGB * fill;
                        return float4(new_rgb, new_a);
                    }
                    else
                    {
                        float3 src_straight = srcA > 1e-5 ? src.rgb / srcA : float3(0, 0, 0);
                        float3 new_straight = lerp(src_straight, strokeRGB, strokeAeff_ci);
                        return float4(new_straight * srcA, srcA);
                    }
                }

                //
                // ── Non-Normal blend: stroke_w 를 stroke 강도로 사용해 기존 blend mode 합성 ──
                //
                float effAlpha = strokeAeff;
                float3 blendP    = strokeRGB * effAlpha;
                float  baseA     = srcA;
                float3 base_rgb  = UnpremulStraightSafe(src.rgb, max(baseA, 1e-5));
                float3 blend_rgb = strokeRGB;

                if (baseA <= 0.0)
                {
                    float outA  = effAlpha + baseA * (1.0 - effAlpha);
                    float3 outP = blendP + src.rgb * (1.0 - effAlpha);
                    return float4(outP, outA);
                }

                float3 dest_rgb = ApplyBlendMode(_BlendMode, base_rgb, blend_rgb, src.rgb, baseA, blendP, effAlpha);
                float3 Cm = saturate(blend_rgb * (1.0 - baseA) + dest_rgb * baseA);
                float  finalA   = effAlpha + baseA * (1.0 - effAlpha);
                float3 finalRGB = Cm * effAlpha + src.rgb * (1.0 - effAlpha);
                return float4(finalRGB, finalA);
            }
            ENDCG
        }
    }
}
