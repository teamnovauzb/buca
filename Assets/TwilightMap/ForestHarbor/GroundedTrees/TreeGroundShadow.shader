Shader "BUCA/Tree Ground Shadow"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            V vert(A a){V o;o.pos=TransformObjectToHClip(a.vertex.xyz);o.uv=a.uv;o.color=a.color;return o;}
            half4 frag(V i):SV_Target
            {
                float r=length(i.uv*2-1);
                return half4(i.color.rgb,i.color.a*pow(saturate(1-r*r),2));
            }
            ENDHLSL
        }
    }
}
