// RUNEHEIR: Echoes of Fimbul — Art Pass 3 (environment)
// Ground for the generated maps: the mesh's vertex colors carry the blended ground kinds (grass, path, paving, snow...),
// and the shader adds what flat color can't: soft patches of light and dark, fine grain and painterly flecks, all in
// world space so neighbouring tiles always match. Lit like Runeheir/Toon (one soft band, a cool shadow tint, crisp
// received shadows, SSAO, fog, point lights). Passes: UniversalForwardOnly, ShadowCaster, DepthOnly, DepthNormals.
Shader "Runeheir/Terrain"
{
    Properties
    {
        _Brightness("Brightness", Range(0.3, 1.5)) = 0.95
        _ShadowColor("Shadow Tint", Color) = (0.6, 0.66, 0.82, 1)
        _ShadowStrength("Received Shadow Strength", Range(0, 1)) = 0.85
        _AmbientStrength("Ambient Strength", Range(0, 2)) = 0.38
        _PatchStrength("Patch Strength", Range(0, 0.5)) = 0.16
        _PatchScale("Patch Scale (per meter)", Range(0.005, 0.5)) = 0.06
        _GrainStrength("Grain Strength", Range(0, 0.5)) = 0.07
        _GrainScale("Grain Scale (per meter)", Range(0.1, 8)) = 1.7
        _FleckStrength("Fleck Strength", Range(0, 0.5)) = 0.07
        _FleckScale("Fleck Scale (per meter)", Range(0.5, 16)) = 5.0
        _StoneScale("Flagstones per meter", Range(0.2, 3)) = 0.85
        _GrooveDarkness("Groove Darkness", Range(0, 1)) = 0.45
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
            half _Brightness;
            half4 _ShadowColor;
            half _ShadowStrength;
            half _AmbientStrength;
            half _PatchStrength;
            float _PatchScale;
            half _GrainStrength;
            float _GrainScale;
            half _FleckStrength;
            float _FleckScale;
            float _StoneScale;
            half _GrooveDarkness;
        CBUFFER_END

        float TerrainHash(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float TerrainNoise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            float2 u = f * f * (3.0 - 2.0 * f);
            float a = TerrainHash(i);
            float b = TerrainHash(i + float2(1, 0));
            float c = TerrainHash(i + float2(0, 1));
            float d = TerrainHash(i + float2(1, 1));
            return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
        }
        float2 TerrainHash2(float2 p)
        {
            return float2(TerrainHash(p), TerrainHash(p + 19.19));
        }

        // Irregular flagstones: Voronoi cells, x = the groove between stones (1 in the crack), y = one value per stone.
        float2 Flagstones(float2 p)
        {
            float2 cell = floor(p);
            float2 local = frac(p);
            float nearest = 8.0;
            float second = 8.0;
            float id = 0.0;
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    float2 offset = float2(x, y);
                    float2 site = offset + 0.15 + 0.7 * TerrainHash2(cell + offset);
                    float d = length(site - local);
                    if (d < nearest)
                    {
                        second = nearest;
                        nearest = d;
                        id = TerrainHash(cell + offset + 7.7);
                    }
                    else if (d < second)
                    {
                        second = d;
                    }
                }
            }

            return float2(1.0 - smoothstep(0.03, 0.09, second - nearest), id);
        }
        ENDHLSL

        Pass
        {
            Name "TerrainForward"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex TerrainVertex
            #pragma fragment TerrainFragment

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
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                float4 shadowCoord : TEXCOORD4;
            #endif
            };

            Varyings TerrainVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                output.shadowCoord = GetShadowCoord(positionInputs);
            #endif
                return output;
            }

            half3 TerrainAdditionalLight(Light light, float3 normalWS, half3 albedo)
            {
                half ndotl = saturate(dot(normalWS, light.direction));
                return albedo * light.color * (smoothstep(0.05h, 0.2h, ndotl) * light.distanceAttenuation * light.shadowAttenuation);
            }

            half4 TerrainFragment(Varyings input) : SV_Target
            {
                float2 world = input.positionWS.xz;

                // Large soft patches, fine grain and small painterly flecks: brightness only, so every ground kind keeps its hue.
                float patch = TerrainNoise(world * _PatchScale) * 0.65 + TerrainNoise(world * _PatchScale * 2.7 + 17.3) * 0.35;
                float grain = TerrainNoise(world * _GrainScale + 3.1) - 0.5;
                float fleck = TerrainNoise(world * _FleckScale + 9.7);
                half shade = 1.0h + (patch - 0.5) * 2.0 * _PatchStrength + grain * _GrainStrength;
                shade += smoothstep(0.82, 0.9, fleck) * _FleckStrength - smoothstep(0.82, 0.9, 1.0 - fleck) * _FleckStrength * 0.8;
                half3 albedo = input.color.rgb * shade * _Brightness;

                // Paved ground (vertex alpha) is laid with flagstones: each stone a little lighter or darker, dark grooves between.
                if (input.color.a > 0.01h)
                {
                    float2 stones = Flagstones(world * _StoneScale);
                    half stone = (1.0h + (stones.y - 0.5) * 0.16h) * (1.0h - stones.x * _GrooveDarkness);
                    albedo *= lerp(1.0h, stone, input.color.a);
                }

                float3 normalWS = normalize(input.normalWS);
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
            #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                inputData.shadowCoord = input.shadowCoord;
            #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            #else
                inputData.shadowCoord = float4(0.0, 0.0, 0.0, 0.0);
            #endif
                half4 shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);

                Light mainLight = GetMainLight(inputData.shadowCoord, input.positionWS, shadowMask);
                half halfLambert = dot(normalWS, mainLight.direction) * 0.5h + 0.5h;
                half receivedShadow = lerp(1.0h, mainLight.shadowAttenuation, _ShadowStrength);
                half lit = smoothstep(0.45h, 0.55h, halfLambert) * smoothstep(0.3h, 0.7h, receivedShadow) * mainLight.distanceAttenuation;

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
                    additional += TerrainAdditionalLight(directional, normalWS, albedo);
                }
                #endif

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                    additional += TerrainAdditionalLight(light, normalWS, albedo);
                LIGHT_LOOP_END
            #endif

                half3 color = MixFog(diffuse + ambient + additional, input.fogFactor);
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
            #pragma target 2.0
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
            #pragma target 2.0
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
            #pragma target 2.0
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

    FallBack "Universal Render Pipeline/Simple Lit"
}
