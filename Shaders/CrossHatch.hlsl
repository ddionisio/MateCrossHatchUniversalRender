#ifndef M8_CROSSHATCH_INCLUDED
#define M8_CROSSHATCH_INCLUDED

float3 TriPlanarWeights(float3 normal) {
    const float _TriplanarSharpness = 1;

    float3 weights = pow(abs(normal), _TriplanarSharpness);
    weights /= (weights.x + weights.y + weights.z);

    return weights;
}

half4 Tex2DTriPlanar(TEXTURE2D_PARAM(tex, sampler_tex), float3 position, float3 weights, float scale) {
    half4 xTex = SAMPLE_TEXTURE2D(tex, sampler_tex, position.yz * scale);
    half4 yTex = SAMPLE_TEXTURE2D(tex, sampler_tex, position.xz * scale);
    half4 zTex = SAMPLE_TEXTURE2D(tex, sampler_tex, position.xy * scale);

    return xTex * weights.x + yTex * weights.y + zTex * weights.z;
}

half CrossHatchShade(half lum, half4 hatch) {
    //compute weights
    half4 shadingFactor = half4(lum.xxxx);
    const half4 leftRoot = half4(-0.25, 0.0, 0.25, 0.5);
    const half4 rightRoot = half4(0.25, 0.5, 0.75, 1.0);

    half4 weights = 4.0 * max(0, min(rightRoot - shadingFactor, shadingFactor - leftRoot));

    //final shade
    return dot(weights, hatch.abgr) + 4.0 * clamp(lum - 0.75, 0, 0.25);
}

#endif