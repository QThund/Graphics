Shader "Hidden/Light2D-Shape-Volumetric"
{
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            ZTest Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local SPRITE_LIGHT __

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float4 volumeColor  : TANGENT;

#ifdef SPRITE_LIGHT
                half2  uv           : TEXCOORD0;
#endif
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                half2   uv          : TEXCOORD0;
                // CUSTOM CODE
                float2  originPos : TEXCOORD2;
                //
                SHADOW_COORDS(TEXCOORD1)
            };

            half4 _LightColor;
            half  _FalloffDistance;
            half4 _FalloffOffset;
            half  _VolumeOpacity;
            half  _InverseHDREmulationScale;

#ifdef SPRITE_LIGHT
            TEXTURE2D(_CookieTex);			// This can either be a sprite texture uv or a falloff texture
            SAMPLER(sampler_CookieTex);
#else
            uniform half  _FalloffIntensity;
            TEXTURE2D(_FalloffLookup);
            SAMPLER(sampler_FalloffLookup);
#endif

            // CUSTOM CODE
            CBUFFER_START(UnityPerMaterial)
                float  _VolumeTextureCount;

                float  _VolumeTexture0Scale;
                float  _VolumeTexture1Scale;
                float  _VolumeTexture2Scale;

                float  _VolumeTexture0TimeScale;
                float  _VolumeTexture1TimeScale;
                float  _VolumeTexture2TimeScale;

                float  _VolumeTexture0Power;
                float  _VolumeTexture1Power;
                float  _VolumeTexture2Power;

                float2  _VolumeTexture0Direction;
                float2  _VolumeTexture1Direction;
                float2  _VolumeTexture2Direction;
            CBUFFER_END

            TEXTURE2D(_VolumeTexture0);
            SAMPLER(sampler_VolumeTexture0);
            TEXTURE2D(_VolumeTexture1);
            SAMPLER(sampler_VolumeTexture1);
            TEXTURE2D(_VolumeTexture2);
            SAMPLER(sampler_VolumeTexture2);
            //

            SHADOW_VARIABLES

            Varyings vert(Attributes attributes)
            {
                Varyings o = (Varyings)0;

                float3 positionOS = attributes.positionOS;
                positionOS.x = positionOS.x + _FalloffDistance * attributes.color.r + (1 - attributes.color.a) * _FalloffOffset.x;
                positionOS.y = positionOS.y + _FalloffDistance * attributes.color.g + (1 - attributes.color.a) * _FalloffOffset.y;


                o.positionCS = TransformObjectToHClip(positionOS);
                o.color = _LightColor * _InverseHDREmulationScale;
                o.color.a = _VolumeOpacity;

                // CUSTOM CODE
                o.originPos = ComputeScreenPos(mul(UNITY_MATRIX_VP, float4(0.0f, 0.0f, 0.0f, 1.0f)) / o.positionCS.w);
                //

#ifdef SPRITE_LIGHT
                o.uv = attributes.uv;
#else
                o.uv = float2(attributes.color.a, _FalloffIntensity);
#endif
                TRANSFER_SHADOWS(o)

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 color = i.color;

                // CUSTOM CODE
                float2 position = (i.positionCS.xy / _ScreenParams.xy - i.originPos.xy) ;
                float volumeTexture0Alpha = _VolumeTextureCount == 0.0f ? 0.0f : SAMPLE_TEXTURE2D(_VolumeTexture0, sampler_VolumeTexture0, position  / _VolumeTexture0Scale + _VolumeTexture0Direction * _Time * _VolumeTexture0TimeScale).a;
                float volumeTexture1Alpha = _VolumeTextureCount  < 1.0f ? 0.0f : SAMPLE_TEXTURE2D(_VolumeTexture1, sampler_VolumeTexture1, position  / _VolumeTexture1Scale + _VolumeTexture1Direction * _Time * _VolumeTexture1TimeScale).a;
                float volumeTexture2Alpha = _VolumeTextureCount  < 2.0f ? 0.0f : SAMPLE_TEXTURE2D(_VolumeTexture2, sampler_VolumeTexture2, position  / _VolumeTexture2Scale + _VolumeTexture2Direction * _Time * _VolumeTexture2TimeScale).a;
                float volumeAlpha = pow(volumeTexture0Alpha, _VolumeTexture0Power) +
                                    pow(volumeTexture1Alpha, _VolumeTexture1Power) +
                                    pow(volumeTexture2Alpha, _VolumeTexture2Power);
                //

#if SPRITE_LIGHT
                color *= SAMPLE_TEXTURE2D(_CookieTex, sampler_CookieTex, i.uv);
#else
                color.a = i.color.a * SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, i.uv).r;
#endif
                // CUSTOM CODE
                color.a *= volumeAlpha;
                //

                APPLY_SHADOWS(i, color, _ShadowVolumeIntensity);

                return color;

            }
            ENDHLSL
        }
    }
}
