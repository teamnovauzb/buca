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
    [Tooltip("Transition gap between the solid and hidden phases.")]
    public float fadeDuration = 0.15f;

    Renderer _renderer;
    Collider _collider;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _collider = GetComponent<Collider>();
    }

    void Update()
    {
        float on = Mathf.Max(0f, onDuration);
        float off = Mathf.Max(0f, offDuration);
        float fade = Mathf.Max(0.01f, fadeDuration);
        float cycle = Mathf.Max(0.01f, on + off + fade * 2f);
        float t = Mathf.Repeat(Time.time + phase, cycle);

        // Phase layout per cycle:
        //   [0 .. onDuration]                                 → solid
        //   [onDuration .. onDuration+fadeDuration]           → fading out
        //   [onDuration+fadeDuration .. onDuration+fadeDuration+offDuration] → gone
        //   [...end]                                          → fading in
        float fadeInStart = on + fade + off;
        bool solid;
        if (t < on)                                 solid = true;
        else if (t < fadeInStart)                   solid = false;
        else                                        solid = true;

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
        if (_renderer != null) _renderer.enabled = solid;
    }

    bool _wasSolid = true;
    bool _wasSolidInitialized;
}
