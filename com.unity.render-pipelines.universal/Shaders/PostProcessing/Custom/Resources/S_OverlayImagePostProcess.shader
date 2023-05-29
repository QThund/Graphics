Shader "Game/S_OverlayImagePostProcess"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Image("Image", 2D) = "white" {}
        _AlphaPower("Alpha Power", Float) = 1.0
        _AlphaOffset("Alpha Offset", Float) = 0.0
        _InvertAlpha("Invert Alpha", Float) = 0.0
        _ImageColor("Image Color", Color) = (1.0, 0.0, 0.0, 1.0)
        _TextureAlphaMinimumClipThreshold("Texture Alpha Minimum Clip Threshold", Float) = 0.0
        _TextureAlphaMaximumClipThreshold("Texture Alpha Maximum Clip Threshold", Float) = 1.0
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
            sampler2D _Image;
            float _AlphaPower;
            float _AlphaOffset;
            float _InvertAlpha;
            float4 _ImageColor;
            float _TextureAlphaMinimumClipThreshold;
            float _TextureAlphaMaximumClipThreshold;

            float4 frag (v2f i) : SV_Target
            {
                float4 screenColor = tex2D(_MainTex, i.uv);

                float4 imageColor = tex2D(_Image, i.uv);
                imageColor.a = _InvertAlpha ? 1.0f - imageColor.a
                                            : imageColor.a;

                imageColor.a = imageColor.a - _TextureAlphaMinimumClipThreshold < 0.0f ? 0.0f
                                                                                       : imageColor.a;
                imageColor.a = _TextureAlphaMaximumClipThreshold - imageColor.a < 0.0f ? 0.0f
                                                                                       : imageColor.a;
                imageColor.a = pow(imageColor.a, _AlphaPower);

                imageColor.a = max(imageColor.a - _AlphaOffset, 0.0f);

                imageColor *= _ImageColor;

                screenColor.rgb = lerp(screenColor.rgb, imageColor.rgb, imageColor.a);

                return screenColor;
            }
            ENDCG
        }
    }
}
