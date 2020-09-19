Shader "Grab" {
    Properties {
        _MainTex("Base (RGB)", 2D) = "" {}
	}

    SubShader {
        Pass {
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragV
			
			#include "UnityCG.cginc"
        
            uniform sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            
            float4 fragV(v2f_img i) : COLOR {
                # if UNITY_UV_STARTS_AT_TOP
                    i.uv.y = 1-i.uv.y;
                # endif
        
                return tex2D(_MainTex, i.uv);
            }
			ENDCG
        }
    }
	Fallback Off
}