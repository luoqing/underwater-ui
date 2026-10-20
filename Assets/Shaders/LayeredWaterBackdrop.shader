Shader "Underwater/LayeredWaterBackdrop"
{
    Properties
    {
        _TopColor ("Surface Color", Color) = (0.10, 0.52, 0.63, 1)
        _HorizonColor ("Mid Water Color", Color) = (0.025, 0.30, 0.45, 1)
        _BottomColor ("Deep Water Color", Color) = (0.006, 0.07, 0.14, 1)
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _BottomColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float height = normalize(input.direction).y;
                float lowerBlend = smoothstep(-0.72, 0.02, height);
                float upperBlend = smoothstep(-0.02, 0.78, height);
                half3 lower = lerp(_BottomColor.rgb, _HorizonColor.rgb, lowerBlend);
                half3 color = lerp(lower, _TopColor.rgb, upperBlend);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
