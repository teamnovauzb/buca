Shader "Buca/ResultsBackdrop"
{
    Properties { _MainTex ("Captured Level", 2D) = "black" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Opaque" }
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float4 screen:TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                o.screen=ComputeScreenPos(o.positionCS); return o;
            }
            half4 frag(Varyings input):SV_Target
            {
                half3 color=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.screen.xy/input.screen.w).rgb;
                return half4(color*.78,1);
            }
            ENDHLSL
        }
    }
}
