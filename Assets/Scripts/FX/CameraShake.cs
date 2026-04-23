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
        _originOffset = transform.localPosition;
    }

    private void OnEnable()
    {
        CombatEventBus.OnBossAttackLanded += OnBossHit;
        CombatEventBus.OnPlayerDied       += OnPlayerDied;
        CombatEventBus.OnBossPhaseChanged += OnPhaseChanged;
        CombatEventBus.OnPlayerDamaged    += OnPlayerDamaged;
        CombatEventBus.OnBossRoar         += OnBossRoared;
        CombatEventBus.OnBossSecondWind   += OnBossSecondWind;
    }

    private void OnDisable()
    {
        CombatEventBus.OnBossAttackLanded -= OnBossHit;
        CombatEventBus.OnPlayerDied       -= OnPlayerDied;
        CombatEventBus.OnBossPhaseChanged -= OnPhaseChanged;
        CombatEventBus.OnPlayerDamaged    -= OnPlayerDamaged;
        CombatEventBus.OnBossRoar         -= OnBossRoared;
        CombatEventBus.OnBossSecondWind   -= OnBossSecondWind;
    }

    // ── Event callbacks ────────────────────────────────────────────────────────

    private void OnBossHit(float damage)
    {
        float mag = Mathf.Clamp(damage / 40f, 0.05f, 0.40f);
        float dur = Mathf.Clamp(damage / 30f, 0.20f, 0.45f);
        Shake(mag, dur);
    }

    private void OnPlayerDamaged(float damage, string attackType)
    {
        float mag = Mathf.Clamp(damage / 34f, 0.08f, 0.42f);
        float dur = Mathf.Clamp(damage / 28f, 0.18f, 0.40f);
        Shake(mag, dur);
    }

    private void OnPlayerDied()        => Shake(0.50f, 0.65f);
    private void OnPhaseChanged(int _) => Shake(0.55f, 0.55f);
    private void OnBossRoared()        => Shake(0.45f, 0.75f);
    private void OnBossSecondWind()    => Shake(0.60f, 0.90f);

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
            float dampened = magnitude * Mathf.Pow(1f - progress, 2f);  // quadratic — hard snap, smooth tail

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
