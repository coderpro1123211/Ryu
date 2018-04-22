Shader "Unlit/Line"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Min("Min", Float) = 0.1
		_Amb ("Ambient", Float) = 0.5
		_Col ("Color", Color) = (0,1,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "RenderQueue"="Transparent" }
		LOD 100
		Blend SrcAlpha OneMinusSrcAlpha

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

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Min;
			fixed4 _Col;
			float _Amb;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			//fixed4 frag (v2f i) : SV_Target
			//{
			//	// sample the texture
			//	fixed4 col = tex2D(_MainTex, float2(0.5, i.uv.x * _MainTex_ST.x));
			//float d = i.uv.x;
			//float fade = frac(_Time.y) * (1.1 + _Min);
			//float ca = col * 0.5 + 0.5;

			//d = (d < fade && d > fade - _Min) ? 1-(fade - d) / _Min : 0;

			//	return fixed4(_Col.rgb*ca,saturate((d * ca + col.r*0.25)+0.5));
			//}

			fixed4 frag(v2f i) : SV_Target
			{
				// sample the texture
				float d = i.uv.x;
				fixed4 col = saturate(tex2D(_MainTex, float2(0.5, d*_MainTex_ST.x)) + _Amb);
				float fade = frac(_Time.y) * (1.1 + _Min);

				d = (d < fade && d > fade - _Min) ? 1 - (fade - d) / _Min : 0;

				return fixed4(_Col.rgb, ((col.r * (d + _Amb))) * saturate(1 - ((sin(_Time.y * 2) + 1) * 0.125)));
			}
			ENDCG
		}
	}
}
