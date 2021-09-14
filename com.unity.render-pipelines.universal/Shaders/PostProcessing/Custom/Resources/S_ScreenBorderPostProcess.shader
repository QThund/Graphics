Shader "Game/S_ScreenBorderPostProcess"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _BorderWidth ("Border Width", Float) = 30.0
        _BorderColor ("Border Color", Color) = (1.0, 0.0, 0.0, 1.0)
        _BorderGradientPower ("Border Gradient Power", Float) = 2.0
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
            float _BorderWidth;
            float4 _BorderColor;
            float _BorderGradientPower;

            float4 frag (v2f i) : SV_Target
            {
                float4 screenColor = tex2D(_MainTex, i.uv);

                float2 gradient;

                if (i.vertex.x <= _BorderWidth)
                {
                    gradient.x = 1.0f - i.vertex.x / _BorderWidth;
                }
                
                if (i.vertex.x >= _ScreenParams.x - _BorderWidth)
                {
                    gradient.x = (i.vertex.x - _ScreenParams.x + _BorderWidth) / _BorderWidth;
                }
                
                if (i.vertex.y <= _BorderWidth)
                {
                    gradient.y = 1.0f - i.vertex.y / _BorderWidth;
                }
                
                if(i.vertex.y >= _ScreenParams.y - _BorderWidth)
                {
                    gradient.y = (i.vertex.y - _ScreenParams.y + _BorderWidth) / _BorderWidth;
                }

                _BorderColor.a *= (gradient.x < 0.0f && gradient.y < 0.0f) ? 0.0f : max(gradient.x, gradient.y);

                float alpha = pow(_BorderColor.a, _BorderGradientPower);
                screenColor.rgb = lerp(screenColor.rgb, _BorderColor.rgb, alpha);
                screenColor.a = max(screenColor.a, alpha);

                return screenColor;
            }
            ENDCG
        }
    }
}
