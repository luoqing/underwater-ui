Shader "Underwater/CausticSeafloor"
{
    Properties
    {
        _BaseMap ("Sand Albedo", 2D) = "white" {}
        _NormalMap ("Sand Normal", 2D) = "bump" {}
        _RoughnessMap ("Sand Roughness", 2D) = "white" {}
        _AOMap ("Sand AO", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.74, 0.80, 0.72, 1)
        _CausticColor ("Caustic Color", Color) = (0.68, 0.91, 1.0, 1)
        _CausticStrength ("Caustic Strength", Range(0, 2)) = 0.48
        _CausticScale ("Caustic Scale", Range(0.1, 5)) = 0.72
        _CausticSpeed ("Caustic Speed", Range(0, 3)) = 0.32
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.72
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "CausticForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_RoughnessMap);
            SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_AOMap);
            SAMPLER(sampler_AOMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _CausticColor;
                float _CausticStrength;
                float _CausticScale;
                float _CausticSpeed;
                float _NormalStrength;
                float4 _BaseMap_ST;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalTS = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv).xyz * 2.0h - 1.0h;
                normalTS.xy *= _NormalStrength;
                normalTS = normalize(normalTS);
                half3 baseNormal = normalize(input.normalWS);
                half3 tangent = normalize(input.tangentWS.xyz);
                half3 bitangent = normalize(cross(baseNormal, tangent) * input.tangentWS.w);
                half3 normalWS = normalize(tangent * normalTS.x + bitangent * normalTS.y + baseNormal * normalTS.z);

                half3 sampledAlbedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half luminance = dot(sampledAlbedo, half3(0.299h, 0.587h, 0.114h));
                sampledAlbedo = pow(saturate(lerp(luminance.xxx, sampledAlbedo, 0.22h)), 0.62h);
                float macroVariation = 0.90 + sin(input.positionWS.x * 0.075 + input.positionWS.z * 0.052) * 0.055;
                float sandBand = sin(input.positionWS.z * 1.45 + sin(input.positionWS.x * 0.43) * 1.3) * 0.025;
                float slope = saturate(1.0 - normalWS.y);
                float slopeShade = lerp(1.04, 0.72, smoothstep(0.12, 0.62, slope));
                half3 albedo = sampledAlbedo * _BaseColor.rgb * (macroVariation + sandBand) * slopeShade;
                half roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv).r;
                half ao = SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, input.uv).r;

                float2 p = input.positionWS.xz * _CausticScale;
                float time = _Time.y * _CausticSpeed;
                float2 warp = float2(
                    sin(p.y * 0.43 - time * 0.37) + sin((p.x + p.y) * 0.19 + time * 0.23),
                    sin(p.x * 0.37 + time * 0.31) + sin((p.x - p.y) * 0.21 - time * 0.19)
                ) * 0.28;
                float2 q = p + warp;
                float a = abs(sin(q.x * 1.13 + q.y * 0.41 + time * 0.83) + sin(q.y * 1.47 - q.x * 0.29 - time * 0.61)) * 0.5;
                float b = abs(sin((q.x + q.y) * 0.79 - time * 0.91) + sin((q.x - q.y) * 1.21 + time * 0.53)) * 0.5;
                float fineLines = pow(saturate(1.0 - min(a, b)), 30.0);
                float shimmer = 0.72 + 0.28 * sin(q.x * 0.31 + q.y * 0.27 + time * 0.46);
                float caustic = fineLines * shimmer * _CausticStrength;
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float diffuse = saturate(dot(normalWS, normalize(mainLight.direction)));
                half3 ambient = SampleSH(normalWS) * albedo * lerp(0.58h, 1.0h, ao);
                half3 direct = albedo * mainLight.color * diffuse * mainLight.shadowAttenuation;
                half3 viewDir = SafeNormalize(_WorldSpaceCameraPos - input.positionWS);
                half3 halfDir = SafeNormalize(viewDir + mainLight.direction);
                half smoothness = 1.0h - roughness;
                half specular = pow(saturate(dot(normalWS, halfDir)), lerp(7.0h, 64.0h, smoothness)) * smoothness * 0.18h;
                float depthFade = saturate(1.0 - max(0.0, -input.positionWS.y) * 0.045);
                half3 color = ambient + direct + mainLight.color * specular * mainLight.shadowAttenuation;
                color += _CausticColor.rgb * caustic * depthFade * (0.22h + diffuse * 0.34h) * mainLight.shadowAttenuation;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
