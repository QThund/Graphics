Shader "Hidden/Light2D-Shape"
{
    Properties
    {
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend[_SrcBlend][_DstBlend]
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local SPRITE_LIGHT __
            #pragma multi_compile_local USE_NORMAL_MAP __
            #pragma multi_compile_local USE_ADDITIVE_BLENDING __
            // CUSTOM CODE
            #pragma multi_compile_local USE_VOLUME_TEXTURES __
            #pragma multi_compile_local USE_DITHERING __
            //

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;

#ifdef SPRITE_LIGHT
                float2 uv           : TEXCOORD0;
#endif
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                half2   uv          : TEXCOORD0;
                // CUSTOM CODE
#ifdef USE_VOLUME_TEXTURES
                float2  originPos : TEXCOORD4;
#endif
                //
                SHADOW_COORDS(TEXCOORD1)
                NORMALS_LIGHTING_COORDS(TEXCOORD2, TEXCOORD3)
            };

            half    _InverseHDREmulationScale;
            half4   _LightColor;
            half    _FalloffDistance;
            half4   _FalloffOffset;

#ifdef SPRITE_LIGHT
            TEXTURE2D(_CookieTex);			// This can either be a sprite texture uv or a falloff texture
            SAMPLER(sampler_CookieTex);
#else
            half    _FalloffIntensity;
            TEXTURE2D(_FalloffLookup);
            SAMPLER(sampler_FalloffLookup);
#endif
            // CUSTOM CODE
#if defined(USE_VOLUME_TEXTURES) || defined(USE_DITHERING)

            CBUFFER_START(UnityPerMaterial)

    #ifdef USE_VOLUME_TEXTURES

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
    #endif

    #ifdef USE_DITHERING

                float _IsDitheringEnabled;
    #endif
            CBUFFER_END

            TEXTURE2D(_VolumeTexture0);
            SAMPLER(sampler_VolumeTexture0);
            TEXTURE2D(_VolumeTexture1);
            SAMPLER(sampler_VolumeTexture1);
            TEXTURE2D(_VolumeTexture2);
            SAMPLER(sampler_VolumeTexture2);
            TEXTURE2D(_VolumeTexture3);
            SAMPLER(sampler_VolumeTexture3);
#endif

#ifdef USE_DITHERING

            TEXTURE2D(_DitheringTexture);
            SAMPLER(sampler_DitheringTexture);
#endif
            //

            NORMALS_LIGHTING_VARIABLES
            SHADOW_VARIABLES

            Varyings vert(Attributes attributes)
            {
                Varyings o = (Varyings)0;

                float3 positionOS = attributes.positionOS;
                positionOS.x = positionOS.x + _FalloffDistance * attributes.color.r + (1-attributes.color.a) * _FalloffOffset.x;
                positionOS.y = positionOS.y + _FalloffDistance * attributes.color.g + (1-attributes.color.a) * _FalloffOffset.y;

                o.positionCS = TransformObjectToHClip(positionOS);
                o.color = _LightColor * _InverseHDREmulationScale;
                o.color.a = attributes.color.a;

                // CUSTOM CODE
#ifdef USE_VOLUME_TEXTURES

                o.originPos = ComputeScreenPos(mul(UNITY_MATRIX_VP, float4(0.0f, 0.0f, 0.0f, 1.0f)) / o.positionCS.w);
#endif
                //

#ifdef SPRITE_LIGHT
                o.uv = attributes.uv;
#else
                o.uv = float2(o.color.a, _FalloffIntensity);
#endif

                float4 worldSpacePos;
                worldSpacePos.xyz = TransformObjectToWorld(positionOS);
                worldSpacePos.w = 1;
                TRANSFER_NORMALS_LIGHTING(o, worldSpacePos)
                TRANSFER_SHADOWS(o)

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 color = i.color;

#if SPRITE_LIGHT
                half4 cookie = SAMPLE_TEXTURE2D(_CookieTex, sampler_CookieTex, i.uv);
    #if USE_ADDITIVE_BLENDING

                color *= cookie * cookie.a;
    #else
                color *= cookie;
    #endif
#else
    #if USE_ADDITIVE_BLENDING
                color *= SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, i.uv).r;
    #else
                color.a = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, i.uv).r;
    #endif
#endif
                // CUSTOM CODE
