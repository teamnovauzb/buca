Shader "Hidden/Buca/GaussianBlur"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _BlurDirection;

            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 stepUv = _MainTex_TexelSize.xy * _BlurDirection.xy;

                // Optimized nine-tap Gaussian kernel expressed as five
                // bilinear samples. It is run once horizontally and once
                // vertically by LeaderboardPanel.
                fixed4 color = tex2D(_MainTex, input.uv) * 0.2270270270;
                color += tex2D(_MainTex, input.uv + stepUv * 1.3846153846) * 0.3162162162;
                color += tex2D(_MainTex, input.uv - stepUv * 1.3846153846) * 0.3162162162;
                color += tex2D(_MainTex, input.uv + stepUv * 3.2307692308) * 0.0702702703;
                color += tex2D(_MainTex, input.uv - stepUv * 3.2307692308) * 0.0702702703;
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
