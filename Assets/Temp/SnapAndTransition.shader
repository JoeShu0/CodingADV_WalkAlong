Shader "Unlit/SnapAndTransition"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _SkyTex ("ReflectTex", Cube) = "white" {}
        _SunDir("DirectionalightDir", Vector) = (0.0,-1.0,0.0,0.0)

        _DetailN("DetailNormal", 2D) = "white" {}

        _LODDisTex("LODDisTexture", 2D) = "white" {}
        _NextLODDisTex("NextLODDisTexture", 2D) = "white" {}

        _LODNTex("LODNTexture", 2D) = "white" {}
        _NextLODNTex("NextLODNTexture", 2D) = "white" {}

        _GridSize ("GridSize", Float) = 1.0
        _TransitionParam("Transition", Vector) = (0.0,0.0,15.0,15.0)
        _CenterPos ("CenterPos", Vector) = (0.0,0.0,0.0,0.0)
        _LODSize ("LODPatchSize", Float) = 1.0
        _AddUVScale("UVScale", Float) = 1.0

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
                float4 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 WPos: TEXCOORD1;
                float4 StaticUV : TEXCOORD2;
                float4 CameraDir : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _DetailN;
            float4 _DetailN_ST;

            uniform samplerCUBE _SkyTex;
            float4 _SunDir;

            sampler2D _LODDisTex;
            float4 _LODDisTex_ST;
            sampler2D _NextLODDisTex;
            float4 _NextLODDisTex_ST;

            sampler2D _LODNTex;
            float4 _LODNTex_ST;
            sampler2D _NextLODNTex;
            float4 _NextLODNTex_ST;


            float _GridSize;
            float4 _TransitionParam;
            float4 _CenterPos;
            float _LODSize;
            float _AddUVScale;

            float _FresnelB;
            float _FresnelMul;
            float _FresnelPow;
            float4 _FresnelCol;
    
            //Onlyuse this kind of recon for tangent space Normal since Z could have 2 sulotion
            void UnpackNormalAndTangent(float4 _NormalNT, out float3 _Normal, out float3 _Tangent)
            {
                float ReconZ = sqrt(1.0f - saturate(dot(_NormalNT.xy, _NormalNT.xy)));
                _Normal = normalize(float3(_NormalNT.x, _NormalNT.y, ReconZ));
                float ReconTZ = sqrt(1.0f - saturate(dot(_NormalNT.zw, _NormalNT.zw)));
                _Tangent = normalize(float3(_NormalNT.z, _NormalNT.w, ReconTZ));
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                //Snapping
                float4 WPos = mul(unity_ObjectToWorld, v.vertex);
                
                float Grid = _GridSize;
                float Grid2 = _GridSize * 2.0f;
                float Grid4 = _GridSize * 4.0f;
                WPos.xz -= frac(unity_ObjectToWorld._m03_m23 / Grid2) * Grid2;
                
                //Transition
                float DistX = abs(WPos.x - _CenterPos.x) - abs(_TransitionParam.x);
                float DistZ = abs(WPos.z - _CenterPos.z) - abs(_TransitionParam.y);
                float TransiFactor = clamp(max(DistX, DistZ) / _TransitionParam.z, 0.0f, 1.0f);
                float2 POffset = frac(WPos.xz / Grid4) - float2(0.5f, 0.5f);
                const float MinTransitionRadius = 0.26;
                if (abs(POffset.x) < MinTransitionRadius)
                    WPos.x += POffset.x * Grid4 * TransiFactor;
                if (abs(POffset.y) < MinTransitionRadius)
                    WPos.z += POffset.y * Grid4 * TransiFactor;
                
                //Gen UV
                float2 UV = (WPos.xz - _CenterPos.xz) / _LODSize + 0.5f;
                float2 UV_n = (WPos.xz - _CenterPos.xz) / _LODSize * 0.5f + 0.5f;

                //StaticUV for detail tex, current scale and transiton fixed!
                float2 S_UV = (WPos.xz - _CenterPos.xz) * 0.5f ;
                

                //sample displacement tex
                float3 col = tex2Dlod(_LODDisTex, float4(UV,0,0)).rgb;
                float3 col_n = tex2Dlod(_NextLODDisTex, float4(UV_n, 0, 0)).rgb;

                float2 LODUVblend = clamp((abs(UV - 0.5f) / 0.5f -0.8f)*5.0f, 0, 1);
                float LODBlendFactor = max(LODUVblend.x, LODUVblend.y);
                col = lerp(col, col_n, LODBlendFactor);
                
                //blend area debug
                //col = float3(LODUVblend,0.0f);

                //Displace Vertex
                WPos += float4(col,0.0f);
                //float4 OWPos = mul(unity_ObjectToWorld, v.vertex);
                //o.WPos = fmod(OWPos, 10.0f) / 10.0f;
                
                //back to LocalSpace
                float4 LPos = mul(unity_WorldToObject, WPos);
                //position debug
                //WPos = mul(unity_ObjectToWorld, LPos);

                
                o.vertex = UnityObjectToClipPos(LPos);
                
                o.StaticUV = float4(S_UV, LODBlendFactor, 0.0f);
                //o.uv = TRANSFORM_TEX(UV, _LODDisTex);
                o.uv = float4(UV, UV_n);
                o.CameraDir = float4(_WorldSpaceCameraPos - WPos.xyz, 0.0f);
                o.WPos = WPos;
                //o.WPos = float4(col, 0.0f);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the normal texture
                float3 _Normal = tex2D(_LODNTex, i.uv.rg);
                float3 _NNormal = tex2D(_NextLODNTex, i.uv.ba);

                _Normal = normalize(lerp(_Normal, _NNormal, i.StaticUV.z));

                //try recon binormal and tangent using x->tangent
                float3 _Binormal = normalize(cross(float3(1,0,0), _Normal));
                float3 _Tangent = normalize(cross(_Normal, _Binormal));

                //Detail normalmap
                float3 _NormalD01 = normalize(UnpackNormal(tex2D(_DetailN, i.StaticUV.xy + float2(_Time.y, _Time.y) * 0.02f)));
                float3 _NormalD02 = normalize(UnpackNormal(tex2D(_DetailN, i.StaticUV.xy + float2(_Time.y, -_Time.y) * 0.04f + float2(0.5f, 0.5f))));

                float3 _NormalD = normalize(_NormalD01 + _NormalD02);

                _Normal = _Tangent * _NormalD.x + _Binormal * _NormalD.y + _Normal * _NormalD.z;
                
                //_WorldSpaceCameraPos
                
                float3 reflectDir = normalize(reflect(-i.CameraDir, _Normal.xyz));

                float4 skyData = texCUBE(_SkyTex, reflectDir);
                //half3 reflectColor = DecodeHDR(skyData, unity_SpecCube0_HDR);
                
                float3 lightDir = _SunDir;//(unity_LightPosition[0] - i.WPos).rgb;

                float4 SunReflect = float4(0.0f, 0.0f, 1.0f, 1.0f);
                SunReflect = pow(dot(normalize(-lightDir), reflectDir), 50);

                float fresnel = _FresnelB + _FresnelMul*pow(1-dot(-normalize(i.CameraDir.xyz), _Normal), _FresnelPow);
                //col.rgb += lerp(col.rgb, skyData.rgb, fresnel) * _FresnelCol.a

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                //return float4(reflectDir,1.0f);
                //return float4(_NormalD,0.0f);
                //return i.WPos;
                return fresnel;
                //return float4(0.5f,0.5f,0.5f,1.0f);
            }
            ENDCG
        }
    }
}
