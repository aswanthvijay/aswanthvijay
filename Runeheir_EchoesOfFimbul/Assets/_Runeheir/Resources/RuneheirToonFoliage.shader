// RUNEHEIR: Echoes of Fimbul — art direction sample (stylized anime 3D)
// Cel-shaded textured surfaces for the painted world: leaves, grass, bark, rock and props.
//   • the Runeheir/Toon light band and cool shadow tint, on textured (alpha-clipped, two-sided) surfaces
//   • sphere normals: foliage is lit as one soft mass around a center (the anime-tree look) instead of per card
//   • a darker base toward the ground, light through backlit leaves, a saturation control and a painterly mip bias
// Passes: UniversalForwardOnly, ShadowCaster, DepthOnly, DepthNormals (alpha-clipped in each).
Shader "Runeheir/ToonFoliage"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        _MipBias("Painterly Softening (mip bias)", Range(0, 4)) = 0
        _Saturation("Saturation", Range(0, 2)) = 1

        [Header(Cel Shading)]
        _ShadowColor("Shadow Tint", Color) = (0.6, 0.66, 0.86, 1)
        _RampThreshold("Light / Shadow Threshold", Range(0, 1)) = 0.5
        _RampSmoothness("Band Edge Softness", Range(0.001, 0.5)) = 0.06
        _ShadowStrength("Received Shadow Strength", Range(0, 1)) = 0.85
        _AmbientStrength("Ambient Strength", Range(0, 2)) = 0.7

        [Header(Foliage)]
        _SphereNormals("Sphere Normals", Range(0, 1)) = 0
        _SphereCenterWS("Sphere Center (world, set per renderer)", Vector) = (0, 1, 0, 0)
        _BaseShade("Darker Toward the Ground", Range(0, 1)) = 0
        _BaseShadeHeight("Base Shade Height (m)", Float) = 1
        _Translucency("Light Through Leaves", Range(0, 2)) = 0

        [Header(Rim)]
        _RimColor("Rim Color (A = intensity)", Color) = (1, 0.97, 0.88, 0.25)
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.7

        [Header(Emission)]
        [HDR] _EmissionColor("Emission", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            half _MipBias;
            half _Saturation;
            half4 _ShadowColor;
            half _RampThreshold;
            half _RampSmoothness;
            half _ShadowStrength;
            half _AmbientStrength;
            half _SphereNormals;
            float4 _SphereCenterWS;
            half _BaseShade;
            float _BaseShadeHeight;
            half _Translucency;
            half4 _RimColor;
            half _RimThreshold;
            half4 _EmissionColor;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        void ClipAlpha(float2 uv)
        {
        #if defined(_ALPHATEST_ON)
            clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a * _BaseColor.a - _Cutoff);
        #endif
        }
        ENDHLSL

        Pass
        {
            Name "ToonFoliageForward"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex FoliageVertex
            #pragma fragment FoliageFragment
            #pragma shader_feature_local _ALPHATEST_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                float baseHeight : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings FoliageVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                output.baseHeight = positionInputs.positionWS.y - TransformObjectToWorld(float3(0.0, 0.0, 0.0)).y;
                return output;
            }

            half ToonBand(half value)
            {
                return smoothstep(_RampThreshold - _RampSmoothness, _RampThreshold + _RampSmoothness, value);
            }

            half3 ToonAdditionalLight(Light light, float3 normalWS, half3 albedo)
            {
                half ndotl = saturate(dot(normalWS, light.direction) * 0.5h + 0.5h);
                half band = smoothstep(0.35h, 0.35h + _RampSmoothness * 2.0h, ndotl);
                return albedo * light.color * (band * light.distanceAttenuation * light.shadowAttenuation);
            }

            half4 FoliageFragment(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                ClipAlpha(input.uv);
                half4 albedo = SAMPLE_TEXTURE2D_BIAS(_BaseMap, sampler_BaseMap, input.uv, _MipBias) * _BaseColor;
                half luma = dot(albedo.rgb, half3(0.2126h, 0.7152h, 0.0722h));
                albedo.rgb = max(lerp(luma.xxx, albedo.rgb, _Saturation), 0.0h);
                albedo.rgb *= lerp(1.0h - _BaseShade, 1.0h, saturate(input.baseHeight / max(_BaseShadeHeight, 0.01)));

                float3 normalWS = normalize(input.normalWS) * (IS_FRONT_VFACE(face, 1.0, -1.0));
                float3 sphereNormal = SafeNormalize(input.positionWS - _SphereCenterWS.xyz);
                normalWS = SafeNormalize(lerp(normalWS, sphereNormal, _SphereNormals));
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
            #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            #else
                inputData.shadowCoord = float4(0.0, 0.0, 0.0, 0.0);
            #endif
                half4 shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);

                Light mainLight = GetMainLight(inputData.shadowCoord, input.positionWS, shadowMask);
                half halfLambert = dot(normalWS, mainLight.direction) * 0.5h + 0.5h;
                half receivedShadow = lerp(1.0h, mainLight.shadowAttenuation, _ShadowStrength);
                half lit = ToonBand(halfLambert) * smoothstep(0.3h, 0.7h, receivedShadow) * mainLight.distanceAttenuation;

                half3 diffuse = albedo.rgb * lerp(_ShadowColor.rgb, half3(1.0h, 1.0h, 1.0h), lit) * mainLight.color;
                half3 ambient = albedo.rgb * SampleSH(normalWS) * _AmbientStrength;
                AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
                ambient *= aoFactor.indirectAmbientOcclusion;
                diffuse *= aoFactor.directAmbientOcclusion;

                // Sun through the leaves when they stand between the sun and the eye.
                half backlit = pow(saturate(dot(viewDirWS, -mainLight.direction)), 3.0h) * receivedShadow;
                half3 through = albedo.rgb * mainLight.color * (_Translucency * backlit);

                half rimDot = 1.0h - saturate(dot(viewDirWS, normalWS));
                half rim = smoothstep(_RimThreshold - 0.04h, _RimThreshold + 0.04h, rimDot) * saturate(halfLambert + 0.1h) * lit;
                half3 rimLight = _RimColor.rgb * (_RimColor.a * rim) * albedo.rgb * 2.0h;

                half3 additional = half3(0.0h, 0.0h, 0.0h);
            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light directional = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                    additional += ToonAdditionalLight(directional, normalWS, albedo.rgb);
                }
                #endif

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                    additional += ToonAdditionalLight(light, normalWS, albedo.rgb);
                LIGHT_LOOP_END
            #endif

                half3 color = diffuse + ambient + through + rimLight + additional + _EmissionColor.rgb;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings ShadowVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                output.positionCS = positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 ShadowFragment(Varyings input) : SV_Target
            {
                ClipAlpha(input.uv);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half DepthFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                ClipAlpha(input.uv);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 DepthNormalsFragment(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                ClipAlpha(input.uv);
                float3 normalWS = normalize(float3(input.normalWS)) * IS_FRONT_VFACE(face, 1.0, -1.0);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                return half4(PackFloat2To888(remappedOctNormalWS), 0.0h);
            #else
                return half4(NormalizeNormalPerPixel(normalWS), 0.0h);
            #endif
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
