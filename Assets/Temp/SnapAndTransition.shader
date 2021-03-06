﻿Shader "Unlit/SnapAndTransition"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _SkyTex ("ReflectTex", Cube) = "white" {}
        _SunDir("DirectionalightDir", Vector) = (0.0,-1.0,0.0,0.0)
        _SunColorI("DirectionalightColorIntensity", Vector) = (1.0,1.0,1.0,1.0)

        _FoamTex("FoamTex", 2D) = "white" {}
        _FoamUTex("FoamTex", 2D) = "white" {}

        _DetailN("DetailNormal", 2D) = "white" {}

        _LODDisTex("LODDisTexture", 2D) = "white" {}
        _NextLODDisTex("NextLODDisTexture", 2D) = "white" {}

        _LODNTex("LODNTexture", 2D) = "white" {}
        _NextLODNTex("NextLODNTexture", 2D) = "white" {}

        _BaseColor("baseCol",Vector) = (0.02, 0.02, 0.15, 1.0)
        _SSSCol("sssCol",Vector) = (0.8, 0.55, 0.5, 1.0)
        _FoamColU("FoamColU",Vector) = (0.8, 0.55, 0.5, 1.0)
            
        //runtime params
        //scale LODs
        _OceanScale("LODScale", Int) = 1
        //Water Center
        _CenterPos("CenterPos", Vector) = (0.0,0.0,0.0,0.0)
        
        //staic params(only Set when initialize)
        //parameters when theLOD scale is 1
        _GridSize ("GridSize", Float) = 1.0 //size for individual grid
        _TransitionParam("Transition", Vector) = (0.0,0.0,15.0,15.0)//LOD Transition x=... 
        _LODSize ("LODPatchSize", Float) = 1.0 //WholeLODPatch Size = gridsize*gridcount*patchtilecount
        

        //**********shader part*************
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
                float4 DisPdepth : TEXCOORD4;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _DetailN;
            float4 _DetailN_ST;

            uniform samplerCUBE _SkyTex;
            float4 _SunDir;
            float4 _SunColorI;

            sampler2D _FoamTex;
            float4 _FoamTex_ST;

            sampler2D _FoamTexU;
            float4 _FoamTexU_ST;

            sampler2D _LODDisTex;
            float4 _LODDisTex_ST;
            sampler2D _NextLODDisTex;
            float4 _NextLODDisTex_ST;

            sampler2D _LODNTex;
            float4 _LODNTex_ST;
            sampler2D _NextLODNTex;
            float4 _NextLODNTex_ST;

            float4 _BaseColor;
            float4 _SSSCol;
            float4 _FoamColU;

            float4 _CenterPos;

            float _GridSize;
            float4 _TransitionParam;
            float _LODSize;
            //float _AddUVScale;

            int _OceanScale;

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
                
                float Grid = _GridSize * _OceanScale;
                float Grid2 = Grid * 2.0f;
                float Grid4 = Grid * 4.0f;

                //get Point world position(scaled by parent!)
                float4 WPos = mul(unity_ObjectToWorld, v.vertex);
                //snap to 2*unit grid(scaled by parent!)
                WPos.xz -= frac(unity_ObjectToWorld._m03_m23 / Grid2) * Grid2;
                
                //Transition point to snap to near by 4 unit point for transition
                //_TransitionParam need to be scaled !
                _TransitionParam *= _OceanScale;
                float DistX = abs(WPos.x - _CenterPos.x) - abs(_TransitionParam.x);
                float DistZ = abs(WPos.z - _CenterPos.z) - abs(_TransitionParam.y);
                float TransiFactor = clamp(max(DistX, DistZ) / _TransitionParam.z, 0.0f, 1.0f);
                float2 POffset = frac(WPos.xz / Grid4) - float2(0.5f, 0.5f);
                const float MinTransitionRadius = 0.26;
                if (abs(POffset.x) < MinTransitionRadius)
                    WPos.x += POffset.x * Grid4 * TransiFactor;
                if (abs(POffset.y) < MinTransitionRadius)
                    WPos.z += POffset.y * Grid4 * TransiFactor;
                
                //Gen LOD UV used for displaceMap and Normal Map
                _LODSize *= _OceanScale;
                float2 UV = (WPos.xz - _CenterPos.xz) / _LODSize + 0.5f;
                float2 UV_n = (WPos.xz - _CenterPos.xz) / _LODSize * 0.5f + 0.5f;

                //StaticUV for detail tex, current scale and transiton fixed!
                float2 S_UV = WPos.xz * 0.5f ;
                
                //sample displacement tex
                float3 col = tex2Dlod(_LODDisTex, float4(UV,0,0)).rgb;
                float3 col_n = tex2Dlod(_NextLODDisTex, float4(UV_n, 0, 0)).rgb;

                float2 LODUVblend = clamp((abs(UV - 0.5f) / 0.5f - 0.75f)*5.0f, 0, 1);
                float LODBlendFactor = max(LODUVblend.x, LODUVblend.y);
                //LODBlendFactor = 0.0f;
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

                //add Depth
                //o.depth = COMPUTE_DEPTH_01; Same as depth texture
                float _depth;
                COMPUTE_EYEDEPTH(_depth);
                o.DisPdepth = float4(col.r,col.g,col.b, _depth);


                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the normal texture
                float4 _Normal = tex2D(_LODNTex, i.uv.rg);
                float4 _NNormal = tex2D(_NextLODNTex, i.uv.ba);
                float _FoamMask = tex2D(_LODNTex, i.uv.rg).a;
                //float _FoamMaskU = tex2Dlod(_LODNTex, i.uv.rg, 1).a;
                float _Foam = tex2D(_FoamTex, i.StaticUV.xy*0.5f).a;
                float _FoamU = tex2D(_FoamTexU, i.StaticUV.xy * 1.0f).r;

                _Normal = normalize(lerp(_Normal, _NNormal, i.StaticUV.z));
                //_Normal = normalize(lerp(_Normal, _NNormal, 0.0f));

                //try recon binormal and tangent using x->tangent
                float3 _Binormal = normalize(cross(float3(1,0,0), _Normal.rgb));
                float3 _Tangent = normalize(cross(_Normal.rgb, _Binormal));

                //Detail normalmap
                float3 _NormalD01 = normalize(UnpackNormal(tex2D(_DetailN, i.StaticUV.xy + float2(_Time.y, _Time.y) * 0.01f)));
                float3 _NormalD02 = normalize(UnpackNormal(tex2D(_DetailN, i.StaticUV.xy + float2(_Time.y, -_Time.y) * 0.02f + float2(0.5f, 0.5f))));
                //Far Scaled Detail
                float3 _NormalD10 = normalize(UnpackNormal(tex2D(_DetailN, i.StaticUV.xy * 0.05f)));

                float3 _NormalD = normalize(_NormalD01 + _NormalD02);


                float3 _NormalLOD0 = _Tangent * _NormalD.x + _Binormal * _NormalD.y + _Normal * _NormalD.z;
                float3 _NormalLOD1 = _NormalD10 = _Tangent * _NormalD10.x + _Binormal * _NormalD10.y + _Normal * _NormalD10.z;
                float3 _NormalLOD2 = _Normal;
                
                //Temp Detail normal and normal fade
                float3 F_Normal = lerp(_NormalLOD0, _NormalLOD1, clamp((i.DisPdepth.a - 20.0f) / 200.0f, 0, 1));
                F_Normal = lerp(F_Normal, _NormalLOD2, clamp((i.DisPdepth.a - 300.0f) / 500.0f, 0, 1));
                F_Normal = lerp(F_Normal, float3(0,1,0), clamp((i.DisPdepth.a - 1000.0f) / 8000.0f, 0, 1));

                
                //_WorldSpaceCameraPos
                
                float3 reflectDir = normalize(reflect(-i.CameraDir, F_Normal.xyz));

                float4 skyData = texCUBE(_SkyTex, reflectDir);
                //half3 reflectColor = DecodeHDR(skyData, unity_SpecCube0_HDR);
                
                float4 col = _BaseColor;

                //add fake SSS
                float4 SSCol = float4(0.8f, 0.55f, 0.5f, 1.0f);
                float TLight = saturate( dot(normalize(_SunDir), normalize(i.CameraDir)));
                float FakeSSS = saturate( dot(normalize(_SunDir), normalize(_Normal))+0.55f);
                //float SSSintensity = length(i.DisPdepth.rb);
                col += _SSSCol * TLight * FakeSSS;// *(_FoamMask + 1.0f);

                //Add foam
                float4 baseCol = float4(0.02f, 0.02f, 0.15f, 1.0f);
                float4 foamCol = float4(1.0f, 1.0f, 1.0f, 1.0f) * _SunColorI.a;
                float4 foamColU = float4(0.25f, 0.55f, 0.85f, 1.0f);
                col += saturate(_FoamU * (_FoamMask + 0.1f)) * _FoamColU;
                col = lerp(col, foamCol, saturate(_Foam * (_FoamMask)));


                //Add basic reflection
                float SunReflect = pow(saturate(dot(normalize(-_SunDir), reflectDir)), 50);
                float fresnel = _FresnelB + _FresnelMul*pow(1-dot(normalize(i.CameraDir.xyz), F_Normal), _FresnelPow);
                col.rgb += lerp(col.rgb, skyData.rgb, fresnel) * _FresnelCol.a;
                col.rgb += SunReflect * _SunColorI.rgb * _SunColorI.a;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                //return (i.DisPdepth.a-50.0f)/10.0f;
                return col;
                //return _FoamMask;
                //return lerp(col, 1.0f, saturate(_Foam*_FoamMask));
                //return float4(0.5f,0.5f,0.5f,1.0f);
            }
            ENDCG
        }
    }
}
