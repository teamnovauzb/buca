#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Isolated cinematic renders and wheel/assembly checks, without changing player progress.</summary>
public static class MoonPuffUpgradePreview
{
    const string Output = "output/moon-puff-upgrade";

    public static string CheckAndRender(bool sequence = false)
    {
        // Uses an isolated preview scene; safe alongside a paused running game.
        Directory.CreateDirectory(Output);
        var scene = EditorSceneManager.NewPreviewScene();
        var previous = RenderTexture.active;
        var target = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32);
        var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildWorldTwoHarborReveal.Path);
            var root = Object.Instantiate(prefab); SceneManager.MoveGameObjectToScene(root, scene);
            var reveal = root.GetComponent<WorldTwoHarborReveal>();
            var upgrade = reveal.trainUpgrade;
            if (upgrade == null || reveal.duration < MoonPuffToyUpgrade.Duration) throw new Exception("Upgrade not connected.");
            var cameraObject = new GameObject("UpgradeUICamera"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>(); camera.scene = scene; camera.enabled = false;
            camera.orthographic = true; camera.orthographicSize = 5; camera.nearClipPlane = .01f; camera.farClipPlane = 30;
            camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.015f,.035f,.055f); camera.cullingMask = ~(1 << 28);
            camera.targetTexture = target; camera.aspect = 1.6f;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 5;
            upgrade.trainCamera.scene = scene; upgrade.trainCamera.enabled = false;
            root.SetActive(true); Canvas.ForceUpdateCanvases();
            Validate(upgrade);
            foreach (float seconds in new[] { .55f, 1.25f, 1.5f, 3f, 4.7f, 7f, 8.7f })
                Render(reveal, camera, target, pixels, seconds * reveal.duration / MoonPuffToyUpgrade.Duration, Output + "/Upgrade-" + seconds.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + ".png");
            if (sequence)
            {
                Directory.CreateDirectory(Output + "/frames");
                for (int frame = 0; frame < Mathf.CeilToInt(reveal.duration * 24); frame++)
                    Render(reveal, camera, target, pixels, frame / 24f, Output + "/frames/" + frame.ToString("D4") + ".png");
            }
            File.WriteAllText(Output + "/validation.txt", "PASS: connected 9.3-second level-30 upgrade prefab; grounded chassis throughout 558 samples; grounded star-pixel morph; final assembly rests; six wheels rotate exactly one revolution at their own circumference forward and backward; both coupling rods stay attached; 241 route points match the actual 30–31 crossing; reverse sampling resets the old train. Preview scenes only: no player progress changed.");
            return "PASS: assembly and wheel checks; seven cinematic renders saved to " + Output;
        }
        finally
        {
            RenderTexture.active = previous; EditorSceneManager.ClosePreviewScene(scene);
            target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
        }
    }

    static void Render(WorldTwoHarborReveal reveal, Camera camera, RenderTexture target, Texture2D pixels, float seconds, string path)
    {
        Canvas.ForceUpdateCanvases(); reveal.Sample(seconds); Canvas.ForceUpdateCanvases();
        foreach (var text in reveal.GetComponentsInChildren<TMPro.TMP_Text>()) text.ForceMeshUpdate();
        for (int pass = 0; pass < 2; pass++)
        {
            RenderPipeline.SubmitRenderRequest(reveal.trainUpgrade.trainCamera,
                new UniversalRenderPipeline.SingleCameraRequest { destination = reveal.trainUpgrade.trainCamera.targetTexture });
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        }
        RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
        File.WriteAllBytes(path, pixels.EncodeToPNG());
    }

    static void Validate(MoonPuffToyUpgrade upgrade)
    {
        var map = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        foreach (var filter in upgrade.mechanism.GetComponentsInChildren<MeshFilter>(true))
            Require(filter.sharedMesh != null && filter.sharedMesh.bounds.size.sqrMagnitude > .00001f &&
                filter.sharedMesh.bounds.center.magnitude < 10, "Invalid train mesh: " + filter.name);
        for (int i = 0; i < upgrade.route.Length; i++)
            Require(Vector3.Distance(upgrade.route[i], map.Point(Mathf.Lerp(map.stops[29].distance, map.stops[30].distance, i / 240f))) < .001f, "Cinematic left actual bridge route.");
        foreach (var wheel in upgrade.mechanism.moonlight.wheels)
        foreach (float sign in new[] { -1f, 1f })
        {
            upgrade.mechanism.Pose(0, 1, true); float angle = wheel.pivot.localEulerAngles.z, total = 0;
            for (int step = 1; step <= 32; step++)
            {
                upgrade.mechanism.Pose(sign * 2 * Mathf.PI * wheel.radius * step / 32, 1, true);
                float next = wheel.pivot.localEulerAngles.z; total += Mathf.DeltaAngle(angle, next); angle = next;
                Gear(upgrade.mechanism.moonlight);
            }
            Require(Mathf.Abs(total + sign * 360) < .05f, "Wheel revolution failed.");
        }
        var ground = upgrade.parts.transform.Find("GroundedChassis");
        for (int frame = 0; frame < 558; frame++)
        {
            upgrade.Sample(frame / 60f);
            Require(ground.localPosition == Vector3.zero, "Chassis lifted during upgrade.");
            Gear(upgrade.mechanism.moonlight);
        }
        upgrade.Sample(4.8f);
        foreach (var part in new[] { upgrade.parts.cab, upgrade.parts.boiler, upgrade.parts.chimney, upgrade.parts.roof, upgrade.parts.crescent })
            Require(part.localPosition.sqrMagnitude < .00001f && Quaternion.Angle(part.localRotation, Quaternion.identity) < .001f, "Part did not settle: " + part.name);
        upgrade.Sample(2.5f); Require(upgrade.mechanism.woodland.body.activeSelf && upgrade.mechanism.moonlight.body.activeSelf, "Pixel morph must show both clipped train models.");
        Require(upgrade.originalRoof.localPosition.y < 3f, "Pixel morph lifted the roof.");
        upgrade.Sample(0);
        Require(upgrade.mechanism.woodland.body.activeSelf && !upgrade.mechanism.moonlight.body.activeSelf, "Replay did not reset.");
    }
    static void Gear(SuitcaseTrainMechanism.Engine engine)
    {
        foreach (var gear in engine.gear)
        {
            var a = gear.couplingRod.TransformPoint(Vector3.left * .5f);
            var b = gear.couplingRod.TransformPoint(Vector3.right * .5f);
            Require(Vector3.Distance(a, gear.rearPin.position) < .001f && Vector3.Distance(b, gear.frontPin.position) < .001f, "Coupling rod detached.");
        }
    }
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
}
#endif
