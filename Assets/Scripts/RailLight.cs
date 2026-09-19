using UnityEngine;

/// <summary>
/// Tracks which authored rail/wall has been hit. LevelManager counts each
/// unique rail for star ratings. Visual materials stay fully prefab-authored;
/// this component never instances or mutates them at runtime.
/// </summary>
[DisallowMultipleComponent]
public class RailLight : MonoBehaviour
{
    [Tooltip("Authored rail renderer reference. No runtime material is created.")]
    public Renderer targetRenderer;

    [Tooltip("Legacy authoring value retained for existing level prefabs.")]
    public Color litEmission = new Color(5f, 5f, 5.5f);

    [Tooltip("Legacy authoring value retained for existing level prefabs.")]
    public float litBoost = 1.6f;

    [Tooltip("Retains the gameplay flash timer without changing authored materials.")]
    public bool flashOnHit = true;
    public float flashMultiplier = 1.8f;
    public float flashDuration = 0.18f;

    float _flashTime;

    public bool IsLit { get; private set; }

    void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null) return;

        // The rail's appearance is fully authored in its prefab/material.
        // Runtime only records hit state for scoring and timing.
    }

    /// <summary>Called by LevelManager on first wall contact.</summary>
    public bool LightUp()
    {
        if (IsLit) { TriggerFlash(); return false; }
        IsLit = true;
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
        _flashTime = 0f;
    }

    void Update()
    {
        if (_flashTime > 0f)
            _flashTime -= Time.deltaTime;
    }
}
