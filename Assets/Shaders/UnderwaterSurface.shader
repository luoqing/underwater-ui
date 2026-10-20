Shader "Underwater/Surface"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.08, 0.62, 0.70, 0.48)
        _DeepColor ("Deep Color", Color) = (0.005, 0.08, 0.16, 0.72)
        _SunColor ("Sun Color", Color) = (0.88, 0.96, 1.0, 1)
        _SunIntensity ("Sun Intensity", Range(0, 6)) = 2.2
        _Distortion ("Distortion", Range(0, 0.08)) = 0.025
        _WaveScale ("Wave Scale", Range(0.02, 2)) = 0.32
        _WaveSpeed ("Wave Speed", Range(0, 3)) = 0.45
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "UnderwaterSurface"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _SunColor;
                float _SunIntensity;
                float _Distortion;
                float _WaveScale;
                float _WaveSpeed;
                float _FresnelPower;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldSeed = TransformObjectToWorld(input.positionOS.xyz);
                float waveA = sin((worldSeed.x * 0.22 + worldSeed.z * 0.17) * _WaveScale * 6.0 + _Time.y * _WaveSpeed);
                float waveB = sin((worldSeed.x * -0.14 + worldSeed.z * 0.29) * _WaveScale * 7.0 - _Time.y * _WaveSpeed * 1.37);
                float3 displaced = input.positionOS.xyz;
                displaced.y += (waveA + waveB) * 0.13;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(displaced);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y * _WaveSpeed;
                float2 p = input.positionWS.xz * _WaveScale;
                float2 wave = float2(
                    sin(p.x * 1.31 + p.y * 0.73 + time) + sin(p.y * 2.17 - p.x * 0.29 - time * 1.31),
                    cos(p.y * 1.43 - p.x * 0.61 - time * 0.83) + cos(p.x * 2.03 + p.y * 0.37 + time * 1.17)
                ) * 0.34;
                wave += float2(
                    sin((p.x - p.y) * 3.17 - time * 0.57),
                    cos((p.x + p.y) * 2.81 + time * 0.71)
                ) * 0.12;
                float2 screenUV = input.screenPos.xy / max(input.screenPos.w, 0.0001);
                half3 refracted = SampleSceneColor(saturate(screenUV + wave * _Distortion));
                float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                float3 animatedNormal = normalize(input.normalWS + float3(wave.x * 0.24, 0.62, wave.y * 0.24));
                float surfaceFacing = saturate(abs(dot(animatedNormal, viewDir)));
                float fresnel = pow(1.0 - surfaceFacing, _FresnelPower);
                float crest = pow(saturate(wave.x * wave.y * 0.22 + 0.43), 8.0);
                Light mainLight = GetMainLight();
                float sunGlint = pow(saturate(dot(reflect(-mainLight.direction, animatedNormal), viewDir)), 54.0);
                float microWaves = sin(p.x * 7.1 + p.y * 1.9 + time * 2.1) * sin(p.y * 8.3 - p.x * 1.3 - time * 1.7);
                float sparkle = pow(saturate(microWaves * 0.24 + 0.48), 14.0) * saturate(0.28 + sunGlint);
                half3 tint = lerp(_DeepColor.rgb, _ShallowColor.rgb, 0.66 + wave.y * 0.035);
                half3 color = lerp(refracted * tint * 1.48, _ShallowColor.rgb, fresnel * 0.60);
                color += _SunColor.rgb * (sunGlint * _SunIntensity + sparkle * 0.16 + crest * 0.08);
                color = MixFog(color, input.fogFactor);
                half alpha = saturate(lerp(_DeepColor.a, _ShallowColor.a, fresnel) + crest * 0.06 + sunGlint * 0.08);
                float horizonFade = smoothstep(0.025, 0.24, surfaceFacing);
                alpha *= horizonFade;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
