using UnityEngine;

/// <summary>
/// Attach this to every rail/wall you want the puck to "light up" on hit.
/// On first contact, permanently boosts the renderer's emission to the
/// lit value; LevelManager counts each unique lit rail for star ratings.
///
/// Set this up manually: add the component, drag the MeshRenderer, and
/// pick the lit emission color (or leave default for white-neon ×1.8).
/// </summary>
[DisallowMultipleComponent]
public class RailLight : MonoBehaviour
{
    [Tooltip("Renderer whose material to boost. Leave empty to auto-find on Awake.")]
    public Renderer targetRenderer;

    [Tooltip("Emission color applied on first hit. Defaults to strong white-neon.")]
    public Color litEmission = new Color(5f, 5f, 5.5f);

    [Tooltip("If true, flashes briefly brighter on hit (scales above litEmission).")]
    public bool flashOnHit = true;
    public float flashMultiplier = 1.8f;
    public float flashDuration = 0.18f;

    Material _runtimeMat;     // instanced so we don't mutate the shared asset
    Color _originalEmission;
    Color _targetEmission;
    float _flashTime;

    public bool IsLit { get; private set; }

    void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null) return;

        // Instantiate the material so boosting emission doesn't bleed into
        // other walls that share the same shared material asset.
        _runtimeMat = targetRenderer.material;
        if (_runtimeMat != null && _runtimeMat.HasProperty("_EmissionColor"))
        {
            _originalEmission = _runtimeMat.GetColor("_EmissionColor");
        }
        _targetEmission = _originalEmission;
    }

    /// <summary>Called by LevelManager on first wall contact.</summary>
    public bool LightUp()
    {
        if (IsLit) { TriggerFlash(); return false; }
        IsLit = true;
        _targetEmission = litEmission;
        TriggerFlash();
        return true; // was a new light-up
    }

    void TriggerFlash()
    {
        if (flashOnHit) _flashTime = flashDuration;
    }

    public void Reset()
    {
        IsLit = false;
        _targetEmission = _originalEmission;
        _flashTime = 0f;
        ApplyEmission(_originalEmission);
    }

    void Update()
    {
        if (_runtimeMat == null || !_runtimeMat.HasProperty("_EmissionColor")) return;

        Color current = _targetEmission;
        if (_flashTime > 0f)
        {
            _flashTime -= Time.deltaTime;
            float k = Mathf.Clamp01(_flashTime / flashDuration);
            current = Color.Lerp(_targetEmission, _targetEmission * flashMultiplier, k);
        }

        ApplyEmission(current);
    }

    void ApplyEmission(Color c)
    {
        _runtimeMat.SetColor("_EmissionColor", c);
        _runtimeMat.EnableKeyword("_EMISSION");
    }
}
