Shader "BUCA/Lighthouse Beacon"
{
    Properties
    {
        // RawImage queries this property even though the beam is generated procedurally.
        [PerRendererData] _MainTex("UI Texture", 2D)="white" {}
        _FlowTime("Rotation time",Float)=0
        _Period("Seconds per full rotation",Float)=8
        _Intensity("Beam strength",Float)=1
        _BeamSize("Overlay size",Vector)=(500,200,0,0)
        _Range("Beam reach",Float)=240
        _Projection("Horizontal plane projection",Float)=.34
        _ArtOrigin("Origin on artwork",Vector)=(1604,496,0,0)
        _ClipToWorkshop("Keep light inside workshop landscape",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _FlowTime, _Period, _Intensity, _Range, _Projection, _ClipToWorkshop;
            float4 _BeamSize, _ArtOrigin;
            struct V {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            V vert(appdata_img v){V o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
            fixed4 frag(V i):SV_Target
            {
                float2 p=(i.uv-.5)*_BeamSize.xy;
                // Project a horizontal rotating searchlight into the map's elevated camera view.
                float angle=_FlowTime*6.2831853/max(.1,_Period);
                float2 plane=float2(p.x,p.y/max(.1,_Projection));
                float2 forward=float2(cos(angle),sin(angle));
                float along=dot(plane,forward);
                float across=abs(dot(plane,float2(-forward.y,forward.x)));
                float width=6+max(0,along)*.29;
                float cone=pow(saturate(1-across/width),1.65);
                float range=smoothstep(3,15,along)*(1-smoothstep(_Range*.45,_Range,along));
                float visibility=lerp(1,.55,smoothstep(-.35,.6,sin(angle)));
                // Reduce scattering over the tower while keeping a continuous visible sweep through the mist.
                float towerWidth=lerp(20,32,saturate(-p.y/105));
                float tower=(1-smoothstep(towerWidth,towerWidth+5,abs(p.x)))
                    *(1-smoothstep(-17,-12,p.y))*smoothstep(-112,-103,p.y);
                float glow=exp(-dot(p,p)/65)*(.12+.10*pow(saturate(-sin(angle)),5));
                float cap=(1-smoothstep(18,25,abs(p.x)))*smoothstep(13,19,p.y)*(1-smoothstep(35,42,p.y));
                float alpha=(cone*range*visibility*(1-tower*.62)*(1-cap)+glow)*_Intensity;
                float2 art=_ArtOrigin.xy+float2(p.x,-p.y);
                float clip=smoothstep(180,205,art.y)*(1-smoothstep(735,758,art.y))
                    *smoothstep(0,8,art.x)*(1-smoothstep(1664,1672,art.x));
                alpha*=lerp(1,clip,_ClipToWorkshop);
                return fixed4(1,.79,.42,saturate(alpha));
            }
            ENDCG
        }
    }
}
