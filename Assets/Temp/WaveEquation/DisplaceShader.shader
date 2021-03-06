﻿Shader "Unlit/DisplaceShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _FresnelB("FresnelBase", Float) = 0.01
        _FresnelMul("FresnelMul", Float) = 0.5
        _FresnelPow("FresnelPow", Float) = 2.0
        _FresnelCol("FresnelBase", Vector) = (1.0,1.0,1.0,.5)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;

                float4 WPos = mul(unity_ObjectToWorld, v.vertex);
                float3 col = tex2Dlod(_MainTex, float4(v.uv,0,0)).rgb;
                WPos += float4(0.0f, col.r, 0.0f, 0.0f);
                float4 LPos = mul(unity_WorldToObject, WPos);

                o.vertex = UnityObjectToClipPos(LPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
