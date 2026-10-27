Shader "BUCA/Station Light Pool"
{
    Properties { _Color("Warm light",Color)=(1,.49,.12,.32) }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-1"}
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            CBUFFER_END
            struct A {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct V {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
            V vert(A a){V o;o.pos=TransformObjectToHClip(a.vertex.xyz);o.uv=a.uv;return o;}
            half4 frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                float feather=pow(saturate(1-dot(p,p)),3);
                return half4(_Color.rgb,_Color.a*feather);
            }
            ENDHLSL
        }
    }
}
