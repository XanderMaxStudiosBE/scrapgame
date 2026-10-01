Shader "Scrapshift/Cozy Yard Sky"
{
    Properties
    {
        _ZenithColor("Sky overhead", Color) = (0.22,0.38,0.52,1)
        _HorizonColor("Warm horizon", Color) = (0.76,0.74,0.66,1)
        _GroundColor("Ground bounce", Color) = (0.27,0.29,0.25,1)
        _SunDirection("Direction to sun", Vector) = (0,1,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "RenderPipeline"="UniversalPipeline" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor, _HorizonColor, _GroundColor;
                float4 _SunDirection;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 direction:TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.direction=input.positionOS.xyz;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float3 direction=normalize(input.direction);
                float height=direction.y;
                half3 sky=lerp(_HorizonColor.rgb,_ZenithColor.rgb,pow(saturate(height),0.65));
                sky=lerp(sky,_GroundColor.rgb,smoothstep(0.0,0.45,-height));
                float alignment=saturate(dot(direction,normalize(_SunDirection.xyz)));
                // A broad restrained glow and small disc, without animated noise or expensive sky scattering.
                sky+=half3(0.22,0.14,0.06)*pow(alignment,32.0);
                sky+=half3(1.0,0.80,0.52)*smoothstep(0.9996,0.99985,alignment);
                return half4(sky,1);
            }
            ENDHLSL
        }
    }
}
