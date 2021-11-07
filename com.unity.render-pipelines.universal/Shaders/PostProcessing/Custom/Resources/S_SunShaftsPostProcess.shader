Shader "Game/S_SunShaftsPostProcess"
{
    // Unifies the blurred texture of the sun shafts texture and the backbuffer

    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _SunShaftsAlphaMultiplier("Sun shafts alpha multiplier", Float) = 1.0
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _BackbufferTex;
            float _SunShaftsAlphaMultiplier;

            float4 frag(v2f i) : SV_Target
            {
                float4 sunShaftsColor = tex2D(_MainTex, i.uv);
                float4 backbufferColor = tex2D(_BackbufferTex, i.uv);
                return saturate(backbufferColor + sunShaftsColor * Luminance(sunShaftsColor.rgb) * _SunShaftsAlphaMultiplier);
            }
            ENDCG
        }
    }
}
