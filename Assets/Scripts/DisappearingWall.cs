using UnityEngine;

/// <summary>
/// Wall that periodically toggles its collider + renderer on/off,
/// creating a timed gate. Two adjacent DisappearingWalls with opposite
/// phases form an alternating barrier the player must time.
///
/// Uses MeshRenderer.enabled + Collider.enabled (no destruction) so
/// it's cheap and doesn't reallocate.
/// </summary>
public class DisappearingWall : MonoBehaviour
{
    [Tooltip("Seconds the wall stays solid before fading out.")]
    public float onDuration = 1.4f;
    [Tooltip("Seconds the wall stays gone before reappearing.")]
    public float offDuration = 1.4f;
    [Tooltip("Phase offset in seconds — start later in the cycle.")]
    public float phase = 0f;
    [Tooltip("Fade-out duration. The collider toggles instantly at the midpoint of the fade.")]
    public float fadeDuration = 0.15f;

    Renderer _renderer;
    Collider _collider;
    MaterialPropertyBlock _mpb;
    Color _baseColor = Color.white;
    string _colorProp = "_BaseColor";

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _collider = GetComponent<Collider>();
        _mpb = new MaterialPropertyBlock();
        if (_renderer != null && _renderer.sharedMaterial != null)
        {
            var mat = _renderer.sharedMaterial;
            if (mat.HasProperty("_BaseColor")) { _colorProp = "_BaseColor"; _baseColor = mat.GetColor("_BaseColor"); }
            else if (mat.HasProperty("_Color")) { _colorProp = "_Color"; _baseColor = mat.GetColor("_Color"); }
        }
    }

    void Update()
    {
        float cycle = onDuration + offDuration + fadeDuration * 2f;
        float t = Mathf.Repeat(Time.time + phase, cycle);

        // Phase layout per cycle:
        //   [0 .. onDuration]                                 → solid
        //   [onDuration .. onDuration+fadeDuration]           → fading out
        //   [onDuration+fadeDuration .. onDuration+fadeDuration+offDuration] → gone
        //   [...end]                                          → fading in
        float fadeInStart = onDuration + fadeDuration + offDuration;
        bool solid;
        float alpha;
        if (t < onDuration)                         { solid = true;  alpha = 1f; }
        else if (t < onDuration + fadeDuration)     { solid = false; alpha = 1f - (t - onDuration) / fadeDuration; }
        else if (t < fadeInStart)                   { solid = false; alpha = 0f; }
        else                                        { solid = true;  alpha = (t - fadeInStart) / fadeDuration; }

        // Audio: detect solid↔gone transitions and play vanish/reappear.
        // Skip the first frame to avoid a spurious SFX when the wall happens
        // to spawn mid-cycle (e.g. starts in the "gone" phase via phase
        // offset — without this it would play wallVanish on scene load).
        if (!_wasSolidInitialized)
        {
            _wasSolid = solid;
            _wasSolidInitialized = true;
        }
        else if (solid != _wasSolid)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(solid
                    ? AudioManager.Instance.wallReappearSfx
                    : AudioManager.Instance.wallVanishSfx);
            _wasSolid = solid;
        }

        if (_collider != null) _collider.enabled = solid;
        if (_renderer != null)
        {
            _renderer.enabled = alpha > 0.02f;
            _renderer.GetPropertyBlock(_mpb);
            var c = _baseColor; c.a = alpha;
            _mpb.SetColor(_colorProp, c);
            _renderer.SetPropertyBlock(_mpb);
        }
    }

    bool _wasSolid = true;
    bool _wasSolidInitialized;
}
