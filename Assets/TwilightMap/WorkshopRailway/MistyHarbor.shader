Shader "BUCA/Workshop Misty Harbor"
{
    Properties
    {
        [PerRendererData] _MainTex("Artwork",2D)="white"{}
        _FlowTime("Water and mist time",Float)=0
        _WaterData("Water coverage, shore distance, falls, streams",2D)="black"{}
        _WaterMotion("Natural water strength",Range(0,2))=1
        _WaterPhaseOffset("Water preview time offset",Float)=0
        _EveningStrength("World 2 evening",Range(0,1))=1
        _MistDensity("Sea mist",Range(0,1))=.8
        _WorldReveal("World 2 reveal",Range(0,1))=0
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
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _FlowTime, _EveningStrength, _MistDensity, _WorldReveal;
            #include "WorkshopNaturalWater.cginc"
            struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct Output {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            Output vert(Input v){Output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p)
            {
                float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(cell),hash(cell+float2(1,0)),f.x),
                    lerp(hash(cell+float2(0,1)),hash(cell+1),f.x),f.y);
            }
            float ellipse(float2 p,float2 center,float2 radius)
            {float2 q=(p-center)/radius;return exp(-dot(q,q)*2);}
            float box(float2 p,float4 bounds,float feather)
            {
                return smoothstep(bounds.x-feather,bounds.x,p.x)*(1-smoothstep(bounds.z,bounds.z+feather,p.x))
                    *smoothstep(bounds.y-feather,bounds.y,p.y)*(1-smoothstep(bounds.w,bounds.w+feather,p.y));
            }
            float lamp(float2 p,float2 center,float size)
            {return ellipse(p,center,float2(size,size));}
            float reflection(float2 p,float2 source,float reach,float time)
            {
                float2 q=p-source;
                float width=6+max(0,q.y)*.15;
                float streak=exp(-pow((q.x+sin(q.y*.12-time)*4)/width,2));
                float ripples=pow(saturate(sin(q.y*.48+sin(q.x*.17+time)*1.8-time*1.3)),5);
                return streak*smoothstep(0,12,q.y)*(1-smoothstep(reach*.55,reach,q.y))*ripples;
            }
            fixed4 frag(Output i):SV_Target
            {
                float2 p=i.uv*float2(1672,941);p.y=941-p.y;
                fixed4 original=tex2D(_MainTex,i.uv);
                float t=_FlowTime;
                float waterMask;
                fixed4 color=AnimateWorkshopWater(i.uv,p,t+_WaterPhaseOffset,waterMask);

                // Local lighting only: no filter over World 1, BUCA, the wooden signs or controls.
                float rightEdge=1510+max(0,p.y-185)*.28;
                float evening=smoothstep(920,1040,p.x)*smoothstep(178,218,p.y)
                    *(1-smoothstep(rightEdge-12,rightEdge+12,p.x))*(1-smoothstep(736,758,p.y));
                evening*=1-box(p,float4(1224,174,1395,233),7);
                evening*=_EveningStrength;
                float clear=smoothstep(saturate((p.x-925)/750)*.65,
                    saturate((p.x-925)/750)*.65+.3,_WorldReveal);
                evening*=1-clear;
                float3 dusk=color.rgb*float3(.50,.59,.82)+float3(.009,.012,.028);
                // Preserve the warmth of the station windows and lantern bulbs.
                float warm=smoothstep(.08,.35,original.r-original.b)*smoothstep(.3,.7,original.g);
                float windows=box(p,float4(1150,245,1345,314),5);
                float lamps=lamp(p,float2(1090,246),18)+lamp(p,float2(1368,294),16)
                    +lamp(p,float2(1072,522),20)+lamp(p,float2(1280,607),18)
                    +lamp(p,float2(1153,563),16)+lamp(p,float2(1510,235),25);
                dusk=lerp(dusk,color.rgb*float3(1.12,1.02,.80),warm*saturate(windows+lamps));
                dusk+=saturate(lamps)*float3(.22,.10,.016);
                float reflected=reflection(p,float2(1400,445),140,t)
                    +reflection(p,float2(1290,530),90,t)+reflection(p,float2(1180,658),95,t)
                    +reflection(p,float2(1062,570),135,t);
                dusk+=reflected*waterMask*float3(.52,.28,.045);
                color.rgb=lerp(color.rgb,dusk,evening);

                // Slow, layered sea mist, concentrated behind the station and above open water.
                // It is part of the background pass, below every interactive level marker and the train.
                float n=noise(float2(p.x*.010-t*.055,p.y*.025+t*.018));
                n=n*.65+noise(float2(p.x*.027-t*.08,p.y*.045-t*.024))*.35;
                float2 q=p+float2(0,sin(p.x*.014+t*.19)*11+(n-.5)*30);
                float opening=saturate(_WorldReveal);
                float spread=1+opening*.45;
                float2 drift=float2(opening*90,-opening*55);
                float distant=ellipse(q,float2(1110,230)+drift*float2(-1,1),float2(230,46)*spread)
                    +ellipse(q,float2(1445,260)+drift,float2(130,40)*spread)
                    +ellipse(q,float2(1000,324)+drift*float2(-.7,.6),float2(110,33)*spread);
                distant*=1-box(p,float4(1138,247,1414,345),12);
                distant*=1-ellipse(p,float2(1510,267),float2(33,100));
                float sea=ellipse(q,float2(1050,414)-drift,float2(150,31)*spread)
                    +ellipse(q,float2(1435,456)+drift,float2(175,36)*spread)
                    +ellipse(q,float2(1190,705)+drift*float2(.5,-.6),float2(280,43)*spread)
                    +ellipse(q,float2(1585,641)+drift,float2(90,75)*spread);
                float veil=ellipse(q,float2(1300,440)+drift*.35,float2(350,245)*spread)*.25;
                float fog=saturate(distant*1.15+sea*waterMask*1.25+veil)*(.35+.65*smoothstep(.18,.72,n))
                    *evening*_MistDensity;
                color.rgb=lerp(color.rgb,float3(.64,.70,.86),saturate(fog));
                // Light the existing station, lighthouse and quay in sequence.
                float glow=lamp(p,float2(1090,246),25)*smoothstep(.12,.24,_WorldReveal)
                    +ellipse(p,float2(1245,275),float2(90,35))*smoothstep(.24,.37,_WorldReveal)
                    +lamp(p,float2(1368,294),24)*smoothstep(.37,.49,_WorldReveal)
                    +lamp(p,float2(1510,235),32)*smoothstep(.49,.61,_WorldReveal)
                    +lamp(p,float2(1280,607),26)*smoothstep(.61,.73,_WorldReveal)
                    +lamp(p,float2(1153,563),25)*smoothstep(.73,.85,_WorldReveal)
                    +lamp(p,float2(1072,522),27)*smoothstep(.85,.97,_WorldReveal);
                color.rgb+=saturate(glow)*float3(.24,.13,.025);
                // A restrained golden sweep leads from the bridge into the harbor.
                float sweep=exp(-pow((p.x-(930+_WorldReveal*770))/22,2));
                float route=ellipse(p,float2(1090,340),float2(145,27))
                    +ellipse(p,float2(1360,390),float2(180,32));
                color.rgb+=sweep*route*sin(saturate(_WorldReveal)*3.14159)*float3(.48,.27,.045);
                return color*i.color;
            }
            ENDCG
        }
    }
}
