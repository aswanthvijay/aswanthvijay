// RUNEHEIR: Echoes of Fimbul — GDD Phase 2 Step 5
// Cel-shaded "HD-XileRO" anime toon shader for URP (Unity 6.1+ / URP 17.1+):
//   • hard-edged light/shadow band with a cool shadow tint and crisp received shadows
//   • anime specular highlight, rim light, emission (runestone glow)
//   • additional lights (point/spot) as banded light, Forward and Forward+ (cluster light loop)
//   • screen-space shadows and SSAO
//   • black "ink" outline: inverted-hull pass with constant on-screen width
// Passes: UniversalForwardOnly, SRPDefaultUnlit (outline), ShadowCaster, DepthOnly, DepthNormals.
// SRP Batcher compatible (all material properties live in UnityPerMaterial).
Shader "Runeheir/Toon"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Cel Shading)]
        _ShadowColor("Shadow Tint", Color) = (0.62, 0.66, 0.82, 1)
        _RampThreshold("Light / Shadow Threshold", Range(0, 1)) = 0.52
        _RampSmoothness("Band Edge Softness", Range(0.001, 0.5)) = 0.03
        _ShadowStrength("Received Shadow Strength", Range(0, 1)) = 1
        _AmbientStrength("Ambient Strength", Range(0, 2)) = 0.6

        [Header(Specular)]
        _SpecularColor("Specular Color (A = intensity)", Color) = (1, 1, 1, 0.35)
        _SpecularSize("Specular Size", Range(0, 1)) = 0.1

        [Header(Rim)]
        _RimColor("Rim Color (A = intensity)", Color) = (1, 0.95, 0.85, 0.4)
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.72

        [Header(Emission)]
        [HDR] _EmissionColor("Emission", Color) = (0, 0, 0, 1)

        [Header(Ink Outline)]
        _OutlineColor("Outline Color", Color) = (0.07, 0.05, 0.06, 1)
        _OutlineWidth("Outline Width (pixels at 1080p)", Range(0, 8)) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadowColor;
            half _RampThreshold;
            half _RampSmoothness;
            half _ShadowStrength;
            half _AmbientStrength;
            half4 _SpecularColor;
            half _SpecularSize;
            half4 _RimColor;
            half _RimThreshold;
            half4 _EmissionColor;
            half4 _OutlineColor;
            float _OutlineWidth;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        ENDHLSL

        // ------------------------------------------------------------------ cel-shaded lighting
        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ToonVertex
            #pragma fragment ToonFragment

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
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord : TEXCOORD4;
            #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ToonVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(positionInputs);
            #endif
                return output;
            }

            half ToonBand(half value)
            {
                return smoothstep(_RampThreshold - _RampSmoothness, _RampThreshold + _RampSmoothness, value);
            }

            // Point/spot lights: crisp N·L band, smooth distance falloff.
            half3 ToonAdditionalLight(Light light, float3 normalWS, half3 albedo)
            {
                half ndotl = saturate(dot(normalWS, light.direction));
                half band = smoothstep(0.05h, 0.05h + _RampSmoothness * 2.0h, ndotl);
                return albedo * light.color * (band * light.distanceAttenuation * light.shadowAttenuation);
            }

            half4 ToonFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // The cluster light loop macros (Forward+) read inputData.positionWS / normalizedScreenSpaceUV.
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                // Same shadow-coordinate rules as URP Lit (screen-space shadows, single cascade, cascades).
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                inputData.shadowCoord = input.shadowCoord;
            #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            #else
                inputData.shadowCoord = float4(0.0, 0.0, 0.0, 0.0);
            #endif
                half4 shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);

                // Main light: one hard band (half-Lambert) multiplied by crisp received shadows.
                Light mainLight = GetMainLight(inputData.shadowCoord, input.positionWS, shadowMask);
                half halfLambert = dot(normalWS, mainLight.direction) * 0.5h + 0.5h;
                half receivedShadow = lerp(1.0h, mainLight.shadowAttenuation, _ShadowStrength);
                half lit = ToonBand(halfLambert) * smoothstep(0.35h, 0.65h, receivedShadow) * mainLight.distanceAttenuation;

                half3 diffuse = albedo.rgb * lerp(_ShadowColor.rgb, half3(1.0h, 1.0h, 1.0h), lit) * mainLight.color;
                half3 ambient = albedo.rgb * SampleSH(normalWS) * _AmbientStrength;

                // SSAO (enabled on the PC renderer): darkens ambient fully and the lit band by the renderer's
                // "Direct Lighting Strength".
                AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
                ambient *= aoFactor.indirectAmbientOcclusion;
                diffuse *= aoFactor.directAmbientOcclusion;

                float3 halfDir = SafeNormalize(float3(mainLight.direction) + float3(viewDirWS));
                half specBand = smoothstep(1.0h - _SpecularSize - 0.01h, 1.0h - _SpecularSize + 0.01h, saturate(dot(normalWS, halfDir)));
                half3 specular = _SpecularColor.rgb * (_SpecularColor.a * specBand * lit) * mainLight.color;

                half rimDot = 1.0h - saturate(dot(viewDirWS, normalWS));
                half rim = smoothstep(_RimThreshold - 0.02h, _RimThreshold + 0.02h, rimDot) * saturate(halfLambert + 0.2h);
                half3 rimLight = _RimColor.rgb * (_RimColor.a * rim);

                half3 additional = half3(0.0h, 0.0h, 0.0h);
            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                // Forward+: additional directional lights are not in the clusters.
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

                half3 color = diffuse + ambient + specular + rimLight + additional + _EmissionColor.rgb;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------ ink outline (inverted hull)
        Pass
        {
            Name "InkOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half fogFactor : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings OutlineVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float2 normalCS = TransformWorldToHClipDir(normalWS).xy;
                float normalLength = length(normalCS);
                float2 direction = normalLength > 1e-5 ? normalCS / normalLength : float2(0.0, 0.0);

                // Constant on-screen thickness: _OutlineWidth pixels at 1080p, scaled with resolution.
                float2 pixelToClip = float2(_ScreenParams.y / _ScreenParams.x, 1.0) * (2.0 / 1080.0);
                positionCS.xy += direction * (_OutlineWidth * positionCS.w) * pixelToClip;

                output.positionCS = positionCS;
                output.fogFactor = ComputeFogFactor(positionCS.z);
                return output;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return half4(MixFog(_OutlineColor.rgb, input.fogFactor), 1.0h);
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------ shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // Set by URP's ShadowUtils for the light currently rendering shadows.
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
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
                return output;
            }

            half4 ShadowFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------ depth prepass / depth texture
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
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
                return output;
            }

            half DepthFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------ normals for SSAO / decals
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
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
                return output;
            }

            half4 DepthNormalsFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 normalWS = normalize(float3(input.normalWS));
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

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
