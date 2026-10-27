Shader "BUCA/Premium Passenger" {
 Properties { _MainTex("Artwork",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
 Pass {
 Tags { "LightMode"="UniversalForward" }
 Cull Off ZWrite On Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); float4 _Color;
 struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=i.color*_Color;return o;}
 half4 frag(V i):SV_Target{half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;clip(c.a-.08);return c;}
 ENDHLSL
 }
 }
}
