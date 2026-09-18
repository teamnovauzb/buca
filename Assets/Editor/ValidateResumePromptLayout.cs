#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ValidateResumePromptLayout
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        var controller = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
        if (controller == null)
            throw new Exception("MainMenuController missing from menu scene.");

        var ensure = typeof(MainMenuController).GetMethod("EnsureResumePrompt",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (ensure == null)
            throw new Exception("EnsureResumePrompt method missing.");
        ensure.Invoke(controller, null);

        var promptField = typeof(MainMenuController).GetField("_resumePromptRoot",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var root = promptField?.GetValue(controller) as GameObject;
        if (root == null)
            throw new Exception("ReturnPlayerPrompt was not created.");
        root.SetActive(true);
        var card = root.transform.Find("ResumeCard");
        var question = card.Find("Question").GetComponent<TextMeshProUGUI>();
        var hint = card.Find("Hint").GetComponent<TextMeshProUGUI>();
        var continueLabel = card.Find("ContinueSavedLevel/Label").GetComponent<TextMeshProUGUI>();
        var qRect = question.rectTransform;
        var hRect = hint.rectTransform;
        float gap = qRect.anchoredPosition.y - qRect.rect.height / 2f
            - (hRect.anchoredPosition.y + hRect.rect.height / 2f);
        if (gap < 25f)
            throw new Exception("Question/hint boxes too close: " + gap);

        foreach (int level in new[] { 2, 30 })
        {
            question.text = $"CONTINUE FROM LEVEL {level}?";
            continueLabel.text = $"CONTINUE LEVEL {level}";
            Canvas.ForceUpdateCanvases();
            question.ForceMeshUpdate();
            hint.ForceMeshUpdate();
            if (question.isTextOverflowing || hint.isTextOverflowing)
                throw new Exception("Resume text overflows at level " + level);
            Debug.Log($"BUCA_RESUME_PROMPT_LAYOUT_PASS level={level} gap={gap:F0} font={question.fontSize:F0}");
        }

        var camera = Camera.main;
        if (camera == null)
            throw new Exception("Main menu camera missing.");
        var canvas = root.GetComponentInParent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 0.5f;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        Canvas.ForceUpdateCanvases();

        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            var output = Path.Combine(Path.GetTempPath(), "BucaResumePrompt-layout-preview.png");
            File.WriteAllBytes(output, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            Debug.Log("BUCA_RESUME_PROMPT_CAPTURE=" + output);
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
#endif
