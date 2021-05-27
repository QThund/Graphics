Shader "Game/S_ScreenScalingPostProcess"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Scale("Screen Scale", Vector) = (1.0, 1.0, 1.0, 1.0)
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
            float2 _Scale;

            float4 frag(v2f i) : SV_Target
            {
                float2 screenUV = (i.uv - 0.5f + _Scale * 0.5f) / _Scale;
                float4 screenColor = tex2D(_MainTex, screenUV);

                screenColor = screenUV.x < 0.0f || screenUV.y < 0.0f || screenUV.x > 1.0f || screenUV.y > 1.0f ? 0.0f : screenColor;

                return screenColor;
            }
            ENDCG
        }
    }
}
