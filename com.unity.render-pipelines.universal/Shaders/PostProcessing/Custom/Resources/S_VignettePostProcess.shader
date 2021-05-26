Shader "Game/S_VignettePostProcess"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _VignetteRadius ("VignetteRadius", Float) = 30.0
        _VignetteColor ("Vignette Color", Color) = (1.0, 0.0, 0.0, 1.0)
        _VignetteGradientPower ("Vignette Gradient Power", Float) = 2.0
        _VignetteTexture("Vignette Texture", 2D) = "white" {}
        _TextureAlphaClipThreshold("Texture Alpha Clip Threshold", Float) = 0.0
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
            float _VignetteRadius;
            float4 _VignetteColor;
            float _VignetteGradientPower;
            sampler2D _VignetteTexture;
            float _TextureAlphaClipThreshold;

            float4 frag (v2f i) : SV_Target
            {
                float4 screenColor = tex2D(_MainTex, i.uv);

                float4 textureColor = tex2D(_VignetteTexture, i.uv);

                clip(textureColor.a - _TextureAlphaClipThreshold);

                _VignetteColor.a *= max(textureColor.a - 0.4f, 0.0f);
                _VignetteColor.a *= clamp(pow(length(i.uv.xy - float2(0.5f, 0.5f)), _VignetteGradientPower) / (_VignetteRadius / _ScreenParams.x), 0.0f, 1.0f);
                
                if (_VignetteColor.a == 0.0f)
                {
                    discard;
                }

                screenColor.rgb = lerp(screenColor.rgb, _VignetteColor.rgb, _VignetteColor.a);

                return screenColor;
            }
            ENDCG
        }
    }
}
