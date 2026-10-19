Shader "Underwater/TransparentGlow"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2, 0.8, 1.0, 0.25)
        _EmissionStrength ("Emission", Range(0, 8)) = 2
        _PatternScale ("Pattern Scale", Range(0.01, 4)) = 0.5
        _Speed ("Speed", Range(0, 3)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "TransparentGlow"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _EmissionStrength;
                float _PatternScale;
                float _Speed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float waveA = sin((input.positionWS.x + input.positionWS.z) * _PatternScale + _Time.y * _Speed);
                float waveB = sin((input.positionWS.x * 0.7 - input.positionWS.z * 1.2) * _PatternScale - _Time.y * _Speed * 1.37);
                float pulse = 0.78 + (waveA + waveB) * 0.055;
                float fresnel = pow(1.0 - saturate(dot(normalize(input.normalWS), normalize(_WorldSpaceCameraPos - input.positionWS))), 2.0);
                float radial = saturate(1.0 - length(input.uv - 0.5) * 2.0);
                radial = radial * radial * (3.0 - 2.0 * radial);
                half3 color = _BaseColor.rgb * (_EmissionStrength * pulse + fresnel * 0.6);
                color = MixFog(color, input.fogFactor);
                return half4(color, saturate(_BaseColor.a * (pulse + fresnel * 0.22) * radial));
            }
            ENDHLSL
        }
    }
}

