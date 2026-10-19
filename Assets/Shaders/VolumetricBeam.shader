Shader "Underwater/VolumetricBeam"
{
    Properties
    {
        _BaseColor ("Beam Color", Color) = (0.32, 0.86, 0.92, 0.12)
        _Intensity ("Intensity", Range(0, 5)) = 1.4
        _Speed ("Shimmer Speed", Range(0, 3)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-5" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "VolumetricBeam"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Intensity;
                float _Speed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float radial = pow(saturate(1.0 - abs(input.uv.x * 2.0 - 1.0)), 1.85);
                float vertical = smoothstep(0.0, 0.18, input.uv.y) * (1.0 - smoothstep(0.72, 1.0, input.uv.y));
                float noiseA = sin(input.positionWS.y * 0.23 + input.positionWS.x * 0.17 + _Time.y * _Speed);
                float noiseB = sin(input.positionWS.z * 0.19 - input.positionWS.y * 0.11 - _Time.y * _Speed * 0.73);
                float noiseC = sin(input.positionWS.x * 0.09 + input.positionWS.z * 0.13 + _Time.y * _Speed * 0.41);
                float breakup = 0.68 + noiseA * 0.09 + noiseB * 0.07 + noiseC * 0.06;
                float3 viewDirection = SafeNormalize(_WorldSpaceCameraPos - input.positionWS);
                float grazingFade = pow(saturate(abs(dot(normalize(input.normalWS), viewDirection))), 0.65);
                float alpha = radial * vertical * breakup * grazingFade * _BaseColor.a;
                half3 color = _BaseColor.rgb * _Intensity * (0.62 + radial * 0.38);
                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
