Shader "BUCA/Harbor Boat Surface"
{
    Properties
    {
        [PerRendererData] _MainTex("Boat",2D)="white"{}
        _Waterline("Hull waterline",2D)="black"{}
        _UVRect("Visible bounds",Vector)=(0,0,1,1)
        _FlowTime("Water time",Float)=0
        _Reflection("Reflection",Float)=0
        _Tint("Harbor light",Color)=(.83,.9,1,1)
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
            sampler2D _MainTex,_Waterline;
            float4 _UVRect,_Tint;
            float _FlowTime,_Reflection;
            struct Input{float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct V{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            V vert(Input i){V o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.uv;o.color=i.color;return o;}
            fixed4 frag(V i):SV_Target
            {
                float2 local=(i.uv-_UVRect.xy)/_UVRect.zw;
                float t=_FlowTime;
                float2 uv=i.uv;
                if(_Reflection>.5)
                {
                    uv.x+=sin(local.y*65-t*3+local.x*12)*.012;
                    fixed4 reflection=tex2D(_MainTex,uv)*i.color;
                    reflection.rgb=lerp(reflection.rgb,float3(.04,.28,.43),.48);
                    reflection.a*=.2*pow(saturate(1-local.y),2)*( .62+.38*sin(local.y*95+t*3));
                    return reflection;
                }
                float2 hullEdge=tex2D(_Waterline,float2(uv.x,.5)).rg;
                float height=(uv.y-hullEdge.r)/_UVRect.w;
                float wave=.0035*sin(local.x*39-t*3.3)+.002*sin(local.x*67+t*2.4);
                float depth=height-.025-wave;
                fixed4 col=tex2D(_MainTex,uv)*i.color;
                col.rgb*=_Tint.rgb;
                // The last few hull pixels disappear into blue water, with a broken contact crest.
                float wet=1-smoothstep(.005,.075,depth);
                col.rgb=lerp(col.rgb,float3(.055,.31,.46),wet*.62*hullEdge.g);
                float crest=exp(-pow((depth-.014)*160,2));
                float broken=.45+.55*pow(saturate(sin(local.x*73+t*1.8)),2);
                col.rgb=lerp(col.rgb,float3(.55,.8,.88),crest*broken*.72*hullEdge.g);
                col.a*=lerp(1,smoothstep(-.008,.012,depth),hullEdge.g);
                return col;
            }
            ENDCG
        }
    }
}
