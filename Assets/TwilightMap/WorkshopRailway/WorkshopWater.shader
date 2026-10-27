Shader "BUCA/Workshop Water"
{
    Properties
    {
        [PerRendererData] _MainTex("Artwork",2D)="white"{}
        _FlowTime("Water time",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _FlowTime;
            struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct Output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            Output vert(Input v){Output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            float WaterBlue(float3 c)
            {
                return smoothstep(.055,.14,c.b-c.r)*smoothstep(.04,.11,c.g-c.r);
            }
            float LandAt(float2 uv)
            {
                float3 c=tex2D(_MainTex,uv).rgb;
                return smoothstep(.015,.10,c.r-c.b);
            }
            fixed4 frag(Output i):SV_Target
            {
                float2 p=i.uv*float2(1672,941);p.y=941-p.y;
                fixed4 original=tex2D(_MainTex,i.uv);
                // Only blue water inside the miniature landscape, excluding the table rim and station.
                float landscape=smoothstep(209,220,p.y)*(1-smoothstep(748,758,p.y));
                float river=smoothstep(814,832,p.x)*(1-smoothstep(1040,1060,p.x));
                float harbor=smoothstep(1010,1040,p.x)*smoothstep(386,398,p.y)*(1-smoothstep(1630,1650,p.x));
                float stream=smoothstep(310,330,p.x)*(1-smoothstep(580,600,p.x))*smoothstep(475,490,p.y)*(1-smoothstep(600,615,p.y));
                float blue=WaterBlue(original.rgb);
                float mask=landscape*saturate(river+harbor+stream)*blue;
                float t=_FlowTime;
                float swell=sin(p.x*.035+p.y*.055-t*1.8);
                float ripple=sin(p.x*.082-p.y*.11+t*2.2);
                float2 drift=float2(swell+sin(p.y*.09+t*1.3)*.45,ripple*.65);
                float2 uv=i.uv+drift*float2(3.2/1672,2.0/941)*mask;
                fixed4 water=tex2D(_MainTex,uv);
                // Two cross-faded scrolling samples carry the current downstream without a reset jump.
                float phase=frac(t*.16);
                float phaseB=frac(t*.16+.5);
                float2 current=float2(5,-15)/float2(1672,941);
                fixed4 flowA=tex2D(_MainTex,uv-current*phase);
                fixed4 flowB=tex2D(_MainTex,uv-current*phaseB);
                float blend=abs(phase*2-1);
                fixed4 flow=lerp(flowA,flowB,blend);
                float safeFlow=WaterBlue(flowA.rgb)*WaterBlue(flowB.rgb);
                water.rgb=lerp(water.rgb,flow.rgb,mask*safeFlow*.65);
                water.rgb*=1+mask*(.065*swell+.025*ripple);
                float crest=pow(saturate(sin(p.y*.15+p.x*.045-t*2.4)),14);
                float broken=smoothstep(.1,.8,sin(p.x*.08+sin(p.y*.05)+t*.4));
                water.rgb+=mask*crest*broken*float3(.045,.09,.11);
                // Locate the actual rock/dock boundary in the artwork, then lap foam against it.
                float2 texel=1/float2(1672,941);
                float nearLand=0,farLand=0;
                [unroll] for(int n=0;n<8;n++)
                {
                    float angle=n*.785398;
                    float2 direction=float2(cos(angle),sin(angle))*texel;
                    nearLand=max(nearLand,LandAt(i.uv+direction*4));
                    farLand=max(farLand,LandAt(i.uv+direction*11));
                }
                float shore=mask*saturate(farLand);
                float approach=sin(t*2.1-p.x*.025-p.y*.018);
                float foamBand=lerp(farLand,nearLand,smoothstep(-.6,.7,approach));
                float lace=.55+.45*sin(p.x*.32+p.y*.23+t*1.7);
                float foam=shore*foamBand*(.15+.32*smoothstep(-.5,.8,approach))*lace;
                water.rgb=lerp(water.rgb,float3(.81,.96,1),foam);
                return water*i.color;
            }
            ENDCG
        }
    }
}
