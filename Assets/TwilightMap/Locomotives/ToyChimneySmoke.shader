Shader "BUCA/Toy Chimney Smoke"
{
    Properties { _BaseColor("Steam tint", Color) = (.94,.96,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 position:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 position:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position=TransformObjectToHClip(input.position.xyz);
                output.uv=input.uv;
                output.color=input.color*_BaseColor;
                return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                float2 p=input.uv*2-1;
                float r2=dot(p,p);
                // Soft round puffs, with a lit upper rim and a gentle grey underside.
                half alpha=(1-smoothstep(.14,1,r2))*input.color.a;
                half shade=.80+.18*saturate(p.y*.5+.5)+.06*(1-saturate(r2));
                return half4(input.color.rgb*shade,alpha);
            }
            ENDHLSL
        }
    }
}
