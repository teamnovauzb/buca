Shader "BUCA/Station Contact"
{
    Properties {_BaseColor("Shade",Color)=(.015,.025,.018,.5)}
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct A {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct V {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
            V vert(A a){V o;o.pos=TransformObjectToHClip(a.vertex.xyz);o.uv=a.uv;return o;}
            half4 frag(V i):SV_Target {float r=length((i.uv-.5)*2);return half4(_BaseColor.rgb,_BaseColor.a*pow(saturate(1-r*r),2));}
            ENDHLSL
        }
    }
}
