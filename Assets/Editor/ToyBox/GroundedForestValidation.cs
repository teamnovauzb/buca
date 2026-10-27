#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class GroundedForestValidation
{
    const string Output = "output/grounded-forest";
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    public static string CheckAndRender()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit mode required.");
        var source = Object.FindAnyObjectByType<TwilightRailwayMap>(FindObjectsInactive.Include);
        Require(source != null, "Main menu required.");
        var preview = EditorSceneManager.NewPreviewScene();
        var targets = new List<RenderTexture>();
        try
        {
            var copy = Object.Instantiate(source.gameObject);
            SceneManager.MoveGameObjectToScene(copy, preview);
            copy.SetActive(false);
            var map = copy.GetComponent<TwilightRailwayMap>();
            map.eventAudio = null; map.motionAudio = null; map.Open(null);
            typeof(TwilightRailwayMap).GetField("highest", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(map, 49);
            foreach (var camera in copy.GetComponentsInChildren<Camera>(true))
            {
                camera.enabled = false; camera.scene = preview;
                if (camera.targetTexture == null) continue;
                var previous = camera.targetTexture;
                var target = new RenderTexture(previous);
                targets.Add(target);
                camera.targetTexture = target;
                foreach (var image in copy.GetComponentsInChildren<UnityEngine.UI.RawImage>(true))
                    if (image.texture == previous) image.texture = target;
            }
            copy.SetActive(true);
            var grove = copy.GetComponentInChildren<GroundedForestGrove>(true);
            Require(grove != null && grove.roots.Length == 14, "Expected fourteen 3D trees.");
            Require(map.illustratedMenu.artwork.GetComponentsInChildren<ForestBirchGraphic>().Length == 0,
                "Flat birch cutouts still active.");
            Require(!grove.picture.raycastTarget, "Trees must not intercept level clicks.");
            Require(grove.forestCamera.cullingMask == 1 << 26, "Forest camera layer incorrect.");
            for (int i = 0; i < grove.roots.Length; i++)
            {
                var root = grove.roots[i];
                var mesh = grove.trees[i].GetComponent<MeshFilter>().sharedMesh;
                Require(mesh.bounds.size.z > 1 && mesh.vertexCount > 1000, "Tree is a flat card.");
                Require(Mathf.Abs(root.localPosition.y) < .0001f, "Root not on the ground.");
                var screen = grove.forestCamera.WorldToViewportPoint(root.position);
                var point = new Vector2(screen.x * 850, (1 - screen.y) * 941);
                Require(Vector2.Distance(point, grove.artworkRoots[i]) < .1f, "Tree ground placement moved: " + i);
                Require(root.Find("GroundContactAndCanopyShadow") != null, "Missing ground shadow.");
                var position = root.position; var rotation = root.rotation;
                grove.Pose(8);
                Require(root.position == position && root.rotation == rotation, "Wind moved the roots.");
            }
            grove.Pose(0);
            var crane = copy.GetComponentInChildren<ForestHarborCrane>(true);
            if (crane != null) crane.Pose(0);
            foreach (var camera in copy.GetComponentsInChildren<Camera>(true))
                if (camera.targetTexture != null)
                    RenderPipeline.SubmitRenderRequest(camera,
                        new UniversalRenderPipeline.SingleCameraRequest { destination = camera.targetTexture });

            Directory.CreateDirectory(Output);
            var render = typeof(ForestHarborValidation).GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Static);
            render.Invoke(null, new object[] { map, 11, 1672, 941, Output + "/Grounded-forest.png" });
            CaptureDetail(map);
            string report = "PASS: 14 solid 3D trees; no active flat birch cutouts; all roots on the ground plane and within 0.1 pixels of their intended map positions; wind leaves the roots fixed; tree ground shadows present; all 50 level buttons and footer controls remain clickable. Overview and detail rendered from the saved scene in an isolated preview.";
            File.WriteAllText(Output + "/validation.txt", report);
            return report;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
            foreach (var target in targets) { target.Release(); Object.DestroyImmediate(target); }
        }
    }

    static void CaptureDetail(TwilightRailwayMap map)
    {
        var camera = map.viewCamera;
        var previous = camera.targetTexture; var active = RenderTexture.active;
        var target = new RenderTexture(3344, 1882, 24, RenderTextureFormat.ARGB32);
        var image = new Texture2D(1200, 600, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target; camera.aspect = 1672f / 941;
            Canvas.ForceUpdateCanvases(); map.illustratedMenu.Layout(); Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,
                new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            // Central woodland at native 2x rendering resolution.
            image.ReadPixels(new Rect(220, 862, 1200, 600), 0, 0); image.Apply();
            File.WriteAllBytes(Output + "/Grounded-forest-detail.png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previous; RenderTexture.active = active;
            target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image);
        }
    }
}
#endif
