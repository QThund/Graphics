Shader "Game/S_GlitchDistortionPostProcess"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _NoiseTexture("Noise Texture", 2D) = "white" {}
        _DisplacementLength("Displacement Length", Float) = 1.0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _NoiseTexture;
            float _DisplacementLength;

            float4 frag(v2f i) : SV_Target
            {
                _DisplacementLength /= _ScreenParams.x;
                float noiseValue = (tex2D(_NoiseTexture, float2(frac(i.uv.y + _Time.y * 60.0f), 0.0f)).r - 0.5f) * 2.0f;
                float4 screenColor = tex2D(_MainTex, float2(frac(i.uv.x + noiseValue * _DisplacementLength), i.uv.y));

                return screenColor;
            }
            ENDCG
        }
    }
}
