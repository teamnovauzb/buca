Shader "BUCA/Honey Amber"
{
 Properties { _BaseColor("Amber",Color)=(1,0.48,0.045,0.72) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 color:COLOR;};
   struct V {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;float radial:TEXCOORD2;};
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor;
   CBUFFER_END
   V vert(A v){V o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.radial=v.color.r;return o;}
   half4 frag(V i):SV_Target
   {
    float3 n=normalize(i.normalWS),v=normalize(GetWorldSpaceViewDir(i.positionWS));
    float rim=smoothstep(.91,.995,i.radial);
    float3 amber=lerp(float3(.91,.39,.035),float3(.65,.25,.018),rim*.5);
    float3 h=normalize(normalize(float3(-.5,.65,-1))+v);
    float shine=pow(saturate(dot(n,h)),180)*1.2;
    float3 h2=normalize(normalize(float3(.8,-.3,-1))+v);
    shine+=pow(saturate(dot(n,h2)),110)*.65;
    float fresnel=pow(1-saturate(abs(dot(n,v))),4);
    float3 col=amber+float3(1,.94,.69)*(shine+fresnel*.25);
    float alpha=lerp(.60,.85,rim);
    return half4(col,alpha);
   }
   ENDHLSL
  }
 }
}
