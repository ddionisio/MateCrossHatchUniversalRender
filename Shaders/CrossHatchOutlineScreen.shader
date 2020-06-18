﻿Shader "Hidden/M8/Universal Render Pipeline/CrossHatch/Outline Screen"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "CrossHatchDepthNormal.hlsl"

            #pragma shader_feature_local USE_DEPTH
            #pragma shader_feature_local USE_NORMALS

            uniform half _Thickness;
            uniform half4 _EdgeColor;
            uniform half _DepthThresholdMin, _DepthThresholdMax;
            uniform half _NormalThresholdMin, _NormalThresholdMax;

            float4 _CameraColorTexture_TexelSize;

            TEXTURE2D(_CameraColorTexture); SAMPLER(sampler_CameraColorTexture);            

            TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);

            TEXTURE2D(_CameraDepthNormalsTexture); SAMPLER(sampler_CameraDepthNormalsTexture);

            float4 Outline(float2 uv)
            {
                float4 original = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uv);

                float offset_positive = +ceil(_Thickness * 0.5);
                float offset_negative = -floor(_Thickness * 0.5);
                float2 texel_size = 1.0 / float2(_CameraColorTexture_TexelSize.z, _CameraColorTexture_TexelSize.w);

                float left = texel_size.x * offset_negative;
                float right = texel_size.x * offset_positive;
                float top = texel_size.y * offset_negative;
                float bottom = texel_size.y * offset_positive;

                float2 uv0 = uv + float2(left, top);
                float2 uv1 = uv + float2(right, bottom);
                float2 uv2 = uv + float2(right, top);
                float2 uv3 = uv + float2(left, bottom);

#ifdef USE_DEPTH
                float d0 = SampleDepth(TEXTURE2D_ARGS(_CameraDepthTexture, sampler_CameraDepthTexture), uv0).r;
                float d1 = SampleDepth(TEXTURE2D_ARGS(_CameraDepthTexture, sampler_CameraDepthTexture), uv1).r;
                float d2 = SampleDepth(TEXTURE2D_ARGS(_CameraDepthTexture, sampler_CameraDepthTexture), uv2).r;
                float d3 = SampleDepth(TEXTURE2D_ARGS(_CameraDepthTexture, sampler_CameraDepthTexture), uv3).r;

                float d = length(float2(d1 - d0, d3 - d2));
                d = smoothstep(_DepthThresholdMin, _DepthThresholdMax, d);
#else
                float d = 0;
#endif

#ifdef USE_NORMALS
                float3 n0 = SampleNormal(TEXTURE2D_ARGS(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture), uv0);
                float3 n1 = SampleNormal(TEXTURE2D_ARGS(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture), uv1);
                float3 n2 = SampleNormal(TEXTURE2D_ARGS(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture), uv2);
                float3 n3 = SampleNormal(TEXTURE2D_ARGS(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture), uv3);

                float3 nd1 = n1 - n0;
                float3 nd2 = n3 - n2;
                float n = sqrt(dot(nd1, nd1) + dot(nd2, nd2));
                n = smoothstep(_NormalThresholdMin, _NormalThresholdMax, n);
#else
                float n = 0;
#endif

                float edge = max(d, n);

                float4 output;
                output.rgb = lerp(original.rgb, _EdgeColor.rgb, edge * _EdgeColor.a);
                output.a = original.a;

                return output;
            }

            struct Attributes
            {
                float4 position : POSITION;
                float2 uv       : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.position.xyz);
                output.vertex = vertexInput.positionCS;
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 c = Outline(input.uv);
                return c;
            }

            #pragma vertex vert
            #pragma fragment frag

            ENDHLSL
        }
    }
}