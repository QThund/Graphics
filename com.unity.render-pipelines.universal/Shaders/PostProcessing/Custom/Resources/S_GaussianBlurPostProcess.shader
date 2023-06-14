Shader "Game/S_GaussianBlurPostProcess"
{
	Properties
	{
		[HideInInspector] _MainTex("Texture", 2D) = "white" {}
		_BlurSize("Blur Size", Range(0, 0.5)) = 0.011
        _Gauss("Gauss", Float) = 0.0
        _Samples("Sample amount", Float) = 10.0
		_StandardDeviation("Standard Deviation (Gauss only)", Range(0.0, 0.3)) = 0.02
	}

	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always

		//Vertical Blur
		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			float _BlurSize;
			float _StandardDeviation;
            float _Gauss;
            float _Samples;

			#define PI 3.14159265359
			#define E 2.71828182846

			v2f vert(appdata v) 
			{
				v2f o;
				o.position = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f i) : SV_TARGET
			{
				// failsafe so we can use turn off the blur by setting the deviation to 0
				if (_Gauss != 0.0f && _StandardDeviation == 0.0f)
				{
					return tex2D(_MainTex, i.uv);
				}

				float sum = _Gauss != 0.0f ? 0.0f : _Samples;

				float4 finalColor = float4(0.0, 0.0, 0.0, 0.0);

				// iterate over blur samples
				for (float index = 0; index < _Samples; index++)
				{
					// get the offset of the sample
					float offset = (index / (_Samples - 1) - 0.5f) * _BlurSize;
					// get uv coordinate of sample
					float2 uv = i.uv + float2(0.0f, offset);

                    if (_Gauss == 0.0f)
                    {
                        // simply add the color if we don't have a gaussian blur (box)
                        finalColor += tex2D(_MainTex, uv);
                    }
                    else
                    {
                        // calculate the result of the gaussian function
                        float stDevSquared = _StandardDeviation * _StandardDeviation;
                        float gauss = (1.0f / sqrt(2.0f * PI * stDevSquared)) * pow(E, -((offset * offset) / (2.0f * stDevSquared)));
                        // add result to sum
                        sum += gauss;
                        // multiply color with influence from gaussian function and add it to sum color
                        finalColor += tex2D(_MainTex, uv) * gauss;
                    }
				}

				// divide the sum of values by the amount of samples
				finalColor = finalColor / sum;
				return finalColor;
			}

			ENDCG
		}

		// Horizontal Blur
		Pass
		{
			CGPROGRAM

			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			float _BlurSize;
			float _StandardDeviation;
            float _Gauss;
            float _Samples;

			#define PI 3.14159265359
			#define E 2.71828182846

			//the vertex shader
			v2f vert(appdata v) 
			{
				v2f o;
				o.position = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag(v2f i) : SV_TARGET
			{

				// failsafe so we can use turn off the blur by setting the deviation to 0
				if (_Gauss != 0.0f && _StandardDeviation == 0.0f)
				{
					return tex2D(_MainTex, i.uv);
				}

				float sum = _Gauss != 0.0f ? 0.0f : _Samples;

				// calculate aspect ratio
				float invAspect = _ScreenParams.y / _ScreenParams.x;
				float4 finalColor = float4(0.0, 0.0, 0.0, 0.0);

				//iterate over blur samples
				for (float index = 0; index < _Samples; index++)
				{
					//get the offset of the sample
					float offset = (index / (_Samples - 1) - 0.5) * _BlurSize * invAspect;
					//get uv coordinate of sample
					float2 uv = i.uv + float2(offset, 0);

				#if !GAUSS
					// simply add the color if we don't have a gaussian blur (box)
					finalColor += tex2D(_MainTex, uv);
				#else
					// calculate the result of the gaussian function
					float stDevSquared = _StandardDeviation * _StandardDeviation;
					float gauss = (1 / sqrt(2 * PI * stDevSquared)) * pow(E, -((offset * offset) / (2 * stDevSquared)));
					// add result to sum
					sum += gauss;
					// multiply color with influence from gaussian function and add it to sum color
					finalColor += tex2D(_MainTex, uv) * gauss;
				#endif

				}

				// divide the sum of values by the amount of samples
				finalColor = finalColor / sum;
				return finalColor;
			}

			ENDCG
		}
	}
}
