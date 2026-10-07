#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Saved Web-only quality profile; desktop art settings stay available.
public static class BucaWebOptimizer
{
    public const string PipelinePath="Assets/Settings/Web_RPAsset.asset";
    [MenuItem("RealBuca/Apply Web Release Settings")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play mode first.");
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if(pipeline==null)
        {
            AssetDatabase.CopyAsset("Assets/Settings/Mobile_RPAsset.asset",PipelinePath);
            pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        }
        var pipelineSettings=new SerializedObject(pipeline);
        pipelineSettings.FindProperty("m_MainLightShadowsSupported").boolValue=false;
        pipelineSettings.FindProperty("m_AdditionalLightShadowsSupported").boolValue=false;
        pipelineSettings.FindProperty("m_SoftShadowsSupported").boolValue=false;
        pipelineSettings.ApplyModifiedPropertiesWithoutUndo();
        pipeline.shadowDistance=0;
        pipeline.renderScale=1;
        // Preserve HDR and renderer indices: the authored menus use them.
        EditorUtility.SetDirty(pipeline);
        var quality=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0]);
        var levels=quality.FindProperty("m_QualitySettings");int index=-1;
        for(int i=0;i<levels.arraySize;i++)if(levels.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue=="Web")index=i;
        if(index<0){index=levels.arraySize;levels.InsertArrayElementAtIndex(index);}
        var level=levels.GetArrayElementAtIndex(index);
        level.FindPropertyRelative("name").stringValue="Web";
        level.FindPropertyRelative("customRenderPipeline").objectReferenceValue=pipeline;
        level.FindPropertyRelative("shadows").intValue=0;
        level.FindPropertyRelative("vSyncCount").intValue=0;
        level.FindPropertyRelative("realtimeReflectionProbes").boolValue=false;
        level.FindPropertyRelative("excludedTargetPlatforms").arraySize=0;
        var defaults=quality.FindProperty("m_PerPlatformDefaultQuality");bool assigned=false;
        for(int i=0;i<defaults.arraySize;i++)
        {
            var pair=defaults.GetArrayElementAtIndex(i);
            if(pair.FindPropertyRelative("first").stringValue=="WebGL")
            {pair.FindPropertyRelative("second").intValue=index;assigned=true;}
        }
        if(!assigned)throw new InvalidOperationException("WebGL quality mapping missing.");
        quality.ApplyModifiedPropertiesWithoutUndo();
        PlayerSettings.stripEngineCode=true;
        PlayerSettings.WebGL.dataCaching=true;
        PlayerSettings.WebGL.debugSymbolMode=WebGLDebugSymbolMode.Off;
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL,Il2CppCodeGeneration.OptimizeSize);
        // Luxodd uses reflection/JSON and catches exceptions. Keep its current
        // stripping and exception behavior until an integrated browser test.
        // Keep Gzip + fallback: hosting Content-Encoding is not verified here.
        UnityEditor.WebGL.UserBuildSettings.codeOptimization=UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
        AssetDatabase.SaveAssets();
        Debug.Log("[BucaWebOptimizer] Saved Web quality: no realtime shadows, baked reflections; IL2CPP OptimizeSize; caching on. Compression/stripping compatibility preserved.");
    }
}
public sealed class BucaWebReleaseBuildCheck : IPreprocessBuildWithReport
{
    public int callbackOrder=>-100;
    public void OnPreprocessBuild(BuildReport report)
    {
        if(report.summary.platform==BuildTarget.WebGL)BucaWebOptimizer.Apply();
    }
}
#endif
