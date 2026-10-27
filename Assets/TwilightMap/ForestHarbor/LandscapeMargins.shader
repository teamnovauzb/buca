Shader "BUCA/Landscape Margins"
{
    Properties { [PerRendererData] _MainTex("Texture",2D)="white"{} }
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
            struct V {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            V vert(appdata_img i){V o;o.vertex=UnityObjectToClipPos(i.vertex);o.uv=i.texcoord;return o;}
            fixed4 frag(V i):SV_Target
            {
                float blend=smoothstep(.2,.8,i.uv.x);
                float3 forest=float3(.16,.24,.13),harbor=float3(.035,.10,.19);
                float3 color=lerp(forest,harbor,blend);
                color*=.7+.3*sin(i.uv.y*3.14159);
                return fixed4(color,1);
            }
            ENDCG
        }
    }
}
