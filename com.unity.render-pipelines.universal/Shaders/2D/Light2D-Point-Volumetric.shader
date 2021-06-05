Shader "Hidden/Light2d-Point-Volumetric"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local USE_POINT_LIGHT_COOKIES __
            #pragma multi_compile_local LIGHT_QUALITY_FAST __

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float2 texcoord     : TEXCOORD0;
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                half2   uv              : TEXCOORD0;
                half2	screenUV        : TEXCOORD1;
                half2	lookupUV        : TEXCOORD2;  // This is used for light relative direction
                half2	lookupNoRotUV   : TEXCOORD3;  // This is used for screen relative direction of a light
                // CUSTOM CODE
                float2  originPos : TEXCOORD6;
                //

#if LIGHT_QUALITY_FAST
                half4	lightDirection	: TEXCOORD4;
#else
                half4	positionWS : TEXCOORD4;
#endif
                SHADOW_COORDS(TEXCOORD5)
            };

#if USE_POINT_LIGHT_COOKIES
            TEXTURE2D(_PointLightCookieTex);
            SAMPLER(sampler_PointLightCookieTex);
#endif

            TEXTURE2D(_FalloffLookup);
            SAMPLER(sampler_FalloffLookup);
            half _FalloffIntensity;

            TEXTURE2D(_LightLookup);
            SAMPLER(sampler_LightLookup);
            half4 _LightLookup_TexelSize;

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            half4   _LightColor;
            half    _VolumeOpacity;
            float4   _LightPosition;
            float4x4 _LightInvMatrix;
            float4x4 _LightNoRotInvMatrix;
            half    _LightZDistance;
            half    _OuterAngle;			// 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
            half    _InnerAngleMult;			// 1-0 where 1 is the value at 0 degrees and 1 is the value at 180 degrees
            half    _InnerRadiusMult;			// 1-0 where 1 is the value at the center and 0 is the value at the outer radius
            half    _InverseHDREmulationScale;
            half    _IsFullSpotlight;

            // CUSTOM CODE
            CBUFFER_START(UnityPerMaterial)
                float  _VolumeTextureCount;

                float  _VolumeTexture0Scale;
                float  _VolumeTexture1Scale;
                float  _VolumeTexture2Scale;
                float  _VolumeTexture3Scale;

                float  _VolumeTexture0TimeScale;
                float  _VolumeTexture1TimeScale;
                float  _VolumeTexture2TimeScale;
                float  _VolumeTexture3TimeScale;

                float  _VolumeTexture0Power;
                float  _VolumeTexture1Power;
                float  _VolumeTexture2Power;
                float  _VolumeTexture3Power;

                float2  _VolumeTexture0Direction;
                float2  _VolumeTexture1Direction;
                float2  _VolumeTexture2Direction;
                float2  _VolumeTexture3Direction;

                float  _VolumeTexture0AlphaMultiplier;
                float  _VolumeTexture1AlphaMultiplier;
                float  _VolumeTexture2AlphaMultiplier;
                float  _VolumeTexture3AlphaMultiplier;

                float  _VolumeTexture0AspectRatio;
                float  _VolumeTexture1AspectRatio;
                float  _VolumeTexture2AspectRatio;
                float  _VolumeTexture3AspectRatio;

                float  _VolumeTexture0IsAdditive;
                float  _VolumeTexture1IsAdditive;
                float  _VolumeTexture2IsAdditive;
                float  _VolumeTexture3IsAdditive;
            CBUFFER_END

            TEXTURE2D(_VolumeTexture0);
            SAMPLER(sampler_VolumeTexture0);
            TEXTURE2D(_VolumeTexture1);
            SAMPLER(sampler_VolumeTexture1);
            TEXTURE2D(_VolumeTexture2);
            SAMPLER(sampler_VolumeTexture2);
            TEXTURE2D(_VolumeTexture3);
            SAMPLER(sampler_VolumeTexture3);
            //

            SHADOW_VARIABLES

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.texcoord;

                float4 worldSpacePos;
                worldSpacePos.xyz = TransformObjectToWorld(input.positionOS);
                worldSpacePos.w = 1;

                float4 lightSpacePos = mul(_LightInvMatrix, worldSpacePos);
                float4 lightSpaceNoRotPos = mul(_LightNoRotInvMatrix, worldSpacePos);
                float halfTexelOffset = 0.5 * _LightLookup_TexelSize.x;
                output.lookupUV = 0.5 * (lightSpacePos.xy + 1) + halfTexelOffset;
                output.lookupNoRotUV = 0.5 * (lightSpaceNoRotPos.xy + 1) + halfTexelOffset;

#if LIGHT_QUALITY_FAST
                output.lightDirection.xy = _LightPosition.xy - worldSpacePos.xy;
                output.lightDirection.z = _LightZDistance;
                output.lightDirection.w = 0;
                output.lightDirection.xyz = normalize(output.lightDirection.xyz);
#else
                output.positionWS = worldSpacePos;
