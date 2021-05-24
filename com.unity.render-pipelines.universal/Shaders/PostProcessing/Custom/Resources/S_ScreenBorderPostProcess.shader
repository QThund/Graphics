Shader "Game/S_ScreenBorderPostProcess"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BorderWidth ("Border Width", Float) = 30.0
        _BorderColor ("Border Color", Color) = (1.0, 0.0, 0.0, 1.0)
        _BorderGradientPower ("Border Gradient Power", Float) = 2.0
        _BorderTexture("Border Texture", 2D) = "white" {}
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
            sampler2D _BorderTexture;

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

                float4 textureColor = tex2D(_BorderTexture, i.uv);

                _BorderColor.a *= max(textureColor.a - 0.4f, 0.0f);
                //_BorderColor.a *= (gradient.x < 0.0f && gradient.y < 0.0f) ? 0.0f : max(gradient.x, gradient.y);

                //_BorderColor.a = (length((_ScreenParams.xy * 0.5f) - i.vertex.xy) - _ScreenParams.xy * 0.5f + _BorderWidth) / _BorderWidth;

                /*gradient.x = abs((_ScreenParams.x * 0.5f) - i.vertex.x);// - _ScreenParams.x * 0.5f + _BorderWidth;
                gradient.y = abs((_ScreenParams.y * 0.5f) - i.vertex.y);// - _ScreenParams.y * 0.5f + _BorderWidth;
                _BorderColor.a = (gradient.x + gradient.y) * 0.5f / _BorderWidth;
                */
                _BorderColor.a *= clamp((length(i.uv.xy - float2(0.5f, 0.5f)) - 0.5f + (_BorderWidth / _ScreenParams.x)) / (_BorderWidth / _ScreenParams.x), 0.0f, 1.0f);
                
                if (_BorderColor.a == 0.0f)
                {
                    discard;
                }

                screenColor.rgb = lerp(screenColor.rgb, _BorderColor.rgb, pow(_BorderColor.a, _BorderGradientPower));

                return screenColor;
            }
            ENDCG
        }
    }
}
