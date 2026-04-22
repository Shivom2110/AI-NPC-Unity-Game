using System.Collections;
using UnityEngine;

/// <summary>
/// Attaches to the camera GameObject. Shakes on boss hits, player damage, and phase transitions.
/// Self-bootstraps — no Inspector wiring needed.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 _originOffset;
    private Coroutine _shakeRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // Attach to the main camera when it exists
        Camera cam = Camera.main;
        if (cam == null) return;
        if (cam.GetComponent<CameraShake>() != null) return;
        cam.gameObject.AddComponent<CameraShake>();
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        CombatEventBus.OnBossAttackLanded   += OnBossHit;
        CombatEventBus.OnPlayerDied         += OnPlayerDied;
        CombatEventBus.OnBossPhaseChanged   += OnPhaseChanged;
    }

    private void OnDisable()
    {
        CombatEventBus.OnBossAttackLanded   -= OnBossHit;
        CombatEventBus.OnPlayerDied         -= OnPlayerDied;
        CombatEventBus.OnBossPhaseChanged   -= OnPhaseChanged;
    }

    // ── Event callbacks ────────────────────────────────────────────────────────

    private void OnBossHit(float damage)
    {
        // Scale shake magnitude with damage
        float mag = Mathf.Clamp(damage / 40f, 0.05f, 0.35f);
        Shake(mag, 0.25f);
    }

    private void OnPlayerDied()       => Shake(0.45f, 0.6f);
    private void OnPhaseChanged(int _) => Shake(0.5f, 0.5f);

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>Shakes the camera with given magnitude and duration.</summary>
    public void Shake(float magnitude, float duration)
    {
        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(ShakeRoutine(magnitude, duration));
    }

    // ── Internals ──────────────────────────────────────────────────────────────

    private IEnumerator ShakeRoutine(float magnitude, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float dampened = magnitude * (1f - progress);   // linear decay

            Vector3 offset = Random.insideUnitSphere * dampened;
            offset.z = 0f;   // keep Z clean for perspective cameras
            transform.localPosition = _originOffset + offset;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localPosition = _originOffset;
        _shakeRoutine = null;
    }
}
