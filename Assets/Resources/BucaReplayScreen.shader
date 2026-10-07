Shader "Buca/ReplayScreen"
{
    Properties
    {
        _MainTex ("Recorded shot", 2D) = "black" {}
        _Aspect ("Window aspect", Float) = 2.45
        _Radius ("Corner radius", Float) = .035
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float _Aspect, _Radius;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(input.positionOS.xyz); o.uv=input.uv; return o;
            }
            half4 frag(Varyings input):SV_Target
            {
                float2 halfSize=float2(_Aspect,1)*.5;
                float2 q=abs(input.uv-.5)*float2(_Aspect,1)-(halfSize-_Radius);
                float distance=length(max(q,0))+min(max(q.x,q.y),0)-_Radius;
                clip(-distance);
                return half4(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).rgb,1);
            }
            ENDHLSL
        }
    }
}
