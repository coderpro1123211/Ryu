Shader "Unlit/Stencil Mask"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Mul ("object size", Float) = 0.2
		_Str ("Strength", Float) = 0.5
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "RenderQueue"="Transparent" "DisableBatching" = "True" }
		Blend SrcAlpha One
		Cull Off
		ZWrite Off
		LOD 100
		Stencil {
			Ref 1
			Comp Always
			Pass Replace
			ZFail Replace
		}

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
				float3 vPos : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Mul;
			float _Str;

			v2f vert (appdata v)
			{
				v2f o;
				o.vPos = v.vertex;
				o.vertex = UnityObjectToClipPos(o.vPos);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float f = 1-length(i.vPos.xyz / _Mul);
				return fixed4(_Str, _Str, _Str, f);//fixed4(1,1,1,1-length(i.vPos / _Mul));
			}
			ENDCG
		}
	}
}
