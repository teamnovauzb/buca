using UnityEngine;

/// <summary>
/// Small glowing orb on the playfield. When the puck touches it,
/// awards bonus score, plays a brief scale-pop animation, then
/// self-destroys. Placed in level prefabs as small yellow orbs.
///
/// Orbs are pure carrot for the player — they don't block the puck,
/// don't kill, just reward skill shots that pass through them.
/// </summary>
public class ScorePickup : MonoBehaviour
{
    [Tooltip("Bonus points added to the live score on collection.")]
    public int bonusPoints = 150;
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
        if (other.attachedRigidbody == null) return; // only puck
        _collected = true;

        if (LevelManager.Instance != null)
            LevelManager.Instance.AddBonusScore(bonusPoints, transform.position);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPickup(0);

        StartCoroutine(PopAndDie());
    }

    System.Collections.IEnumerator PopAndDie()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        var mr = GetComponent<Renderer>();
        Material matInstance = mr != null ? mr.material : null;

        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / popDuration);
            float s = Mathf.Lerp(1f, popScale, 1f - (1f - k) * (1f - k)); // easeOutQuad
            transform.localScale = _baseScale * s;
            if (matInstance != null && matInstance.HasProperty("_BaseColor"))
            {
                var c = matInstance.GetColor("_BaseColor");
                c.a = 1f - k;
                matInstance.SetColor("_BaseColor", c);
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
