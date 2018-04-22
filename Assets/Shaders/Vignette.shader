Shader "Effects/Image/Vignette"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Int ("Intensity", Float) = 0.5
		_Size ("Size", Float) = 1
		_Pow ("Power", Float) = 1
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
			float _Int;
			float _Pow;
			float _Size;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
					
				float d = length(i.uv - 0.5);
				d *= _Int;
				d *= _Size;
				d = pow(d, _Pow);

				return col - d;
			}
			ENDCG
		}
	}
}