#ifdef USE_VOLUME_TEXTURES

                // Volume textures
                float defaultVolumeTexture0Alpha = 0.0f;
                float defaultVolumeTexture1Alpha = _VolumeTexture1IsAdditive ? 0.0f : 1.0f;
                float defaultVolumeTexture2Alpha = _VolumeTexture2IsAdditive ? 0.0f : 1.0f;
                float defaultVolumeTexture3Alpha = _VolumeTexture3IsAdditive ? 0.0f : 1.0f;

                float2 position = (i.positionCS.xy / _ScreenParams.xy - i.originPos.xy);
                float volumeTexture0Alpha = _VolumeTextureCount == 0.0f ? defaultVolumeTexture0Alpha : SAMPLE_TEXTURE2D(_VolumeTexture0, sampler_VolumeTexture0, position / _VolumeTexture0Scale * float2(1.0f, _VolumeTexture0AspectRatio) - _VolumeTexture0Direction * _Time * _VolumeTexture0TimeScale).a;
                float volumeTexture1Alpha = _VolumeTextureCount < 1.5f ? defaultVolumeTexture1Alpha : SAMPLE_TEXTURE2D(_VolumeTexture1, sampler_VolumeTexture1, position / _VolumeTexture1Scale * float2(1.0f, _VolumeTexture1AspectRatio) - _VolumeTexture1Direction * _Time * _VolumeTexture1TimeScale).a;
                float volumeTexture2Alpha = _VolumeTextureCount < 2.5f ? defaultVolumeTexture2Alpha : SAMPLE_TEXTURE2D(_VolumeTexture2, sampler_VolumeTexture2, position / _VolumeTexture2Scale * float2(1.0f, _VolumeTexture2AspectRatio) - _VolumeTexture2Direction * _Time * _VolumeTexture2TimeScale).a;
                float volumeTexture3Alpha = _VolumeTextureCount < 3.5f ? defaultVolumeTexture3Alpha : SAMPLE_TEXTURE2D(_VolumeTexture3, sampler_VolumeTexture3, position / _VolumeTexture3Scale * float2(1.0f, _VolumeTexture3AspectRatio) - _VolumeTexture3Direction * _Time * _VolumeTexture3TimeScale).a;
                volumeTexture0Alpha = _VolumeTextureCount == 0.0f ? defaultVolumeTexture0Alpha : pow(volumeTexture0Alpha, _VolumeTexture0Power) * _VolumeTexture0AlphaMultiplier;
                volumeTexture1Alpha = _VolumeTextureCount < 1.5f ? defaultVolumeTexture1Alpha : pow(volumeTexture1Alpha, _VolumeTexture1Power) * _VolumeTexture1AlphaMultiplier;
                volumeTexture2Alpha = _VolumeTextureCount < 2.5f ? defaultVolumeTexture2Alpha : pow(volumeTexture2Alpha, _VolumeTexture2Power) * _VolumeTexture2AlphaMultiplier;
                volumeTexture3Alpha = _VolumeTextureCount < 3.5f ? defaultVolumeTexture3Alpha : pow(volumeTexture3Alpha, _VolumeTexture3Power) * _VolumeTexture3AlphaMultiplier;
                float volumeAlpha = volumeTexture0Alpha;
                volumeAlpha = _VolumeTexture1IsAdditive ? volumeAlpha + volumeTexture1Alpha : volumeAlpha * volumeTexture1Alpha;
                volumeAlpha = _VolumeTexture2IsAdditive ? volumeAlpha + volumeTexture2Alpha : volumeAlpha * volumeTexture2Alpha;
                volumeAlpha = _VolumeTexture3IsAdditive ? volumeAlpha + volumeTexture3Alpha : volumeAlpha * volumeTexture3Alpha;

                color *= _VolumeTextureCount == 0.0f ? 1.0f : volumeAlpha;
#endif

                // Dithering
#ifdef USE_DITHERING

                if (_IsDitheringEnabled)
                {
                    color += (SAMPLE_TEXTURE2D(_DitheringTexture, sampler_DitheringTexture, i.positionCS.xy / 8.0).r / 32.0 - (1.0 / 128.0));
                }
#endif
                //

                APPLY_NORMALS_LIGHTING(i, color);
                APPLY_SHADOWS(i, color, _ShadowIntensity);

                return color;
            }
            ENDHLSL
        }
    }
}
