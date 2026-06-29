float4 Temperature(float4 color, float weight)
{
    float softSelect = step(-0.01f, weight);

    // 0 1,0,0 -> 0.5,0.5,0 -> 0, 1, 0 -> 0, 0.5, 0.5 -> 0,0,1
    float t = (1 - weight) * 2;
    return color * (1 - softSelect) + softSelect * float4(saturate(float3(1.5f - t, 1.0f - abs(t - 1), 1.5f - abs(t - 2))), color.w);
}

float4 UVToPos(float2 uvPos)
{
    float4 worldPos = float4(uvPos, 1, 1);
    worldPos = mul(UNITY_MATRIX_VP, worldPos);
    return float4(worldPos.xy, 0.5, 1);
}

