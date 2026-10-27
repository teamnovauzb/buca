Shader "BUCA/Station Toy"
{
    Properties
    {
        _BaseColor("Color", Color)=(1,1,1,1)
        _BaseMap("Grain", 2D)="white" {}
        _Metallic("Metal",Range(0,1))=0
        _Smoothness("Lacquer",Range(0,1))=.45
        [HDR] _EmissionColor("Glow",Color)=(0,0,0,0)
        _GrainStrength("Grain strength",Range(0,1))=1
        _TextureTint("Grain color",Range(0,1))=1
        _CityAtmosphere("Evening atmosphere",Range(0,1))=0
        _BakedLight("Use saved lighting",Float)=0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags {"LightMode"="SRPDefaultUnlit"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor, _EmissionColor;
            float4 _BaseMap_ST;
            half _Metallic, _Smoothness, _GrainStrength, _TextureTint, _CityAtmosphere, _BakedLight;
            CBUFFER_END
            struct A {float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float3 light:TEXCOORD2;};
            struct V {float4 pos:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; float2 uv:TEXCOORD2; half3 light:TEXCOORD3;};
            V vert(A a)
            {
                V o; o.world=TransformObjectToWorld(a.vertex.xyz); o.pos=TransformWorldToHClip(o.world);
                o.normal=TransformObjectToWorldNormal(a.normal);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);o.light=_BakedLight>.5?a.light:half3(0,0,0);return o;
            }
            half4 frag(V i):SV_Target
            {
                float3 n=normalize(i.normal), v=normalize(GetWorldSpaceViewDir(i.world));
                float3 key=normalize(float3(-.6,.95,-.75));
                float3 fill=normalize(float3(.9,.4,.45));
                half3 grainSample=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
                half luminance=dot(grainSample,half3(.2126,.7152,.0722));
                half3 base=lerp(half3(1,1,1),lerp(luminance.xxx,grainSample,_TextureTint),_GrainStrength)*_BaseColor.rgb;
                half keyLight=saturate(dot(n,key));
                half fillLight=saturate(dot(n,fill));
                half hemi=saturate(n.y*.5+.5);
                half3 diffuse=base*(half3(.12,.17,.18)+hemi*.13+keyLight*half3(1.05,.96,.80)+fillLight*half3(.17,.27,.30));
                float3 h=normalize(key+v), h2=normalize(fill+v);
                half spec=pow(saturate(dot(n,h)),lerp(28,160,_Smoothness));
                spec+=pow(saturate(dot(n,h2)),55)*.45;
                half broad=pow(saturate(dot(n,h)),6)*_Metallic*.24;
                half rim=pow(1-saturate(dot(n,v)),4)*(.035+_Metallic*.13);
                half3 specColor=lerp(half3(1,.96,.87),lerp(base,half3(1,.85,.5),.3),_Metallic);
                half3 col=diffuse+specColor*(spec*lerp(.22,.8,_Metallic)+broad)+half3(.52,.75,.74)*rim;
                col=lerp(col,col*half3(.70,.81,1.03)+half3(.009,.016,.03),_CityAtmosphere);
                // Light from the modeled lanterns and windows is baked into this
                // mesh stream, avoiding hundreds of realtime lights on WebGL.
                col+=base*i.light;
                col+=_EmissionColor.rgb;
                half haze=saturate((distance(_WorldSpaceCameraPos,i.world)-25)/65)*_CityAtmosphere*.47;
                return half4(lerp(col,half3(.16,.23,.36),haze),1);
            }
            ENDHLSL
        }
        Pass
        {
            Tags {"LightMode"="DepthOnly"}
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 vertDepth(float4 v:POSITION):SV_POSITION{return TransformObjectToHClip(v.xyz);}
            half4 fragDepth():SV_Target{return 0;}
            ENDHLSL
        }
    }
}
