Shader "BUCA/Forest Harbor Water Flow"
{
    Properties
    {
        [PerRendererData] _MainTex("Artwork", 2D) = "white" {}
        _FlowMask("Water regions (fall, current, foam, stream)", 2D) = "black" {}
        _ShoreWaveData("Shore waves (coverage, distance, normal)", 2D) = "black" {}
        _ForestPlate("Birch clearing", 2D) = "black" {}
        _ForestReplaceMask("Forest replacement coverage", 2D) = "black" {}
        _UseForestPlate("Use birch clearing", Float) = 0
        _WithoutMenu("Menu removal artwork", 2D) = "black" {}
        _HideMapMenu("Hide map menu", Float) = 0
        _FlowTime("Flow time", Float) = 0
        _RepairOnly("Boat removal patches only", Float) = 0
        _Color("Tint", Color) = (1,1,1,1)
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False"}
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            sampler2D _MainTex, _FlowMask, _ShoreWaveData;
            float4 _MainTex_TexelSize, _Color, _ClipRect, _TextureSampleAdd;
            float _FlowTime, _RepairOnly, _UseForestPlate, _HideMapMenu;
            sampler2D _ForestPlate,_ForestReplaceMask,_WithoutMenu;
            fixed4 artworkSample(float2 uv)
            {
                fixed4 original=tex2D(_MainTex,uv);
                float4 water=tex2D(_FlowMask,uv);
                float coverage=tex2D(_ForestReplaceMask,uv).r*_UseForestPlate*(1-saturate(max(max(water.r,water.g),max(water.b,water.a))*3));
                fixed4 result=lerp(original,tex2D(_ForestPlate,uv),coverage);
                float2 menuDelta=abs(float2(uv.x,1-uv.y)*float2(1672,941)-float2(288,866))-float2(57,65);
                float menuCoverage=(1-smoothstep(-3,4,max(menuDelta.x,menuDelta.y)))*_HideMapMenu;
                return lerp(result,tex2D(_WithoutMenu,uv),menuCoverage);
            }
            float patch(float2 p,float2 centre,float2 halfSize)
            {
                float2 q=abs(p-centre)-halfSize;
                return 1-smoothstep(-6,3,max(q.x,q.y));
            }
            // Local canopy bending: each crown has a fixed base and a different gust phase.
            float crownWind(float2 p,float2 centre,float2 radius,float t)
            {
                float2 q=(p-centre)/radius;
                float mask=1-smoothstep(.55,1,dot(q,q));
                float height=saturate((1-q.y)*.5);
                float gust=.7+.3*sin(t*.43-centre.x*.009);
                return mask*height*gust*(sin(t*1.35-centre.x*.018)+.28*sin(t*2.7+centre.y*.04));
            }
            float forestWind(float2 p,float t)
            {
                float w=0;
                w+=crownWind(p,float2(88,355),float2(30,45),t);
                w+=crownWind(p,float2(198,332),float2(39,44),t);
                w+=crownWind(p,float2(280,353),float2(24,52),t);
                w+=crownWind(p,float2(370,343),float2(39,47),t);
                w+=crownWind(p,float2(431,365),float2(25,40),t);
                w+=crownWind(p,float2(489,372),float2(27,34),t);
                w+=crownWind(p,float2(581,428),float2(32,40),t);
                w+=crownWind(p,float2(555,383),float2(22,37),t);
                w+=crownWind(p,float2(655,438),float2(31,38),t);
                w+=crownWind(p,float2(419,409),float2(31,34),t);
                w+=crownWind(p,float2(456,436),float2(24,29),t);
                w+=crownWind(p,float2(318,405),float2(22,37),t);
                w+=crownWind(p,float2(355,438),float2(18,32),t);
                w+=crownWind(p,float2(145,205),float2(32,36),t);
                w+=crownWind(p,float2(208,219),float2(28,38),t);
                w+=crownWind(p,float2(241,174),float2(25,30),t);
                w+=crownWind(p,float2(102,76),float2(30,35),t);
                w+=crownWind(p,float2(152,55),float2(26,35),t);
                w+=crownWind(p,float2(265,72),float2(25,32),t);
                w+=crownWind(p,float2(382,128),float2(24,40),t);
                w+=crownWind(p,float2(301,562),float2(33,39),t);
                w+=crownWind(p,float2(251,565),float2(22,32),t);
                w+=crownWind(p,float2(415,576),float2(28,38),t);
                w+=crownWind(p,float2(585,576),float2(28,42),t);
                w+=crownWind(p,float2(676,560),float2(24,39),t);
                w+=crownWind(p,float2(60,675),float2(30,40),t);
                w+=crownWind(p,float2(112,711),float2(30,37),t);
                w+=crownWind(p,float2(326,751),float2(31,39),t);
                w+=crownWind(p,float2(174,732),float2(25,35),t);
                return clamp(w*3.2,-3.8,3.8);
            }
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Varying { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; float4 local:TEXCOORD1; };
            Varying vert(Input v)
            {
                Varying o; o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            fixed4 frag(Varying i):SV_Target
            {
                float4 region=tex2D(_FlowMask,i.uv);
                float2 p=float2(i.uv.x,1-i.uv.y)*float2(1672,941);
                float t=_FlowTime;
                // Stronger swells in the foreground harbor, fading smoothly into the river.
                float harbor=smoothstep(1140,1270,p.x)*smoothstep(650,740,p.y);
                float harborEnergy=1+harbor*.85;
                float fall=region.r, current=region.g, foam=region.b;
                float stream=region.a;
                float4 shore=tex2D(_ShoreWaveData,i.uv);
                float shoreDistance=shore.g*96;
                float2 shoreNormal=shore.ba*2-1;
                float shorePhase=shoreDistance*.36+t*2.3+sin(p.x*.055)*.7;
                float shoreSwell=sin(shorePhase);
                // A phase travelling toward increasing artwork Y always moves DOWN the falls.
                float travel=p.y*.24-t*7.5;
                float ribbon=sin(p.x*.62+sin(p.y*.07-t*1.8)*.65);
                float surge=sin(travel+sin(p.x*.17)*2.2);
                float2 offset=float2(sin(p.y*.13-t*3.4)*1.05, surge*2.8)*fall;
                offset+=float2(sin(p.y*.3+t*2.2)*1.8,sin(p.x*.15-p.y*.18+t*3)*.85)*current*harborEnergy;
                offset+=float2(sin(p.y*.095-t*1.65+p.x*.025)*1.8,sin(p.x*.075+p.y*.065-t*1.45)*1.1)*current*harbor;
                offset+=float2(sin(p.x*.31+t*3),cos(p.y*.4-t*4))*.9*foam;
                // Uneven wavelets travel downstream to the right and toward the foreground.
                float bridgePool=smoothstep(605,623,p.y)*(1-smoothstep(664,685,p.y));
                float2 streamDirection=normalize(lerp(float2(.94,.34),float2(1,-.08),bridgePool));
                float2 streamSide=float2(-streamDirection.y,streamDirection.x);
                float along=dot(p,streamDirection);
                float across=dot(p,streamSide);
                float streamPhase=along*.23-t*5.1+sin(across*.21)*.7;
                float wave=sin(streamPhase);
                float detail=sin(along*.41-t*6.1+sin(across*.37));
                offset+=streamDirection*(wave*3.6+detail*.95)*stream;
                offset+=streamSide*sin(across*.28+along*.09-t*2.3)*1.05*stream;
                // Positive time moves these crests toward decreasing distance to shore.
                offset+=shoreNormal*(shoreSwell*2.8+sin(shorePhase*.53+t*.31)*.65)*shore.r*harborEnergy;
                offset+=float2(shoreNormal.y,-shoreNormal.x)*sin(p.x*.13+t*1.5)*.65*shore.r;
                // Protect non-foliage detail even where a crown bounds a nearby landmark.
                float3 original=artworkSample(i.uv).rgb;
                float foliage=smoothstep(.015,.07,original.g-original.r*.86)
                    *smoothstep(.02,.09,original.g-original.b)*step(p.x,800);
                offset.x+=forestWind(p,t)*foliage*(1-_UseForestPlate);
                float2 uv=i.uv+float2(offset.x,-offset.y)*_MainTex_TexelSize.xy;
                fixed4 col=artworkSample(uv)+_TextureSampleAdd;
                if(stream>.001)
                {
                    // Two overlapping phases carry painted detail downstream without a reset.
                    float phase0=frac(t*.32),phase1=frac(t*.32+.5);
                    float weight0=1-abs(phase0*2-1);
                    float2 stepUV=float2(streamDirection.x,-streamDirection.y)*_MainTex_TexelSize.xy*26;
                    float2 uv0=uv-stepUV*phase0,uv1=uv-stepUV*phase1;
                    float valid0=smoothstep(.08,.65,tex2D(_FlowMask,uv0).a)*weight0;
                    float valid1=smoothstep(.08,.65,tex2D(_FlowMask,uv1).a)*(1-weight0);
                    float total=valid0+valid1;
                    fixed4 drifting=(artworkSample(uv0)*valid0+artworkSample(uv1)*valid1)/max(total,.0001)+_TextureSampleAdd;
                    // Reject samples outside water so the bridge and rocks never drag into it.
                    col=lerp(col,drifting,saturate(total)*stream*.72);
                }
                // Thin highlights and uneven pulses travel continuously, without a loop seam.
                float threads=pow(saturate(ribbon*.5+.5),7);
                float glint=pow(saturate(surge*.5+.5),4)*threads;
                col.rgb*=1+fall*(surge*.055+sin(p.y*.44-t*12+p.x*.09)*.025);
                col.rgb+=float3(.065,.095,.11)*glint*fall;
                float ripple=sin(p.x*.13+p.y*.42-t*3.2)+sin(p.x*.24-p.y*.3+t*2.1);
                col.rgb+=float3(.025,.045,.055)*ripple*current*(1+harbor*.4);
                // Warm reflected lamps shimmer with the ripples instead of glowing uniformly.
                float reflection=saturate((col.r-col.b*.75)*3);
                col.rgb*=1+reflection*current*sin(p.y*.47+t*2.6+sin(p.x*.12))*.14*harborEnergy;
                float bubbles=sin(p.x*.4+t*4.1)*sin(p.y*.53-t*5.3);
                col.rgb+=float3(.045,.055,.06)*bubbles*foam;
                float crest=pow(saturate(wave*.5+.5),7)*(.55+.45*sin(across*.32+along*.07));
                col.rgb+=float3(.035,.055,.065)*(wave*.5+detail*.22)*stream;
                col.rgb+=float3(.085,.11,.12)*crest*stream;
                // Sparse short foam flecks drift with the current at different spacings.
                float2 foamGrid=float2((along-t*12)*.077,across*.19);
                float2 foamCell=floor(foamGrid);
                float seed=frac(sin(dot(foamCell,float2(127.1,311.7)))*43758.5453);
                float2 foamPoint=frac(foamGrid)-float2(.25+seed*.5,.3+frac(seed*7.3)*.4);
                float fleck=exp(-dot(foamPoint*float2(8,17),foamPoint*float2(8,17)));
                fleck*=smoothstep(.58,.76,seed)*(.65+.35*sin(t*1.4+seed*18));
                col.rgb=lerp(col.rgb,float3(.78,.94,1),fleck*stream*.38);
                float shoreCrest=pow(saturate(shoreSwell*.5+.5),12);
                float foamBreaks=smoothstep(.18,.8,sin(p.x*.17+sin(p.x*.047)*2+t*.28)*.5+.5);
                float wash=(1-smoothstep(7,27,shoreDistance))*shore.r;
                col.rgb+=float3(.018,.035,.045)*shoreSwell*shore.r;
                col.rgb=lerp(col.rgb,float3(.72,.91,1),shoreCrest*foamBreaks*wash*(.32+harbor*.16));
                col.rgb*=1+reflection*shore.r*sin(shorePhase+p.x*.08)*.09*harborEnergy;
                // The painted lighthouse flare must turn with the live beacon, rather than
                // remaining permanently lit to the right beneath the rotating overlay.
                float2 lampDelta=p-float2(1604,496);
                float flareArea=smoothstep(18,27,abs(lampDelta.x))*(1-smoothstep(58,80,abs(lampDelta.x)))
                    *(1-smoothstep(13,27,abs(lampDelta.y)));
                float beaconAngle=_FlowTime*6.2831853/8;
                float facing=pow(saturate(cos(beaconAngle)*sign(lampDelta.x)),12);
                float paintedGold=saturate((col.r-col.b*.8)*4);
                float coolLuminance=max(col.b,col.g*.38);
                float3 unlitWater=float3(coolLuminance*.16,coolLuminance*.49,coolLuminance);
                col.rgb=lerp(col.rgb,unlitWater,flareArea*paintedGold*(1-facing)*.95);
                col*=i.color;
                if(_RepairOnly>2.5)
                    col.a*=patch(p,float2(1408,481),float2(48,77));
                else if(_RepairOnly>1.5)
                    col.a*=patch(p,float2(1383,861),float2(78,69));
                else if(_RepairOnly>.5)
                    col.a*=max(patch(p,float2(1067,637),float2(60,88)),patch(p,float2(1569,793),float2(106,82)));
                #ifdef UNITY_UI_CLIP_RECT
                col.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a-.001);
                #endif
                return col;
            }
            ENDCG
        }
    }
}
