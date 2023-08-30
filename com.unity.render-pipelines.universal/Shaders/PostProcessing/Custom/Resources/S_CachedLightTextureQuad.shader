Shader "2D/S_CachedLightTextureQuad"
{
    // Shader used for drawing quads onto a light render texture used by the 2D renderer. Quads have a texture provided through the _LightTexture parameter,
    // a color multiplier (_Color) and produced pixels are denormalized according to the _MaximumColorChannelValues argument.

    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    ENDHLSL

    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            TEXTURE2D(_LightTexture);
            SAMPLER(sampler_LightTexture);

            uniform float4 _Color;
            uniform float _MaximumColorChannelValues;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 color = SAMPLE_TEXTURE2D(_LightTexture, sampler_LightTexture, i.uv) * _MaximumColorChannelValues;
                clip(color.r + color.g + color.b > 0.0f ? 1.0f : -1.0f);
                return color + _Color;
            }
            ENDHLSL
        }
    }
}
