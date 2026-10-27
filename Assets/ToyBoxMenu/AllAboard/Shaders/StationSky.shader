Shader "BUCA/Station Sky"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Background"}
        Pass
        {
            Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A{float4 vertex:POSITION;};
            struct V{float4 pos:SV_POSITION;float3 world:TEXCOORD0;};
            V vert(A a){V o;o.world=TransformObjectToWorld(a.vertex.xyz);o.pos=TransformWorldToHClip(o.world);return o;}
            half4 frag(V i):SV_Target
            {
                float3 ray=normalize(i.world-_WorldSpaceCameraPos);
                float t=smoothstep(-.25,.02,ray.y);
                return half4(lerp(half3(.23,.34,.46),half3(.025,.067,.17),t),1);
            }
            ENDHLSL
        }
    }
}
