Shader "BUCA/Station Canal"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A{float4 vertex:POSITION;};
            struct V{float4 pos:SV_POSITION;float3 world:TEXCOORD0;float2 local:TEXCOORD1;};
            V vert(A a){V o;o.world=TransformObjectToWorld(a.vertex.xyz);o.pos=TransformWorldToHClip(o.world);o.local=a.vertex.xz;return o;}
            half4 frag(V i):SV_Target
            {
                float2 p=i.world.xz;
                float wave=sin(p.x*4.7+p.y*5.9+_Time.y*.55)*sin(p.y*15.3-p.x*1.1-_Time.y*.4);
                float ripple=pow(saturate(wave*.5+.5),12);
                float shimmer=sin(p.x*1.7+p.y*.41)*.5+.5;
                half3 col=half3(.05,.16,.235)+half3(.065,.15,.21)*ripple;
                float2 q=i.local;
                float distort=sin(q.y*18+_Time.y*.8)*.17+sin(q.y*35-_Time.y*.5)*.06;
                float lamps=0;
                // Broken warm reflections below the lamps along the far quay.
                [unroll] for(int j=0;j<12;j++)
                {
                    float x=-31+j*5.8;
                    float dx=q.x-x+distort;
                    lamps+=exp(-dx*dx*5)*saturate(1-abs(q.y-14)/8);
                }
                lamps+=exp(-pow((q.y-3.8+distort)*1.6,2))*saturate(1-abs(q.x-11)/8);
                float bands=pow(saturate(sin(q.y*30+_Time.y*.7)*.5+.5),2);
                col+=half3(2.9,1.36,.23)*lamps*bands;
                return half4(col,1);
            }
            ENDHLSL
        }
    }
}
