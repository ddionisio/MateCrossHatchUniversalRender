#ifndef M8_UNIVERSAL_CROSSHATCH_LIGHTING_INCLUDED
#define M8_UNIVERSAL_CROSSHATCH_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "CrossHatch.hlsl"

half4 CrossHatchUniversalFragmentBlinnPhongShadeOnly(InputData inputData, half3 diffuse, half4 specularGloss, half smoothness, half3 emission, half4 crossHatch, half3 crossHatchColor, half alpha)
{
    Light mainLight = GetMainLight(inputData.shadowCoord);
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, half4(0, 0, 0, 0));

    half3 attenuatedLightColor = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);

    half3 diffuseColor = mainLight.color; //only use ambient light

    half3 shadeColor = inputData.bakedGI + LightingLambert(attenuatedLightColor, mainLight.direction, inputData.normalWS);

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    half3 specularColor = LightingSpecular(attenuatedLightColor, mainLight.direction, inputData.normalWS, inputData.viewDirectionWS, specularGloss, smoothness);
#endif

#ifdef _ADDITIONAL_LIGHTS
    uint pixelLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
    {
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS);
        half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
        shadeColor += LightingLambert(attenuatedLightColor, light.direction, inputData.normalWS);

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
        specularColor += LightingSpecular(attenuatedLightColor, light.direction, inputData.normalWS, inputData.viewDirectionWS, specularGloss, smoothness);
#endif
    }
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    shadeColor += inputData.vertexLighting;
#endif

    shadeColor += emission;

    half3 finalColor = (diffuseColor * diffuse) + emission; //allow emission to influence color

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    shadeColor += specularColor;
#endif

    //Cross-Hatch shade
    half shadeLum = Luminance(shadeColor);
    half shade = CrossHatchShade(shadeLum, crossHatch);

    finalColor = lerp(crossHatchColor, finalColor, shade);

    return half4(finalColor, alpha);
}

half4 CrossHatchUniversalFragmentBlinnPhong(InputData inputData, half3 diffuse, half4 specularGloss, half smoothness, half3 emission, half4 crossHatch, half3 crossHatchColor, half alpha)
{
    Light mainLight = GetMainLight(inputData.shadowCoord);
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, half4(0, 0, 0, 0));

    //half3 attenuatedLightColor = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);
    //half3 diffuseColor = inputData.bakedGI + LightingLambert(attenuatedLightColor, mainLight.direction, inputData.normalWS);
    //half3 specularColor = LightingSpecular(attenuatedLightColor, mainLight.direction, inputData.normalWS, inputData.viewDirectionWS, specularGloss, smoothness);
        
    half3 mainDistanceAttenuatedLightColor = mainLight.color * mainLight.distanceAttenuation;
    half3 mainAttenuatedLightColor = mainDistanceAttenuatedLightColor * mainLight.shadowAttenuation;
    half3 mainDiffuseColor = LightingLambert(mainAttenuatedLightColor, mainLight.direction, inputData.normalWS);

    half3 diffuseColor = inputData.bakedGI; //add main light later

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    half3 specularColor = LightingSpecular(mainAttenuatedLightColor, mainLight.direction, inputData.normalWS, inputData.viewDirectionWS, specularGloss, smoothness);
#endif
    
#ifdef _ADDITIONAL_LIGHTS
    uint pixelLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
    {
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS);
        half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
        diffuseColor += LightingLambert(attenuatedLightColor, light.direction, inputData.normalWS);

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
        specularColor += LightingSpecular(attenuatedLightColor, light.direction, inputData.normalWS, inputData.viewDirectionWS, specularGloss, smoothness);
#endif
    }
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
    diffuseColor += inputData.vertexLighting;
#endif

    half3 shadeColor = diffuseColor + mainDiffuseColor + emission;
    
    half3 finalColor = (diffuseColor + mainDistanceAttenuatedLightColor) * diffuse + emission; //main light as ambiance

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    finalColor += specularColor;
    shadeColor += specularColor; //also append to shade
#endif

    //Cross-Hatch shade
    half shadeLum = Luminance(shadeColor);
    half shade = CrossHatchShade(shadeLum, crossHatch);

    finalColor = lerp(crossHatchColor, finalColor, shade);
    
    return half4(finalColor, alpha);
}

#endif