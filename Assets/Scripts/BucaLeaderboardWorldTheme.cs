using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Switches the saved result meshes and palette without rebuilding the live standings.</summary>
public sealed class BucaLeaderboardWorldTheme : MonoBehaviour
{
    [Serializable] public struct Surface
    {
        public Renderer renderer;
        public Material room, city;
    }
    [Serializable] public struct LetterColor
    {
        public TMP_Text label;
        public Color room, city;
    }
    [Serializable] public struct Shape
    {
        public MeshFilter filter;
        public Mesh room, city;
    }
    [Serializable] public struct Pose
    {
        public Transform transform;
        public Vector3 roomPosition, cityPosition;
    }
    [Serializable] public struct Rotation
    {
        public Transform transform;
        public Quaternion room, city;
    }
    [Serializable] public struct TextSize
    {
        public TMP_Text label;
        public float roomMax, roomMin, cityMax, cityMin;
    }
    public LevelManager levelManager;
    public BucaResultStandings standings;
    public Surface[] surfaces;
    public LetterColor[] lettering;
    public Shape[] shapes;
    public Pose[] poses;
    public Rotation[] rotations;
    public TextSize[] textSizes;
    public RectTransform card;
    public Vector2 roomCardSize;
    public GameObject[] roomObjects, cityObjects;
    public Material roomRow, roomPlayer, cityRow, cityPlayer, cityBadge;
    public Material[] roomMedals;
    public Color roomInk, roomIvory;
    public Vector2 roomRankSize;
    public Camera gameplayCamera;
    public UnityEngine.UI.RawImage cityBackdrop;
    public RenderTexture levelCapture, blurScratch, blurredLevel;
    public Material blurMaterial;
    public Color cityInk = new Color(.98f, .93f, .80f);
    public Color cityRankInk = new Color(.025f, .045f, .065f);
    public bool IsCity { get; private set; }
    bool applied;

    public void ApplyForCurrentWorld() => Apply(levelManager != null && levelManager.CurrentLevelNumber > 30);

    // One capture per presentation, rather than a continuous full-screen blur.
    public void CaptureBackdrop()
    {
        if (cityBackdrop == null) return;
        cityBackdrop.gameObject.SetActive(IsCity);
        if (!IsCity || gameplayCamera == null || levelCapture == null || blurMaterial == null) return;
        var target = gameplayCamera.targetTexture;
        var active = RenderTexture.active;
        try
        {
            int height = Mathf.Clamp(Mathf.RoundToInt(960 / Mathf.Max(.1f, gameplayCamera.aspect)), 256, 1920);
            foreach (var texture in new[] { levelCapture, blurScratch, blurredLevel })
                if (texture.height != height || !texture.IsCreated())
                { texture.Release(); texture.width = 960; texture.height = height; texture.Create(); }
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = levelCapture };
            if (RenderPipeline.SupportsRenderRequest(gameplayCamera, request)) RenderPipeline.SubmitRenderRequest(gameplayCamera, request);
            else { gameplayCamera.targetTexture = levelCapture; gameplayCamera.Render(); }
            for (int pass = 0; pass < 3; pass++)
            {
                blurMaterial.SetVector("_BlurDirection", new Vector4(3, 0, 0, 0));
                Graphics.Blit(pass == 0 ? levelCapture : blurredLevel, blurScratch, blurMaterial);
                blurMaterial.SetVector("_BlurDirection", new Vector4(0, 3, 0, 0));
                Graphics.Blit(blurScratch, blurredLevel, blurMaterial);
            }
        }
        catch (Exception exception)
        {
            cityBackdrop.gameObject.SetActive(false);
            Debug.LogWarning("[Midnight Brass] Backdrop capture unavailable: " + exception.Message);
        }
        finally { gameplayCamera.targetTexture = target; RenderTexture.active = active; }
    }

    public void Apply(bool city)
    {
        if (applied && IsCity == city) return;
        applied = true;
        IsCity = city;
        foreach (var surface in surfaces)
            if (surface.renderer != null) surface.renderer.sharedMaterial = city ? surface.city : surface.room;
        foreach (var item in lettering)
            if (item.label != null) item.label.color = city ? item.city : item.room;
        if (shapes != null) foreach (var shape in shapes)
            if (shape.filter != null) shape.filter.sharedMesh = city ? shape.city : shape.room;
        if (poses != null) foreach (var pose in poses)
            if (pose.transform != null) pose.transform.localPosition = city ? pose.cityPosition : pose.roomPosition;
        if (rotations != null) foreach (var rotation in rotations)
            if (rotation.transform != null) rotation.transform.localRotation = city ? rotation.city : rotation.room;
        if (textSizes != null) foreach (var size in textSizes)
            if (size.label != null)
            {
                size.label.fontSize = size.label.fontSizeMax = city ? size.cityMax : size.roomMax;
                size.label.fontSizeMin = city ? size.cityMin : size.roomMin;
            }
        if (card != null && roomCardSize.x > 0) card.sizeDelta = city ? new Vector2(2100,1180) : roomCardSize;
        foreach (var item in roomObjects) if (item != null) item.SetActive(!city);
        foreach (var item in cityObjects) if (item != null) item.SetActive(city);
        if (cityBackdrop != null) cityBackdrop.gameObject.SetActive(city);
        standings.normalFace = city ? cityRow : roomRow;
        standings.playerFace = city ? cityPlayer : roomPlayer;
        standings.ink = city ? cityInk : roomInk;
        standings.ivory = city ? cityInk : roomIvory;
        standings.rankInk = city ? cityRankInk : roomInk;
        standings.normalBadgeFace = city ? cityBadge : null;
        standings.rankMaterials = city ? new[] { cityBadge, cityBadge, cityBadge } : roomMedals;
        if (roomRankSize.x <= 0 && standings.rankLabels.Length > 0)
            roomRankSize = standings.rankLabels[0].rectTransform.sizeDelta;
        foreach (var label in standings.rankLabels)
            if (label != null) label.rectTransform.sizeDelta = city ? new Vector2(.27f, roomRankSize.y) : roomRankSize;
        standings.Sync(standings.panel);
    }
}
