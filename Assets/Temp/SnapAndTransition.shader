Shader "Unlit/SnapAndTransition"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GridSize ("GridSize", Float) = 1.0
        _TransitionParam("Transition", Vector) = (0.0,0.0,15.0,15.0)
        _CenterPos ("CenterPos", Vector) = (0.0,0.0,0.0,0.0)
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
                //float4 WPos: TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _GridSize;
            float4 _TransitionParam;
            float4 _CenterPos;

            v2f vert (appdata v)
            {
                v2f o;
                float4 WPos = mul(unity_ObjectToWorld, v.vertex);
                float Grid = _GridSize;
                float Grid2 = _GridSize * 2.0f;
                float Grid4 = _GridSize * 4.0f;

                WPos.xz -= frac(unity_ObjectToWorld._m03_m23 / Grid2) * Grid2;
                
                //float lerpDist = 15.0f;
                //float Dist = length(WPos.xz - float2(0.0f, 0.0f));
                float Dist = WPos.x - _TransitionParam.x - _CenterPos.x;
                float TransiFactor = clamp(Dist / _TransitionParam.z, 0.0f, 1.0f);
                //TransiFactor = 0.1f; 

                float2 POffset = frac(WPos.xz / Grid4) - float2(0.5f, 0.5f);
                const float MinTransitionRadius = 0.26;
                if (abs(POffset.x) < MinTransitionRadius)
                    WPos.x += POffset.x * Grid4 * TransiFactor;
                if (abs(POffset.y) < MinTransitionRadius)
                    WPos.z += POffset.y * Grid4 * TransiFactor;

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
                //return i.WPos;
            }
            ENDCG
        }
    }
}
