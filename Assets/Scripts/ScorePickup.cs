using UnityEngine;

/// <summary>
/// Optional collectible reward. Wooden stars grant a shot heart plus bonus points.
/// Collected objects stay available for Undo until the level unloads.
/// </summary>
public class ScorePickup : MonoBehaviour
{
    [Tooltip("Bonus points added to the live score on collection.")]
    public int bonusPoints = 75;
    [Tooltip("Wooden reward stars also grant one extra shot heart.")]
    public bool grantsHeart;
    [Tooltip("Scale multiplier at collection for the pop effect.")]
    public float popScale = 2.5f;
    [Tooltip("Seconds for the pop+fade animation.")]
    public float popDuration = 0.3f;
    [Tooltip("Speed of the idle bob + spin animation.")]
    public float idleSpinSpeed = 90f;
    public float idleBobFreq = 2.2f;
    public float idleBobAmplitude = 0.08f;

    Vector3 _basePos;
    Vector3 _baseScale;
    bool _collected;
    float _phase;

    void Awake()
    {
        _basePos = transform.localPosition;
        _baseScale = transform.localScale;
        // Random phase so multiple pickups don't bob in sync
        _phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        if (_collected) return;
        // Idle bob + spin
        float t = Time.time * idleBobFreq + _phase;
        transform.localPosition = _basePos + new Vector3(0f, Mathf.Sin(t) * idleBobAmplitude, 0f);
        transform.Rotate(0f, idleSpinSpeed * Time.deltaTime, 0f, Space.Self);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        var rb = other.attachedRigidbody;
        if (rb == null || (rb.GetComponent<PuckController>() == null && rb.GetComponent<TutorialPracticePuck>() == null)) return;
        var practicePuck=rb.GetComponent<TutorialPracticePuck>();
        if(practicePuck!=null) { practicePuck.practice.UseMechanic(); gameObject.SetActive(false); return; }
        _collected = true;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.AddBonusScore(bonusPoints, transform.position);
            if(grantsHeart) LevelManager.Instance.AddStarHeart(transform.position);
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPickup(0);

        StartCoroutine(PopAndDie());
    }

    System.Collections.IEnumerator PopAndDie()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        float t = 0f;
        float duration = Mathf.Max(0.01f, popDuration);
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = Mathf.Lerp(1f, popScale, 1f - (1f - k) * (1f - k)); // easeOutQuad
            transform.localScale = _baseScale * s;
            yield return null;
        }
        gameObject.SetActive(false); // Retain it until level unload so Undo can restore it.
    }
    public bool Collected => _collected;
    public void RestoreCollected(bool collected)
    {
        StopAllCoroutines();
        _collected=collected;
        transform.localScale=_baseScale;
        var collider=GetComponent<Collider>(); if(collider!=null) collider.enabled=!collected;
        gameObject.SetActive(!collected);
    }
}
