Shader "BUCA/Harbor Water Repair"
{
    Properties { [PerRendererData] _MainTex("Clean water plate",2D)="white"{} }
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
            struct V {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            V vert(appdata_img i){V o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.texcoord;return o;}
            float patch(float2 p,float2 centre,float2 halfSize)
            {
                float2 q=abs(p-centre)-halfSize;
                return 1-smoothstep(-6,3,max(q.x,q.y));
            }
            fixed4 frag(V i):SV_Target
            {
                float2 pixel=float2(i.uv.x*1672,(1-i.uv.y)*941);
                float alpha=max(patch(pixel,float2(1067,637),float2(60,88)),patch(pixel,float2(1569,793),float2(106,82)));
                return fixed4(tex2D(_MainTex,i.uv).rgb,alpha);
            }
            ENDCG
        }
    }
}
