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

    [Tooltip("Emission color applied on first hit. Auto-derived in Awake from the wall's own resting emission × litBoost, so it always matches the wall's color.")]
    public Color litEmission = new Color(5f, 5f, 5.5f);

    [Tooltip("Hit color = the wall's resting emission × this. Keeps the flash the same hue as the wall, so recoloring the material is all you need.")]
    public float litBoost = 1.6f;

    [Tooltip("If true, flashes briefly brighter on hit (scales above litEmission).")]
    public bool flashOnHit = true;
    public float flashMultiplier = 1.8f;
    public float flashDuration = 0.18f;

    Material _runtimeMat;     // instanced so we don't mutate the shared asset
    Color _originalEmission;
    Color _targetEmission;
    float _flashTime;
    bool _settled;            // resting target already written → skip per-frame writes

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
            // Derive the hit color from the wall's OWN resting emission so the
            // glow-on-hit always matches the wall's color. Recolor the shared
            // material and both the resting glow AND the hit flash follow — no
            // per-wall setup, and it survives level regeneration.
            litEmission = _originalEmission * litBoost;
        }
        _targetEmission = _originalEmission;
    }

    /// <summary>Called by LevelManager on first wall contact.</summary>
    public bool LightUp()
    {
        if (IsLit) { TriggerFlash(); _settled = false; return false; }
        IsLit = true;
        _targetEmission = litEmission;
        TriggerFlash();
        _settled = false;
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
        _settled = false;
        ApplyEmission(_originalEmission);
    }

    void Update()
    {
        if (_runtimeMat == null || !_runtimeMat.HasProperty("_EmissionColor")) return;

        if (_flashTime > 0f)
        {
            _flashTime -= Time.deltaTime;
            float k = Mathf.Clamp01(_flashTime / flashDuration);
            ApplyEmission(Color.Lerp(_targetEmission, _targetEmission * flashMultiplier, k));
            _settled = false;
            return;
        }

        // Not flashing — write the resting emission ONCE, then idle. The dozens of
        // resting walls in a level no longer touch their material every frame.
        if (!_settled) { ApplyEmission(_targetEmission); _settled = true; }
    }

    void ApplyEmission(Color c)
    {
        // Guard: Unity auto-invokes Reset() the moment this component is added
        // in the editor (e.g. by the level generator), before Awake() has set
        // up _runtimeMat — so this can run with no material yet. No-op safely.
        if (_runtimeMat == null || !_runtimeMat.HasProperty("_EmissionColor")) return;
        _runtimeMat.SetColor("_EmissionColor", c);
        _runtimeMat.EnableKeyword("_EMISSION");
    }
}
