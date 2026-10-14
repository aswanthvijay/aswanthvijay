// RUNEHEIR: Echoes of Fimbul — Art Pass 3 (environment)
// Instanced grass tufts. The mesh's vertex colors run dark at the root to light at the tip (alpha = how far a vertex
// sways); the shader tints them with the map's grass color, varies them with the same world-space patches as the
// ground so tufts sit in their meadow, sways them in the wind, and lights them as the ground is lit (normal up), so
// grass and ground blend instead of reading as separate objects. Two-sided, no outline, receives shadows.
Shader "Runeheir/Grass"
{
    Properties
    {
        _BaseColor("Grass Color", Color) = (0.42, 0.63, 0.30, 1)
        _TipColor("Tip Color", Color) = (0.62, 0.78, 0.38, 1)
        _ShadowColor("Shadow Tint", Color) = (0.6, 0.66, 0.82, 1)
        _Brightness("Brightness", Range(0.3, 1.5)) = 0.95
        _AmbientStrength("Ambient Strength", Range(0, 2)) = 0.38
        _PatchScale("Patch Scale (per meter)", Range(0.005, 0.5)) = 0.06
        _PatchStrength("Patch Strength", Range(0, 0.5)) = 0.16
        _WindStrength("Wind Strength", Range(0, 0.5)) = 0.12
        _WindSpeed("Wind Speed", Range(0, 5)) = 1.6
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
            half4 _BaseColor;
            half4 _TipColor;
            half4 _ShadowColor;
            half _Brightness;
            half _AmbientStrength;
            float _PatchScale;
            half _PatchStrength;
            half _WindStrength;
            half _WindSpeed;
        CBUFFER_END

        float GrassHash(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float GrassNoise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            float2 u = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(GrassHash(i), GrassHash(i + float2(1, 0)), u.x), lerp(GrassHash(i + float2(0, 1)), GrassHash(i + float2(1, 1)), u.x), u.y);
        }

        float3 Sway(float3 positionWS, half weight)
        {
            float phase = _Time.y * _WindSpeed + positionWS.x * 0.35 + positionWS.z * 0.27;
            float gust = sin(phase) * 0.7 + sin(phase * 2.3 + 1.7) * 0.3;
            positionWS.xz += float2(0.8, 0.45) * gust * _WindStrength * weight;
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "GrassForward"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex GrassVertex
            #pragma fragment GrassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half4 color : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings GrassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = Sway(TransformObjectToWorld(input.positionOS.xyz), input.color.a);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                // Same shadow coordinates as URP's own shaders (cascades or screen-space shadows).
                VertexPositionInputs inputs = (VertexPositionInputs)0;
                inputs.positionWS = positionWS;
                inputs.positionCS = output.positionCS;
                inputs.positionVS = TransformWorldToView(positionWS);
                float4 ndc = output.positionCS * 0.5f;
                inputs.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
                inputs.positionNDC.zw = output.positionCS.zw;
                output.shadowCoord = GetShadowCoord(inputs);
                return output;
            }

            half4 GrassFragment(Varyings input) : SV_Target
            {
                float2 world = input.positionWS.xz;
                float patch = GrassNoise(world * _PatchScale) * 0.65 + GrassNoise(world * _PatchScale * 2.7 + 17.3) * 0.35;
                half3 albedo = lerp(_BaseColor.rgb, _TipColor.rgb, input.color.r) * (1.0h + (patch - 0.5) * 2.0 * _PatchStrength);
                albedo *= lerp(0.84h, 1.0h, input.color.g) * _Brightness;

                Light mainLight = GetMainLight(input.shadowCoord);
                half lit = smoothstep(0.3h, 0.7h, mainLight.shadowAttenuation) * mainLight.distanceAttenuation;
                half3 normalWS = half3(0.0h, 1.0h, 0.0h);
                half3 diffuse = albedo * lerp(_ShadowColor.rgb, half3(1.0h, 1.0h, 1.0h), lit) * mainLight.color;
                half3 ambient = albedo * SampleSH(normalWS) * _AmbientStrength;
                return half4(MixFog(diffuse + ambient, input.fogFactor), 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 DepthVertex(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformWorldToHClip(Sway(TransformObjectToWorld(input.positionOS.xyz), input.color.a));
            }

            half DepthFragment(float4 positionCS : SV_POSITION) : SV_Target
            {
                return positionCS.z;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
