Shader "Game/S_DisplacementPostProcess"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

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

            TEXTURE2D(_DisplacementTexture);
            SAMPLER(sampler_DisplacementTexture);

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 displacement = SAMPLE_TEXTURE2D(_DisplacementTexture, sampler_DisplacementTexture, i.uv);
                float4 backgroundColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float4 targetColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + displacement.xy * 0.01f);
                displacement.z = saturate(displacement.z);
                targetColor.rgb = targetColor.rgb * displacement.z + backgroundColor.rgb * (1.0f - displacement.z); // The color is blended according to the Z component of the displacement texture
                return targetColor;
            }
            ENDHLSL
        }
    }
}