#endif

                // CUSTOM CODE
                output.originPos = ComputeScreenPos(mul(UNITY_MATRIX_VP, float4(0.0f, 0.0f, 0.0f, 1.0f)) / output.positionCS.w);
                //

                float4 clipVertex = output.positionCS / output.positionCS.w;
                output.screenUV = ComputeScreenPos(clipVertex).xy;

                TRANSFER_SHADOWS(output)

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 normal = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.screenUV);
                half4 lookupValueNoRot = SAMPLE_TEXTURE2D(_LightLookup, sampler_LightLookup, input.lookupNoRotUV);  // r = distance, g = angle, b = x direction, a = y direction
                half4 lookupValue = SAMPLE_TEXTURE2D(_LightLookup, sampler_LightLookup, input.lookupUV);  // r = distance, g = angle, b = x direction, a = y direction

                // Inner Radius
                half attenuation = saturate(_InnerRadiusMult * lookupValueNoRot.r);   // This is the code to take care of our inner radius

                // Spotlight
                half  spotAttenuation = saturate((_OuterAngle - lookupValue.g + _IsFullSpotlight) * _InnerAngleMult);
                attenuation = attenuation * spotAttenuation;

                half2 mappedUV;
                mappedUV.x = attenuation;
                mappedUV.y = _FalloffIntensity;
                attenuation = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, mappedUV).r;

                // CUSTOM CODE
                float defaultVolumeTexture0Alpha = 0.0f;
                float defaultVolumeTexture1Alpha = _VolumeTexture1IsAdditive ? 0.0f : 1.0f;
                float defaultVolumeTexture2Alpha = _VolumeTexture2IsAdditive ? 0.0f : 1.0f;
                float defaultVolumeTexture3Alpha = _VolumeTexture3IsAdditive ? 0.0f : 1.0f;

                float2 position = (input.positionCS.xy / _ScreenParams.xy - input.originPos.xy);
                float volumeTexture0Alpha = _VolumeTextureCount == 0.0f ? defaultVolumeTexture0Alpha : SAMPLE_TEXTURE2D(_VolumeTexture0, sampler_VolumeTexture0, position / _VolumeTexture0Scale * float2(1.0f, _VolumeTexture0AspectRatio) - _VolumeTexture0Direction * _Time * _VolumeTexture0TimeScale).a;
                float volumeTexture1Alpha = _VolumeTextureCount < 1.5f ? defaultVolumeTexture1Alpha : SAMPLE_TEXTURE2D(_VolumeTexture1, sampler_VolumeTexture1, position / _VolumeTexture1Scale * float2(1.0f, _VolumeTexture1AspectRatio) - _VolumeTexture1Direction * _Time * _VolumeTexture1TimeScale).a;
                float volumeTexture2Alpha = _VolumeTextureCount < 2.5f ? defaultVolumeTexture2Alpha : SAMPLE_TEXTURE2D(_VolumeTexture2, sampler_VolumeTexture2, position / _VolumeTexture2Scale * float2(1.0f, _VolumeTexture2AspectRatio) - _VolumeTexture2Direction * _Time * _VolumeTexture2TimeScale).a;
                float volumeTexture3Alpha = _VolumeTextureCount < 3.5f ? defaultVolumeTexture3Alpha : SAMPLE_TEXTURE2D(_VolumeTexture3, sampler_VolumeTexture3, position / _VolumeTexture3Scale * float2(1.0f, _VolumeTexture3AspectRatio) - _VolumeTexture3Direction * _Time * _VolumeTexture3TimeScale).a;
                volumeTexture0Alpha = pow(volumeTexture0Alpha, _VolumeTexture0Power) * _VolumeTexture0AlphaMultiplier;
                volumeTexture1Alpha = pow(volumeTexture1Alpha, _VolumeTexture1Power) * _VolumeTexture1AlphaMultiplier;
                volumeTexture2Alpha = pow(volumeTexture2Alpha, _VolumeTexture2Power) * _VolumeTexture2AlphaMultiplier;
                volumeTexture3Alpha = pow(volumeTexture3Alpha, _VolumeTexture3Power) * _VolumeTexture3AlphaMultiplier;
                float volumeAlpha = volumeTexture0Alpha;
                volumeAlpha = _VolumeTexture1IsAdditive ? volumeAlpha + volumeTexture1Alpha : volumeAlpha * volumeTexture1Alpha;
                volumeAlpha = _VolumeTexture2IsAdditive ? volumeAlpha + volumeTexture2Alpha : volumeAlpha * volumeTexture2Alpha;
                volumeAlpha = _VolumeTexture3IsAdditive ? volumeAlpha + volumeTexture3Alpha : volumeAlpha * volumeTexture3Alpha;
                //

#if USE_POINT_LIGHT_COOKIES
                half4 cookieColor = SAMPLE_TEXTURE2D(_PointLightCookieTex, sampler_PointLightCookieTex, input.lookupUV);
                half4 lightColor = cookieColor * _LightColor * attenuation;
#else
                half4 lightColor = _LightColor * attenuation;
#endif
                // CUSTOM CODE
                lightColor.a *= _VolumeTextureCount == 0.0f ? 1.0f : volumeAlpha;
                //

                APPLY_SHADOWS(input, lightColor, _ShadowVolumeIntensity);

                return _VolumeOpacity * lightColor * _InverseHDREmulationScale;
            }
            ENDHLSL
        }
    }
}
