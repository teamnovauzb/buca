// Motion is evaluated only inside the authored water field. UV samples that reach
// masonry, banks or rails are rejected, so solid objects never wobble with the water.
sampler2D _WaterData;
float _WaterMotion, _WaterPhaseOffset;

fixed4 AnimateWorkshopWater(float2 uv,float2 p,float t,out float coverage)
{
    float4 field=tex2D(_WaterData,uv);
    coverage=field.r;
    fixed4 original=tex2D(_MainTex,uv);
    if(coverage<.002 || _WaterMotion<.001) return original;

    float fall=field.b,stream=field.a;
    float edge=smoothstep(0,5,field.g*32);
    float strength=_WaterMotion*coverage;
    float wave0=p.x*.038+p.y*.057-t*1.22;
    float wave1=p.x*-.064+p.y*.081-t*1.71+1.7;
    float wave2=p.x*.122+p.y*.155-t*2.65;
    float breath=.86+.14*sin(t*.31+p.x*.002);
    float2 swell=float2(sin(wave0)*2.15+sin(wave1)*.85,
        cos(wave0)*.82+sin(wave1+1.1)*.50+sin(wave2)*.18)*breath;
    swell=lerp(swell,float2(sin(p.y*.09-t*1.7)*.55,sin(p.y*.18-t*5.4)*1.7),fall);
    float2 pixelUV=float2(1.0/1672,-1.0/941);
    float2 displaced=uv+swell*pixelUV*strength*edge;
    float valid=tex2D(_WaterData,displaced).r;
    fixed4 water=lerp(original,tex2D(_MainTex,displaced),smoothstep(.15,.85,valid));

    // Two overlapping phases carry painted ripples downstream continuously.
    // Each phase becomes invisible before wrapping, preventing a reset or flicker.
    float forest=1-smoothstep(680,820,p.x);
    float2 riverDirection=normalize(lerp(float2(.24,1),float2(.87,.50),forest));
    float2 direction=normalize(lerp(float2(.97,.24),riverDirection,stream));
    direction=normalize(lerp(direction,float2(.03,1),fall));
    float rate=lerp(.16,.24,stream);rate=lerp(rate,.62,fall);
    float travel=lerp(16,25,stream);travel=lerp(travel,40,fall);
    float phase0=frac(t*rate),phase1=frac(t*rate+.5);
    float blend0=1-abs(phase0*2-1);
    float2 uv0=displaced-direction*pixelUV*phase0*travel;
    float2 uv1=displaced-direction*pixelUV*phase1*travel;
    float weight0=blend0*smoothstep(.2,.85,tex2D(_WaterData,uv0).r);
    float weight1=(1-blend0)*smoothstep(.2,.85,tex2D(_WaterData,uv1).r);
    float total=weight0+weight1;
    fixed3 current=(tex2D(_MainTex,uv0).rgb*weight0+tex2D(_MainTex,uv1).rgb*weight1)/max(total,.001);
    water.rgb=lerp(water.rgb,current,saturate(total)*edge*lerp(.38,.84,max(stream,fall)));

    // Crossing wave trains produce broad swells with fine, broken surface glints.
    float slope=sin(wave0)*.038+sin(wave1)*.022+sin(wave2)*.009;
    water.rgb*=1+slope*(1-fall*.75)*_WaterMotion;
    float broken=.5+.5*sin(p.x*.13+sin(p.y*.07)*2-t*.35);
    float crest=pow(saturate(sin(wave1)*.5+.5),14)*pow(broken,3);
    water.rgb+=float3(.025,.05,.065)*crest*(1-fall)*_WaterMotion;

    // Lapping waves travel toward shore; foam grows, breaks up and recedes slowly.
    float distance=field.g*32;
    float shore=1-smoothstep(3,23,distance);
    float lap=sin(distance*.31+t*1.42+sin(p.x*.033+p.y*.021)*1.1);
    float foam=pow(saturate(lap*.5+.5),9)*shore*(.35+.65*broken)*(1-fall);
    water.rgb=lerp(water.rgb,float3(.75,.92,1),foam*.23*_WaterMotion);

    // Long vertical streaks and soft turbulence read as falling water, not shaking.
    float ribbons=pow(saturate(sin(p.x*.49+sin(p.x*.12)*2)*.5+.5),5);
    float fallPulse=sin(p.y*.23-t*8.8+sin(p.x*.15));
    water.rgb+=float3(.045,.065,.075)*ribbons*(.45+.55*fallPulse)*fall*_WaterMotion;
    return fixed4(lerp(original.rgb,water.rgb,strength),original.a);
}
