using UnityEngine;

/// <summary>Renders solid trees into the illustrated map; wind bends the crowns above fixed roots.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class GroundedForestGrove : MonoBehaviour
{
    public Camera forestCamera;
    public UnityEngine.UI.RawImage picture;
    public MeshRenderer[] trees;
    public Transform[] roots;
    public Vector2[] artworkRoots;
    MaterialPropertyBlock properties;
    RenderTexture originalTarget, runtimeTarget;
    float elapsed;
    bool paused;

    void OnEnable()
    {
        if (Application.isPlaying && forestCamera != null && picture != null && runtimeTarget == null)
        {
            originalTarget = forestCamera.targetTexture;
            if (originalTarget != null)
            {
                runtimeTarget = new RenderTexture(originalTarget) { name = "Grounded forest (runtime)" };
                runtimeTarget.Create();
                forestCamera.targetTexture = runtimeTarget;
                picture.texture = runtimeTarget;
            }
        }
        Pose(elapsed);
    }

    void OnDisable()
    {
        if (forestCamera != null) forestCamera.enabled = false;
        if (runtimeTarget == null) return;
        if (forestCamera != null) forestCamera.targetTexture = originalTarget;
        if (picture != null) picture.texture = originalTarget;
        runtimeTarget.Release();
        Destroy(runtimeTarget);
        runtimeTarget = null;
    }

    void OnApplicationPause(bool value) => paused = value;

    void Update()
    {
        bool visible = picture != null && picture.isActiveAndEnabled;
        if (forestCamera != null) forestCamera.enabled = visible;
        if (!Application.isPlaying || paused || !visible) return;
        elapsed += Time.unscaledDeltaTime;
        Pose(elapsed);
    }

    public void Pose(float seconds)
    {
        if (trees == null) return;
        if (properties == null) properties = new MaterialPropertyBlock();
        for (int i=0;i<trees.Length;i++)
        {
            if (trees[i] == null) continue;
            trees[i].GetPropertyBlock(properties);
            properties.SetFloat("_WindTime", seconds);
            properties.SetFloat("_WindPhase", i*1.71f);
            trees[i].SetPropertyBlock(properties);
        }
    }
}
