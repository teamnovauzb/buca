#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class BuildSuitcaseTrainPair
{
    [MenuItem("RealBuca/Forest Harbor/Add Wooden Train Chimney Smoke")]
    public static string ApplyHoneySmoke()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode only.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if (scene.path != "Assets/Scenes/MainMenu.unity" || map == null ||
            map.trainMechanism.woodland.body.name != "HoneyOakToyEngine")
            throw new Exception("Open the menu with the wooden train first.");
        Directory.CreateDirectory(HoneyOutput);
        string backup = HoneyOutput + "/MainMenu.before-chimney-smoke.unity";
        if (!File.Exists(backup)) File.Copy(scene.path, backup);
        InstallHoneySmoke(map);
        var rig = map.GetComponent<SuitcaseLocomotiveView>();
        var view = map.illustratedMenu;
        Undo.RecordObjects(new Object[] { map, rig, view, view.locomotive }, "Make room for the rising steam");
        // Expand the transparent viewport at the same pixels-per-unit so the train
        // keeps its size and wheel contact, while the smoke has room above the funnel.
        rig.verticalSize = 3.8f;
        rig.centerOffset = new Vector3(0, 2f, 0);
        view.locomotive.sizeDelta = new Vector2(228, 152);
        view.forestTrainGroundOffset = 2f * Mathf.Sqrt(.5f) * 20;
        view.PoseTrain(); rig.Frame();
        PrefabUtility.SaveAsPrefabAsset(map.trainMechanism.woodland.body, Folder + "/HoneyOakToyEngine.prefab");
        foreach (var item in new Object[] { map, rig, view, map.chimneySmoke }) EditorUtility.SetDirty(item);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Saved chimney puffs synchronized to each chuk, with room above the funnel.";
    }

    [MenuItem("RealBuca/Forest Harbor/Sync Chimney Smoke To Chuk Sound")]
    public static string ApplyHoneyChugSmoke()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode only.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if (scene.path != "Assets/Scenes/MainMenu.unity" || map == null || map.woodenChug == null)
            throw new Exception("Open the menu with the wooden train sound first.");
        Directory.CreateDirectory(HoneyOutput);
        string backup = HoneyOutput + "/MainMenu.before-synchronized-smoke.unity";
        if (!File.Exists(backup)) EditorSceneManager.SaveScene(scene, backup, true);
        InstallHoneySmoke(map);
        PrefabUtility.SaveAsPrefabAsset(map.trainMechanism.woodland.body, Folder + "/HoneyOakToyEngine.prefab");
        EditorUtility.SetDirty(map); EditorUtility.SetDirty(map.chimneySmoke);
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Saved one visible chimney puff per audible chuk; no automatic idle emission.";
    }

    static void InstallHoneySmoke(TwilightRailwayMap map)
    {
        var shader = Shader.Find("BUCA/Toy Chimney Smoke");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Chimney smoke shader did not compile.");
        string materialPath = Folder + "/Materials/HoneyOak-ChimneySmoke.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "HoneyOak-ChimneySmoke" };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.SetColor("_BaseColor", new Color(.94f, .96f, 1f, 1));
        EditorUtility.SetDirty(material);
        var engine = map.trainMechanism.woodland.body.transform;
        var emitter = engine.Find("ChimneySmoke");
        if (emitter == null)
        {
            emitter = Group("ChimneySmoke", engine);
            Undo.RegisterCreatedObjectUndo(emitter.gameObject, "Add chimney smoke");
        }
        emitter.localPosition = new Vector3(1.04f, 2.79f, 0);
        emitter.localRotation = Quaternion.identity; emitter.localScale = Vector3.one;
        var particles = emitter.GetComponent<ParticleSystem>();
        if (particles == null) particles = emitter.gameObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = true; main.duration = 2; main.playOnAwake = true; main.prewarm = false;
        main.useUnscaledTime = true; main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.90f, 1.25f);
        main.startSpeed = 0; main.startSize = new ParticleSystem.MinMaxCurve(.40f, .55f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-.3f, .3f);
        main.startColor = Color.white; main.maxParticles = 24;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var shape = particles.shape; shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = .07f;
        shape.rotation = new Vector3(-90, 0, 0);
        var emission = particles.emission; emission.enabled = false; emission.rateOverTime = 0;
        var velocity = particles.velocityOverLifetime; velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = -.28f; velocity.y = .85f; velocity.z = 0;
        var size = particles.sizeOverLifetime; size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(
            new Keyframe(0, .45f), new Keyframe(.35f, 1.1f), new Keyframe(1, 1.65f)));
        var color = particles.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(.84f,.88f,.92f), 1) },
            new[] { new GradientAlphaKey(.75f, 0), new GradientAlphaKey(.82f, .08f), new GradientAlphaKey(.60f, .40f), new GradientAlphaKey(0, 1) });
        color.color = gradient;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard; renderer.sharedMaterial = material;
        renderer.maxParticleSize = 1; renderer.minParticleSize = 0;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        var smoke = emitter.GetComponent<TrainChimneySmoke>();
        if (smoke == null) smoke = emitter.gameObject.AddComponent<TrainChimneySmoke>();
        smoke.plume = particles;
        Undo.RecordObject(map, "Connect chimney smoke"); map.chimneySmoke = smoke;
    }
}
#endif
