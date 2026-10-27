#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class BuildForestWind
{
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Edit mode only");
        const string path="Assets/Audio/Ambience/ForestWind.wav";
        var importer=(AudioImporter)AssetImporter.GetAtPath(path);
        var settings=importer.defaultSampleSettings;
        settings.loadType=AudioClipLoadType.CompressedInMemory;
        settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;
        importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.SaveAndReimport();
        var life=Object.FindAnyObjectByType<ForestHarborLife>(FindObjectsInactive.Include);
        var child=life.transform.Find("ForestWindAmbience");
        if(child==null){child=new GameObject("ForestWindAmbience").transform;child.SetParent(life.transform,false);}
        var audio=child.GetComponent<AudioSource>();
        if(audio==null)audio=child.gameObject.AddComponent<AudioSource>();
        audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);audio.loop=true;audio.playOnAwake=false;
        audio.spatialBlend=0;audio.volume=0;life.windAudio=audio;life.windVolume=.38f;
        if(ShaderUtil.ShaderHasError(Shader.Find("BUCA/Forest Harbor Water Flow")))throw new System.Exception("Wind shader failed");
        EditorSceneManager.MarkSceneDirty(life.gameObject.scene);EditorSceneManager.SaveScene(life.gameObject.scene);AssetDatabase.SaveAssets();
        return "Saved World 1 wind sway and 32-second looping stereo wind; respects ambience volume, focus and pause.";
    }
}
#endif
