using UnityEngine;

/// <summary>Advances masked water, reflection and boat-surface materials independently of game time.</summary>
[DisallowMultipleComponent]
public sealed class ForestHarborWaterFlow : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.RawImage artwork;
    [SerializeField, Range(0f, 2f)] float speed = 1f;
    static readonly int FlowTime = Shader.PropertyToID("_FlowTime");
    Material originalMaterial, animatedMaterial;
    float elapsed;

    public void Configure(UnityEngine.UI.RawImage image) => artwork = image;

    void OnEnable()
    {
        if (!Application.isPlaying || artwork == null || artwork.material == null) return;
        originalMaterial = artwork.material;
        animatedMaterial = new Material(originalMaterial) { name = originalMaterial.name + " (Runtime)" };
        artwork.material = animatedMaterial;
        animatedMaterial.SetFloat(FlowTime, elapsed);
    }

    void Update()
    {
        if (animatedMaterial == null) return;
        elapsed += Time.unscaledDeltaTime * speed;
        animatedMaterial.SetFloat(FlowTime, elapsed);
    }

    void OnDisable()
    {
        if (animatedMaterial == null) return;
        if (artwork != null && artwork.material == animatedMaterial) artwork.material = originalMaterial;
        Destroy(animatedMaterial);
        animatedMaterial = null;
    }
}
