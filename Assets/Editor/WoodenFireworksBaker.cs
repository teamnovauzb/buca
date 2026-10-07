#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Saved twin launch/burst effects and their synchronized prerecorded boom.</summary>
public static class WoodenFireworksBaker
{
    const string PrefabPath="Assets/ToyBoxMenu/Prefabs/PremiumWinCelebration.prefab";
    const string AudioPath="Assets/Audio/Celebration/HoleInOneTwinFireworks.wav";
    public static void Configure(ToyBoxWinCelebration view)
    {
        var old=view.fountainRoot.transform.Find("FireworkBursts");
        if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("FireworkBursts").transform;root.SetParent(view.fountainRoot.transform,false);
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/ToyBoxMenu/HoleInOne/WarmSpark.mat");
        var list=new List<ParticleSystem>();
        foreach(var effect in view.celebrationEffects)if(effect!=null)list.Add(effect);
        // The two existing jets become short launches which end as each burst opens.
        foreach(var effect in list)
        {
            if(!effect.name.StartsWith("SparkFountain"))continue;
            effect.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=effect.main;main.duration=.48f;main.startDelay=effect.name.EndsWith("-1")?0:.18f;
            main.startLifetime=new ParticleSystem.MinMaxCurve(.18f,.30f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(6f,8f);main.gravityModifier=.05f;
        }
        for(int side=0;side<2;side++)
        {
            float burstAt=side==0?.55f:.73f;
            Vector3 position=new Vector3(side==0?-1.35f:1.35f,2.05f,0);
            list.Add(Burst(root,"Firework"+(side+1),position,material,burstAt,false,(uint)(701+side)));
            list.Add(Burst(root,"Flash"+(side+1),position,material,burstAt,true,(uint)(801+side)));
        }
        view.celebrationEffects=list.ToArray();
        view.fireworkAudio=view.GetComponent<AudioSource>();
        if(view.fireworkAudio==null)view.fireworkAudio=view.gameObject.AddComponent<AudioSource>();
        view.fireworkAudio.playOnAwake=false;view.fireworkAudio.loop=false;view.fireworkAudio.spatialBlend=0;view.fireworkAudio.volume=.8f;
        view.fireworkAudio.dopplerLevel=0;view.fireworkAudio.priority=80;
        view.fireworkBoom=AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
        if(view.fireworkBoom==null)throw new Exception("Missing saved firework sound");
        view.fireworkAudio.clip=view.fireworkBoom;
    }
    static ParticleSystem Burst(Transform parent,string name,Vector3 position,Material material,float delay,bool flash,uint seed)
    {
        var go=new GameObject(name,typeof(ParticleSystem));go.transform.SetParent(parent,false);go.transform.localPosition=position;
        var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=seed;
        var main=ps.main;main.loop=false;main.playOnAwake=false;main.useUnscaledTime=true;main.duration=1.4f;main.startDelay=delay;
        main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.startLifetime=new ParticleSystem.MinMaxCurve(flash?.14f:.7f,flash?.14f:1.15f);
        main.startSpeed=new ParticleSystem.MinMaxCurve(flash?0:1.3f,flash?0:2.7f);
        main.startSize=new ParticleSystem.MinMaxCurve(flash?.7f:.04f,flash?.7f:.09f);
        main.gravityModifier=flash?0:.1f;main.maxParticles=flash?1:64;
        var emission=ps.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)(flash?1:56))});
        var shape=ps.shape;shape.enabled=!flash;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.035f;
        var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(new Color(1,.96f,.7f),0),new GradientColorKey(new Color(1,.65f,.15f),.6f),new GradientColorKey(new Color(1,.36f,.04f),1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.95f,.35f),new GradientAlphaKey(0,1)});color.color=gradient;
        var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,0));
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        renderer.renderMode=flash?ParticleSystemRenderMode.Billboard:ParticleSystemRenderMode.Stretch;renderer.lengthScale=3f;renderer.velocityScale=.10f;
        return ps;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        var importer=(AudioImporter)AssetImporter.GetAtPath(AudioPath);
        if(importer==null)throw new Exception("Audio not imported yet");
        var sample=importer.defaultSampleSettings;sample.loadType=AudioClipLoadType.DecompressOnLoad;sample.compressionFormat=AudioCompressionFormat.Vorbis;sample.quality=.85f;importer.defaultSampleSettings=sample;importer.forceToMono=false;importer.SaveAndReimport();
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try{Configure(root.GetComponent<ToyBoxWinCelebration>());PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();return "Two saved fireworks with synchronized boom audio.";}
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
#endif
