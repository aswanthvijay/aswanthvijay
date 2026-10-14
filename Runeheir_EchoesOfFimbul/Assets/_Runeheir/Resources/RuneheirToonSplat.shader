// RUNEHEIR: Echoes of Fimbul — art direction sample (stylized anime 3D)
// A painted, cel-shaded ground for Unity Terrain: four layers from the terrain's control map, blended by their own
// brightness (stones and tufts rise above the soil between them), softened toward paint by a mip bias, tinted and
// saturated per layer, then lit with the Runeheir/Toon band, received shadows and a cool shadow tint.
// Draw the terrain un-instanced (Terrain.drawInstanced = false); layers tile in world space.
Shader "Runeheir/ToonSplat"
{
    Properties
    {
        [HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _Splat0("Layer 0", 2D) = "grey" {}
        [HideInInspector] _Splat1("Layer 1", 2D) = "grey" {}
        [HideInInspector] _Splat2("Layer 2", 2D) = "grey" {}
        [HideInInspector] _Splat3("Layer 3", 2D) = "grey" {}
        _TileSizes("Layer Tile Sizes (m)", Vector) = (3, 3, 3, 3)
        _Tint0("Layer 0 Tint", Color) = (1, 1, 1, 1)
        _Tint1("Layer 1 Tint", Color) = (1, 1, 1, 1)
        _Tint2("Layer 2 Tint", Color) = (1, 1, 1, 1)
        _Tint3("Layer 3 Tint", Color) = (1, 1, 1, 1)
        _BlendDepth("Height Blend Depth", Range(0.01, 1)) = 0.18
        _MipBias("Painterly Softening (mip bias)", Range(0, 4)) = 1.5
        _Saturation("Saturation", Range(0, 2)) = 1.25

        _ShadowColor("Shadow Tint", Color) = (0.58, 0.64, 0.86, 1)
        _RampThreshold("Light / Shadow Threshold", Range(0, 1)) = 0.45
        _RampSmoothness("Band Edge Softness", Range(0.001, 0.5)) = 0.12
        _ShadowStrength("Received Shadow Strength", Range(0, 1)) = 0.85
        _AmbientStrength("Ambient Strength", Range(0, 2)) = 0.7
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry-100"
            "TerrainCompatible" = "True"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _Control_ST;
            float4 _TileSizes;
            half4 _Tint0;
            half4 _Tint1;
            half4 _Tint2;
            half4 _Tint3;
            half _BlendDepth;
            half _MipBias;
            half _Saturation;
            half4 _ShadowColor;
            half _RampThreshold;
            half _RampSmoothness;
            half _ShadowStrength;
            half _AmbientStrength;
        CBUFFER_END

        TEXTURE2D(_Control); SAMPLER(sampler_Control);
        TEXTURE2D(_Splat0); SAMPLER(sampler_Splat0);
        TEXTURE2D(_Splat1);
        TEXTURE2D(_Splat2);
        TEXTURE2D(_Splat3);
        ENDHLSL

        Pass
        {
            Name "ToonSplatForward"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex SplatVertex
            #pragma fragment SplatFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings SplatVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half3 Saturate(half3 color)
            {
                half luma = dot(color, half3(0.2126h, 0.7152h, 0.0722h));
                return max(lerp(luma.xxx, color, _Saturation), 0.0h);
            }

            half4 SplatFragment(Varyings input) : SV_Target
            {
                half4 control = SAMPLE_TEXTURE2D(_Control, sampler_Control, input.uv);
                float2 world = input.positionWS.xz;
                half3 c0 = SAMPLE_TEXTURE2D_BIAS(_Splat0, sampler_Splat0, world / _TileSizes.x, _MipBias).rgb * _Tint0.rgb;
                half3 c1 = SAMPLE_TEXTURE2D_BIAS(_Splat1, sampler_Splat0, world / _TileSizes.y, _MipBias).rgb * _Tint1.rgb;
                half3 c2 = SAMPLE_TEXTURE2D_BIAS(_Splat2, sampler_Splat0, world / _TileSizes.z, _MipBias).rgb * _Tint2.rgb;
                half3 c3 = SAMPLE_TEXTURE2D_BIAS(_Splat3, sampler_Splat0, world / _TileSizes.w, _MipBias).rgb * _Tint3.rgb;

                // Height blend: each layer's own brightness stands in for its height.
                half3 lumaWeights = half3(0.2126h, 0.7152h, 0.0722h);
                half4 heights = half4(dot(c0, lumaWeights), dot(c1, lumaWeights), dot(c2, lumaWeights), dot(c3, lumaWeights));
                half4 h = control * (heights + 0.25h) + control;
                half top = max(max(h.x, h.y), max(h.z, h.w));
                half4 w = max(h - top + _BlendDepth, 0.0h);
                w /= max(dot(w, half4(1.0h, 1.0h, 1.0h, 1.0h)), 1e-4h);
                half3 albedo = Saturate(c0 * w.x + c1 * w.y + c2 * w.z + c3 * w.w);

                float3 normalWS = normalize(input.normalWS);
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
                half band = smoothstep(_RampThreshold - _RampSmoothness, _RampThreshold + _RampSmoothness, halfLambert);
                half receivedShadow = lerp(1.0h, mainLight.shadowAttenuation, _ShadowStrength);
                half lit = band * smoothstep(0.3h, 0.7h, receivedShadow);

                half3 diffuse = albedo * lerp(_ShadowColor.rgb, half3(1.0h, 1.0h, 1.0h), lit) * mainLight.color;
                half3 ambient = albedo * SampleSH(normalWS) * _AmbientStrength;
                AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
                ambient *= aoFactor.indirectAmbientOcclusion;
                diffuse *= aoFactor.directAmbientOcclusion;

                half3 additional = half3(0.0h, 0.0h, 0.0h);
            #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light directional = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                    additional += albedo * directional.color * directional.distanceAttenuation * saturate(dot(normalWS, directional.direction));
                }
                #endif

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                    half ndotl = saturate(dot(normalWS, light.direction));
                    additional += albedo * light.color * (smoothstep(0.05h, 0.3h, ndotl) * light.distanceAttenuation * light.shadowAttenuation);
                LIGHT_LOOP_END
            #endif

                half3 color = diffuse + ambient + additional;
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
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            float4 ShadowVertex(Attributes input) : SV_POSITION
            {
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
                return positionCS;
            }

            half4 ShadowFragment() : SV_Target
            {
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
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment

            float4 DepthVertex(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half DepthFragment(float4 positionCS : SV_POSITION) : SV_Target
            {
                return positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
            };

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 DepthNormalsFragment(Varyings input) : SV_Target
            {
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
