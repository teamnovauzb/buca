Shader "BUCA/Grounded Birch"
{
    Properties
    {
        _WindTime("Wind time", Float)=0
        _WindPhase("Tree phase", Float)=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Pass
        {
            Tags {"LightMode"="SRPDefaultUnlit"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _WindTime, _WindPhase;
            CBUFFER_END
            struct A {float4 vertex:POSITION; float3 normal:NORMAL; float4 color:COLOR; float2 uv:TEXCOORD0; float2 kind:TEXCOORD1;};
            struct V {float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; float4 color:COLOR; float2 uv:TEXCOORD2; float2 kind:TEXCOORD3; float height:TEXCOORD4;};
            V vert(A a)
            {
                V o;
                float h=max(0,a.vertex.y-.25);
                float wind=(.45+.35*sin(_WindTime*.5+_WindPhase)+.14*sin(_WindTime*1.1+_WindPhase*.7));
                // The root collar and grass stay completely fixed. Every branch uses
                // the same height-based curve so its junction never separates.
                a.vertex.x+=wind*h*h*.009;
                a.vertex.z+=sin(_WindTime*.43+_WindPhase)*h*h*.003;
                o.world=TransformObjectToWorld(a.vertex.xyz);o.pos=TransformWorldToHClip(o.world);
                o.normal=TransformObjectToWorldNormal(normalize(a.normal+float3(0,-a.normal.x*wind*h*.018,0)));
                o.color=a.color;o.uv=a.uv;o.kind=a.kind;o.height=a.vertex.y;return o;
            }
            half4 frag(V i):SV_Target
            {
                clip(i.height+.008);
                float3 n=normalize(i.normal), view=normalize(GetWorldSpaceViewDir(i.world));
                float3 key=normalize(float3(-.6,.95,-.75));
                half3 base=i.color.rgb;
                if(i.kind.x<.5)
                {
                    float row=floor(i.uv.y*4.2);
                    float seed=frac(sin(row*37.17+8)*15731.43);
                    float horizontal=abs(frac(i.uv.x*2+seed)-.5);
                    float stripe=1-smoothstep(.035,.09,abs(frac(i.uv.y*4.2)-.48));
                    float broken=1-smoothstep(.16,.28,horizontal);
                    base=lerp(base,half3(.16,.18,.13),stripe*broken*.80*saturate(i.height*2));
                }
                half hemi=saturate(n.y*.5+.5), lit=saturate(dot(n,key));
                half fill=saturate(dot(n,normalize(float3(.8,.4,.5))));
                half ao=lerp(.80,1,saturate(i.height/3.8));
                half3 light=half3(.38,.40,.29)+hemi*.17+lit*half3(.60,.56,.39)+fill*half3(.10,.15,.14);
                half3 result=base*light*ao;
                half spec=pow(saturate(dot(n,normalize(key+view))),28)*.055;
                result+=half3(.95,.96,.65)*spec;
                return half4(result,1);
            }
            ENDHLSL
        }
    }
}
