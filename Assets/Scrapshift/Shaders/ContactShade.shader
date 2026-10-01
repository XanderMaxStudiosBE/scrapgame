Shader "Scrapshift/Soft Contact Shade"
{
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend DstColor Zero
        ZWrite Off Cull Off
        Offset -1,-1
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; half opacity:TEXCOORD0; half fog:TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.opacity=input.color.a;
                output.fog=ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                // Fade toward white (neutral multiplication) in fog rather than drawing distant dark patches.
                half shade=1-input.opacity*ComputeFogIntensity(input.fog);
                return half4(shade,shade,shade,1);
            }
            ENDHLSL
        }
    }
}
