#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RabbitGiftLiveValidation
{
    const string Key="RabbitGiftLiveValidation";
    static RabbitGiftReward reward;
    static int phase, startFrame;
    static double started;
    static float scale;
    static bool sawCheer;
    static RabbitGiftLiveValidation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
    public static string Start()
    {
        if(EditorApplication.isPlaying)return Run();
        SessionState.SetBool(Key+"Pending",true);
        EditorApplication.EnterPlaymode();return "Waiting for Play mode";
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool(Key+"Pending",false))return;
        SessionState.SetBool(Key+"Pending",false);EditorApplication.delayCall+=()=>Run();
    }
    public static string Run()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first");
        sawCheer=false;scale=Time.timeScale;started=EditorApplication.timeSinceStartup;phase=0;
        SessionState.SetString(Key,"running");
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BuildRabbitGiftReward.Path));
        UnityEngine.Object.DontDestroyOnLoad(go);go.name="RabbitValidation";go.SetActive(true);
        reward=go.GetComponent<RabbitGiftReward>();Time.timeScale=0;
        startFrame=Time.frameCount;reward.StartCoroutine(reward.Play());reward.Confirm();
        return "Started live validation";
    }
    static void Tick()
    {
        if(SessionState.GetString(Key,"")!="running")return;
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Play mode ended during validation");
            if(reward==null)throw new Exception("Reward removed during validation");
            if(EditorApplication.timeSinceStartup-started>25)throw new Exception("Live playback timed out");
            if(reward.cheerSource!=null && reward.cheerSource.isPlaying)
            {
                if(reward.Elapsed<RabbitGiftCelebration.StartSeconds)throw new Exception("Cheer played before greeting");
                sawCheer=true;
            }
            if(phase==0 && reward.WaitingForContinue && reward.Elapsed>4.6f)
            {
                if(Time.frameCount<=startFrame+10||Time.timeScale!=0||!reward.controls.interactable)
                    throw new Exception("Unscaled frames or ready control failed");
                reward.continueButton.onClick.Invoke();phase=1;
            }
            if(phase==1&&!reward.gameObject.activeSelf)
            {
                if(reward.sound.isPlaying||reward.cheerSource.isPlaying)throw new Exception("Audio not stopped");
                if(!sawCheer)throw new Exception("Greeting cheer never played");
                Finish("PASS: real Play-mode frames, unscaled reveal at timeScale=0, early confirmation blocked, Continue closes, cheer starts at greeting and both sounds stop");
            }
        }
        catch(Exception e){Finish("FAIL: "+e.Message);}
    }
    static void Finish(string message)
    {
        SessionState.SetString(Key,message);Time.timeScale=scale;
        if(reward!=null)UnityEngine.Object.Destroy(reward.gameObject);
        System.IO.Directory.CreateDirectory("output/rabbit-gift");System.IO.File.WriteAllText("output/rabbit-gift/live-validation.txt",message);
    }
}
#endif
