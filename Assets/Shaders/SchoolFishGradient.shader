Shader "Underwater/SchoolFishGradient"
{
    Properties
    {
        _DorsalColor ("Dorsal Color", Color) = (0.03, 0.18, 0.30, 1)
        _MidColor ("Mid Color", Color) = (0.12, 0.48, 0.68, 1)
        _BellyColor ("Belly Color", Color) = (0.68, 0.86, 0.86, 1)
        _HeadColor ("Head Color", Color) = (0.46, 0.78, 0.84, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.58
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "SchoolFishForward"
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
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _DorsalColor;
                float4 _MidColor;
                float4 _BellyColor;
                float4 _HeadColor;
                float _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half bellyMask = 1.0h - smoothstep(-0.28h, 0.16h, input.positionOS.y);
                half dorsalMask = smoothstep(0.03h, 0.43h, input.positionOS.y);
                half headMask = smoothstep(0.22h, 0.80h, input.positionOS.x);

                half3 albedo = _MidColor.rgb;
                albedo = lerp(albedo, _BellyColor.rgb, bellyMask * 0.88h);
                albedo = lerp(albedo, _DorsalColor.rgb, dorsalMask * 0.92h);
                albedo = lerp(albedo, _HeadColor.rgb, headMask * (0.42h + bellyMask * 0.18h));

                half3 normalWS = normalize(input.normalWS);
                half3 viewDirection = SafeNormalize(_WorldSpaceCameraPos - input.positionWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half diffuse = saturate(dot(normalWS, mainLight.direction));
                half3 ambient = SampleSH(normalWS) * albedo;
                half3 direct = albedo * mainLight.color * diffuse * mainLight.shadowAttenuation;
                half3 halfDirection = SafeNormalize(viewDirection + mainLight.direction);
                half specular = pow(saturate(dot(normalWS, halfDirection)), lerp(12.0h, 72.0h, _Smoothness)) * _Smoothness * 0.34h;
                half rim = pow(1.0h - saturate(dot(normalWS, viewDirection)), 3.4h) * 0.10h;
                half3 color = ambient + direct + mainLight.color * specular * mainLight.shadowAttenuation;
                color += _BellyColor.rgb * rim;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
