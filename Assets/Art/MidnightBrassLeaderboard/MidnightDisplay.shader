Shader "Buca/Midnight Display"
{
    Properties
    {
        _BaseColor ("Finish", Color) = (.04,.065,.11,1)
        _Metallic ("Metal", Range(0,1)) = 0
        _Smoothness ("Polish", Range(0,1)) = .4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Metallic, _Smoothness;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 positionWS:TEXCOORD1; float3 local:TEXCOORD2; };
            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(input.normalOS);
                o.local=input.positionOS.xyz;
                return o;
            }
            half4 frag(Varyings input):SV_Target
            {
                // Fixed studio lighting keeps these off-screen display meshes independent
                // of gameplay lights. Curved bevel normals still drive real surface shading.
                float3 n=normalize(input.normalWS);
                float3 v=normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 key=normalize(float3(-.58,.72,-.70));
                float3 fill=normalize(float3(.65,.15,-.65));
                float diffuse=.20+.75*saturate(dot(n,key))+.18*saturate(dot(n,fill));
                float faceGradient=.92+.08*tanh(input.local.y*.55)-.025*tanh(input.local.x*.45);
                float spec=pow(saturate(dot(n,normalize(key+v))),lerp(22,90,_Smoothness));
                float fillSpec=pow(saturate(dot(n,normalize(fill+v))),40)*.24;
                float3 specular=lerp(float3(.035,.04,.05),_BaseColor.rgb*.85,_Metallic)*(spec+fillSpec);
                float rim=pow(1-saturate(dot(n,v)),3)*.09;
                float3 color=_BaseColor.rgb*diffuse*faceGradient+specular+float3(.20,.28,.40)*rim;
                return half4(color*1.30,1);
            }
            ENDHLSL
        }
    }
}
