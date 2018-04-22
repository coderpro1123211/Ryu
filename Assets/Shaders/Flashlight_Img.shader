Shader "VFX/Flashlight_Img"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Tint ("Tint", Color) = (1,1,1,1)
		_Band ("Band", 2D) = "white"
		_Rep ("Repeat", Float) = 1
		_Offset("Offset", Float) = 0.5
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
			sampler2D _Mask;
			sampler2D _Band;
			float _Trans;
			float _Rep;
			float _Offset;
			fixed4 _Tint;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed4 msk = tex2D(_Mask, i.uv);
				fixed4 msk2 = tex2D(_Band, i.uv * _Rep);
				return lerp(col * msk, col * _Tint * saturate(msk2.r + _Offset), 1-_Trans);
			}
			ENDCG
		}
	}
}
