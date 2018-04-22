Shader "Unlit/Mask stuffs"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Fade ("Fade", Float) = 0.5
		_Min ("Min", Float) = 0.1
		_Mul ("Multiply distance with", Float) = 1
		_Col ("Color", Color) = (1,1,1,1)
		_Amb ("Ambient", Float) = 0.1
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" }
		LOD 100
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off
		ZWrite Off

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
				float4 vPos : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Fade;
			float _Mul;
			float _Min;
			fixed4 _Col;
			float _Amb;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.vPos = v.vertex;

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				float d = length(i.vPos) * _Mul;
				fixed4 col = saturate(tex2D(_MainTex, float2(0.5, d*_MainTex_ST.x)) + _Amb);
				float fade = frac(_Time.y) * (1.1+_Min);

				d = (d < fade && d > fade - _Min) ? 1 - (fade - d) / _Min : 0;

				return fixed4(_Col.rgb, ((col.r * (d+_Amb))) * saturate(1-((sin(_Time.y*2) + 1) * 0.125)));
			}
			ENDCG
		}
	}
}
