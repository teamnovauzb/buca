#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class BuildSuitcaseTrainPair
{
    const string ChugPath = "Assets/Audio/Celebration/WoodenTrainChukLoop.wav";

    [MenuItem("RealBuca/Forest Harbor/Add Wooden Train Chuk Sound")]
    public static string ApplyHoneyChug()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Edit mode only.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        if (scene.path != "Assets/Scenes/MainMenu.unity" || map == null || map.motionAudio == null)
            throw new Exception("Open the existing MainMenu railway first.");
        Directory.CreateDirectory(HoneyOutput);
        string backup = HoneyOutput + "/MainMenu.before-chuk-audio.unity";
        if (!File.Exists(backup)) File.Copy(scene.path, backup);
        InstallHoneyChug(map);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return "Saved wooden train chuk-chuk loop: four chuffs per wheel revolution, shared SFX volume, silent when stopped.";
    }

    static void InstallHoneyChug(TwilightRailwayMap map)
    {
        if (!File.Exists(ChugPath)) BakeHoneyChug();
        AssetDatabase.ImportAsset(ChugPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (AudioImporter)AssetImporter.GetAtPath(ChugPath);
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = true; importer.SaveAndReimport();
        Undo.RecordObjects(new Object[] { map, map.motionAudio }, "Connect wooden train chuk sound");
        map.woodenChug = AssetDatabase.LoadAssetAtPath<AudioClip>(ChugPath);
        if (map.woodenChug == null) throw new Exception("Train chuk sound import failed.");
        map.motionAudio.playOnAwake = false; map.motionAudio.spatialBlend = 0;
        map.motionAudio.loop = true; map.motionAudio.dopplerLevel = 0;
        EditorUtility.SetDirty(map); EditorUtility.SetDirty(map.motionAudio);
    }

    // Original synthesized steam: breathy exhaust, a warm low pulse and a soft wooden clack.
    // Four alternating accents form one loop; speed comes from the animated driving wheels.
    static void BakeHoneyChug()
    {
        const int rate = 24000;
        var samples = new float[rate];
        var random = new System.Random(1847);
        float[] accents = { 1f, .77f, .91f, .80f };
        float peak = 0;
        for (int puff = 0; puff < TrainChimneySmoke.ChuffsPerLoop; puff++)
        {
            float low = 0, high = 0, body = 0;
            int offset = puff * rate / TrainChimneySmoke.ChuffsPerLoop;
            for (int i = 0; i < rate * .21f; i++)
            {
                float t = i / (float)rate;
                float noise = (float)(random.NextDouble() * 2 - 1);
                high += .43f * (noise - high);
                low += .045f * (noise - low);
                body += .07f * (noise - body);
                float attack = 1 - Mathf.Exp(-t / .007f);
                float release = Mathf.Clamp01((.21f - t) / .045f);
                float breath = attack * Mathf.Exp(-t / .065f) * release;
                float thump = Mathf.Sin(2 * Mathf.PI * (135 * t - 95 * t * t)) * Mathf.Exp(-t / .035f);
                float clack = Mathf.Sin(2 * Mathf.PI * 310 * t) * Mathf.Exp(-t / .014f);
                float sample = accents[puff] * (breath * (1.6f * (high - low) + 1.3f * body) + attack * (.20f * thump + .09f * clack));
                samples[offset + i] = sample; peak = Mathf.Max(peak, Mathf.Abs(sample));
            }
        }
        using (var writer = new BinaryWriter(File.Create(ChugPath)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
            writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
            foreach (float sample in samples) writer.Write((short)(sample / Mathf.Max(.001f, peak) * .88f * short.MaxValue));
        }
    }
}
#endif
