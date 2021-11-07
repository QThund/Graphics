Shader "Game/S_RadialBlurPostProcess"
{
    // Downloaded from https://halisavakis.com/my-take-on-shaders-radial-blur/ and modified (added the max function)

    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Samples("Samples", Range(4, 32)) = 16
        _EffectAmount("Effect amount", float) = 1
        _CenterX("Center X", float) = 0.5
        _CenterY("Center Y", float) = 0.5
        _Radius("Radius", float) = 0.1
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
            float _Samples;
            float _EffectAmount;
            float _CenterX;
            float _CenterY;
            float _Radius;
            
            float4 frag(v2f i) : SV_Target
            {
                float4 col = float4(0.0f, 0.0f, 0.0f, 0.0f);
                float2 center = float2(_CenterX, _CenterY);
                float2 dist = i.uv - center;

                for (int j = 0; j < _Samples; j++)
                {
                    float scale = 1 - _EffectAmount * (j / _Samples) * (saturate(length(dist) / _Radius));
                    col += tex2D(_MainTex, dist * max(scale, float2(0.0f, 0.0f)) + center);
                }

                col /= _Samples;

                return col;
            }

            ENDCG
        }
    }
}
