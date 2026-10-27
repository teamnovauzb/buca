#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Explicitly launched Play-mode check; never participates in a shipped build.</summary>
[InitializeOnLoad]
public sealed class MoonPuffUpgradePlayCheck : MonoBehaviour
{
    const string ArmedKey = "Buca.MoonPuffUpgradePlayCheck.Armed";
    static MoonPuffUpgradePlayCheck()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(ArmedKey, false)) return;
            SessionState.EraseBool(ArmedKey);
            EditorApplication.delayCall += () => Run();
        };
    }
    public static string ArmAndPlay()
    {
        if (Application.isPlaying) return Run();
        SessionState.SetBool(ArmedKey, true); EditorApplication.isPlaying = true;
        return "Live check armed for the first playable frame.";
    }
    public static string LastReport = "Not run";
    WorldTwoHarborReveal reveal;
    float originalTimeScale;
    bool restored;

    public static string Run()
    {
        if (!Application.isPlaying) return "Play mode required";
        LastReport = "Running";
        var go = new GameObject("MoonPuffUpgradePlayCheck");
        go.AddComponent<MoonPuffUpgradePlayCheck>().StartCoroutine(go.GetComponent<MoonPuffUpgradePlayCheck>().Check());
        return "Started isolated upgrade playback; player progress is untouched.";
    }

    IEnumerator Check()
    {
        originalTimeScale = Time.timeScale; Time.timeScale = 0;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TwilightMap/WorkshopRailway/WorldTwoHarborReveal.prefab");
        reveal = Instantiate(prefab).GetComponent<WorldTwoHarborReveal>();
        reveal.name = "MoonPuffUpgradeLiveCheck";
        var upgrade = reveal.trainUpgrade;
        bool finished = false;
        StartCoroutine(PlayToEnd(() => finished = true));
        int firstFrame = Time.frameCount, sampledFrames = 0, smokeFrames = 0;
        bool oldTrain = false, newTrain = false, heardChug = false, wheelMoved = false;
        Quaternion before = Quaternion.identity;
        float elapsed = 0;
        while (!reveal.WaitingForPlay && elapsed < reveal.duration + 3)
        {
            sampledFrames++;
            oldTrain |= upgrade.mechanism.woodland.body.activeSelf;
            newTrain |= upgrade.mechanism.moonlight.body.activeSelf;
            if (upgrade.parts.smoke.plume.particleCount > 0) smokeFrames++;
            heardChug |= upgrade.motionAudio.isPlaying && upgrade.motionAudio.clip == upgrade.chug;
            var after = upgrade.mechanism.moonlight.wheels[0].pivot.localRotation;
            if (upgrade.SampledSeconds > 5.65f && Quaternion.Angle(before, after) > .1f) wheelMoved = true;
            before = after; elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        bool waiting = reveal.WaitingForPlay && !finished;
        bool stopped = !upgrade.motionAudio.isPlaying && upgrade.parts.cab.localPosition == Vector3.zero;
        bool button = reveal.continueButton != null && reveal.continueButton.gameObject.activeInHierarchy;
        if (button) reveal.continueButton.onClick.Invoke();
        yield return null; yield return null;
        bool cameraStopped = upgrade.trainCamera != null && !upgrade.trainCamera.gameObject.activeInHierarchy;
        bool pass = Time.frameCount > firstFrame + 10 && sampledFrames > 10 && smokeFrames > 0 &&
            oldTrain && newTrain && heardChug && wheelMoved && waiting && stopped && button && finished && cameraStopped;
        LastReport = (pass ? "PASS" : "FAIL") + ": frames=" + sampledFrames + ", smoke frames=" + smokeFrames +
            ", old/new train=" + oldTrain + "/" + newTrain + ", chug=" + heardChug + ", wheels=" + wheelMoved +
            ", waiting for Play=" + waiting + ", stopped=" + stopped + ", Play button=" + button +
            ", completed after click=" + finished + ", camera stopped=" + cameraStopped + ". Tested at timeScale=0; no progress writes.";
        File.WriteAllText("output/moon-puff-upgrade/playmode-validation.txt", LastReport);
        Restore(); Destroy(gameObject);
    }

    IEnumerator PlayToEnd(System.Action complete) { yield return reveal.Play(); complete(); }
    void Restore()
    {
        if (restored) return; restored = true; Time.timeScale = originalTimeScale;
        if (reveal != null) Destroy(reveal.gameObject);
    }
    void OnDestroy() { Restore(); }
}
#endif
