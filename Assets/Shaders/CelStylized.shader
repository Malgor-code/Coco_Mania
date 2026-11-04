
Shader "Custom/CelStylized"
{
    Properties
    {
        [Header(Base)]
        [MainTexture] _BaseMap ("Albedo (sRGB)", 2D) = "white" {}
        [MainColor]   _BaseColor ("Color", Color) = (1,1,1,1)

        [Header(Mask Textures)]
        [KeywordEnum(Off, ARM, Separate)] _Masks ("Modo de mascaras", Float) = 0
        _ARMMap       ("ARM (R=AO G=Rough B=Metal) LINEAR", 2D) = "white" {}
        _AOMap        ("AO (R) LINEAR", 2D) = "white" {}
        _RoughnessMap ("Roughness (R) LINEAR", 2D) = "white" {}
        _MetallicMap  ("Metallic (R) LINEAR", 2D) = "white" {}
        _AOStrength   ("AO fuerza", Range(0,1)) = 1
        _Roughness    ("Roughness (multiplicador)", Range(0,1)) = 0.5
        _Metallic     ("Metallic (multiplicador)", Range(0,1)) = 0

        [Header(Normal Map)]
        [KeywordEnum(Off, RGB, RG)] _Normal ("Modo de normal", Float) = 0
        [NoScaleOffset] _BumpMap ("Normal Map (LINEAR)", 2D) = "bump" {}
        _NormalStrength ("Fuerza normal", Range(0,2)) = 1

        [Header(Cel Shading)]
        _ShadowColor    ("Tinte de sombra (multiplica albedo)", Color) = (0.62,0.5,0.85,1)
        _MidColor       ("Tinte de tono medio", Color) = (0.88,0.82,0.95,1)
        _ShadowSaturation ("Saturacion extra en sombra", Range(0,2)) = 0.6
        _ShadeThreshold ("Borde sombra", Range(0,1)) = 0.42
        _MidThreshold   ("Borde tono medio (= sombra para 2 tonos)", Range(0,1)) = 0.68
        _ShadeSoftness  ("Suavidad de borde (0 = duro, solo AA)", Range(0,0.1)) = 0.0
        _AmbientInfluence ("Luz ambiente plana", Range(0,1)) = 0.15
        [Toggle(_RAMP)] _UseRamp ("Usar rampa de color", Float) = 0
        [NoScaleOffset] _RampMap ("Rampa (clamp)", 2D) = "white" {}

        [Header(Specular)]
        [Toggle(_SPECULAR)] _UseSpec ("Especular estilizado", Float) = 0
        [HDR] _SpecColor ("Color especular", Color) = (1,0.96,0.85,1)
        _SpecSize     ("Tamano", Range(0.01,0.5)) = 0.1
        _SpecSoftness ("Suavidad (0 = duro)", Range(0,0.1)) = 0.0

        [Header(Rim Light)]
        [Toggle(_RIM)] _UseRim ("Rim light", Float) = 0
        [HDR] _RimColor ("Color rim", Color) = (1,0.8,0.9,1)
        _RimThreshold ("Umbral (mayor = mas fino)", Range(0,1)) = 0.72
        _RimSoftness  ("Suavidad (0 = duro)", Range(0,0.2)) = 0.0
        _RimLightInfluence ("Solo del lado de la luz", Range(0,1)) = 0.6

        [Header(Emission and Bloom)]
        [Toggle(_EMISSION)] _UseEmission ("Emision", Float) = 0
        [NoScaleOffset] _EmissionMap ("Emision (sRGB)", 2D) = "white" {}
        [HDR] _EmissionColor ("Color emision (HDR)", Color) = (0,0,0,1)
        _BloomBoost ("Multiplicador HDR (spec/rim/emision)", Range(0,8)) = 1.0

        [Header(Outline)]
        _OutlineColor ("Color outline", Color) = (0.12,0.07,0.05,1)
        _OutlineWidth ("Grosor (pixeles)", Range(0,8)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);   
        TEXTURE2D(_ARMMap);
        TEXTURE2D(_AOMap);
        TEXTURE2D(_RoughnessMap);
        TEXTURE2D(_MetallicMap);
        TEXTURE2D(_BumpMap);
        TEXTURE2D(_EmissionMap);
        TEXTURE2D(_RampMap);      SAMPLER(sampler_RampMap);   

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half   _AOStrength, _Roughness, _Metallic;
            half   _NormalStrength;
            half4  _ShadowColor, _MidColor;
            half   _ShadowSaturation, _MidThreshold;
            half   _ShadeThreshold, _ShadeSoftness, _AmbientInfluence;
            half4  _SpecColor;
            half   _SpecSize, _SpecSoftness;
            half4  _RimColor;
            half   _RimThreshold, _RimSoftness, _RimLightInfluence;
            half4  _EmissionColor;
            half   _BloomBoost;
            half4  _OutlineColor;
            half   _OutlineWidth;
        CBUFFER_END
        ENDHLSL

      
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex   vert
            #pragma fragment frag

            // Features del material (se eliminan del build si ningun material las usa)
            #pragma shader_feature_local _MASKS_OFF _MASKS_ARM _MASKS_SEPARATE
            #pragma shader_feature_local _NORMAL_OFF _NORMAL_RGB _NORMAL_RG
            #pragma shader_feature_local_fragment _RAMP
            #pragma shader_feature_local_fragment _SPECULAR
            #pragma shader_feature_local_fragment _RIM
            #pragma shader_feature_local_fragment _EMISSION

            // Features de URP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                #if !defined(_NORMAL_OFF)
                float4 tangentOS  : TANGENT;
                #endif
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3  normalWS   : TEXCOORD2;
                #if !defined(_NORMAL_OFF)
                half4  tangentWS  : TEXCOORD3;  
                #endif
                half   fogFactor  : TEXCOORD4;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);

                #if defined(_NORMAL_OFF)
                    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                #else
                    VertexNormalInputs n = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                    o.normalWS  = n.normalWS;
                    o.tangentWS = half4(n.tangentWS, v.tangentOS.w * GetOddNegativeScale());
                #endif
                return o;
            }

          
            half CelEdge(half x, half t, half soft)
            {
                half w = max(max(soft, (half)fwidth(x)), 1e-4h);
                return smoothstep(t - w, t + w, x);
            }
            half CelBand(half x, half t) { return CelEdge(x, t, _ShadeSoftness); }

            half4 frag(Varyings i) : SV_Target
            {
                
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;

                
                half ao = 1.0h, rough = _Roughness, metal = _Metallic;
                #if defined(_MASKS_ARM)
                    
                    half3 arm = SAMPLE_TEXTURE2D(_ARMMap, sampler_BaseMap, i.uv).rgb;
                    ao    = lerp(1.0h, arm.r, _AOStrength);
                    rough = arm.g * _Roughness;
                    metal = arm.b * _Metallic;
                #elif defined(_MASKS_SEPARATE)
                    ao    = lerp(1.0h, SAMPLE_TEXTURE2D(_AOMap,        sampler_BaseMap, i.uv).r, _AOStrength);
                    rough = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_BaseMap, i.uv).r * _Roughness;
                    metal = SAMPLE_TEXTURE2D(_MetallicMap,  sampler_BaseMap, i.uv).r * _Metallic;
                #endif

                
                half3 N = normalize(i.normalWS);
                #if !defined(_NORMAL_OFF)
                    half4 nt = SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, i.uv);
                    half3 nTS;
                    #if defined(_NORMAL_RG)
                        
                        nTS.xy = (nt.rg * 2.0h - 1.0h) * _NormalStrength;
                        nTS.z  = sqrt(saturate(1.0h - dot(nTS.xy, nTS.xy)));
                    #else
                        nTS    = nt.rgb * 2.0h - 1.0h;
                        nTS.xy *= _NormalStrength;
                        nTS    = normalize(nTS);
                    #endif
                    half3 T = normalize(i.tangentWS.xyz);
                    half3 B = cross(N, T) * i.tangentWS.w;
                    N = normalize(mul(nTS, half3x3(T, B, N)));
                #endif

                half3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);

                
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half ndl = dot(N, mainLight.direction);
                
                half lit = (ndl * 0.5h + 0.5h) * mainLight.shadowAttenuation;

                
                half bShade = CelBand(lit, _ShadeThreshold);
                half bMid   = CelBand(lit, _MidThreshold);

                
                half3 ambient = SampleSH(half3(0, 1, 0)) * _AmbientInfluence;

                #if defined(_RAMP)
                    half3 ramp = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(saturate(lit), 0.5)).rgb;
                    half3 albedoCel = albedo.rgb * ramp;
                #else
                    
                    half  luma      = dot(albedo.rgb, half3(0.299h, 0.587h, 0.114h));
                    half3 cShadow   = lerp(luma.xxx, albedo.rgb, 1.0h + _ShadowSaturation) * _ShadowColor.rgb;
                    half3 cMid      = albedo.rgb * _MidColor.rgb;
                    half3 albedoCel = lerp(cShadow, cMid, bShade);
                    albedoCel       = lerp(albedoCel, albedo.rgb, bMid);
                #endif

                half3 color = albedoCel * (mainLight.color + ambient) * ao;
                half band = bShade;   

                
                #if defined(_ADDITIONAL_LIGHTS)
                {
                    uint count = GetAdditionalLightsCount();
                    for (uint li = 0u; li < count; ++li)
                    {
                        Light l = GetAdditionalLight(li, i.positionWS);
                        half x = (dot(N, l.direction) * 0.5h + 0.5h) * l.distanceAttenuation;
                        color += albedo.rgb * l.color * (CelBand(x, _ShadeThreshold) * ao);
                    }
                }
                #endif

                
                half3 hdr = 0;
                #if defined(_SPECULAR)
                {
                    half3 H   = normalize(mainLight.direction + V);
                    half  ndh = saturate(dot(N, H));
                    half  th  = 1.0h - _SpecSize * (0.1h + rough * 0.9h);   
                    half  sp  = CelEdge(ndh, th, _SpecSoftness) * band;
                    half3 spCol = lerp(_SpecColor.rgb, albedo.rgb * _SpecColor.rgb, metal);
                    hdr += spCol * sp * mainLight.color;
                }
                #endif

               
                #if defined(_RIM)
                {
                    half fres = 1.0h - saturate(dot(N, V));
                    half rim  = CelEdge(fres, _RimThreshold, _RimSoftness);
                    rim *= lerp(1.0h, CelEdge(ndl, -0.1h, 0.0h), _RimLightInfluence);   
                    hdr += _RimColor.rgb * rim;
                }
                #endif

                
                #if defined(_EMISSION)
                    hdr += SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, i.uv).rgb * _EmissionColor.rgb;
                #endif

                
                color += hdr * _BloomBoost;

                color = MixFog(color, i.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionCS : SV_POSITION; half fogFactor : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 nWS   = TransformObjectToWorldNormal(v.normalOS);
                float4 clip  = TransformWorldToHClip(posWS);

                float2 nClip = mul((float3x3)UNITY_MATRIX_VP, nWS).xy;
                nClip = normalize(nClip + 1e-6);
               
                clip.xy += nClip * (_OutlineWidth * 2.0 / _ScreenParams.xy) * clip.w;

                o.positionCS = clip;
                o.fogFactor  = ComputeFogFactor(clip.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(MixFog(_OutlineColor.rgb, i.fogFactor), 1.0h);
            }
            ENDHLSL
        }

        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 nWS   = TransformObjectToWorldNormal(v.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, lightDir));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE * cs.w);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE * cs.w);
                #endif
                o.positionCS = cs;
                return o;
            }
            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

       
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex   vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }
            half frag(Varyings i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }
    }
    FallBack Off
}
