using UnityEngine;

/// <summary>
/// Handles dodge roll stamina, iframe timing, and precision grading.
/// </summary>
public class RollSystem : MonoBehaviour
{
    public static RollSystem Instance { get; private set; }

    [Header("Roll Settings")]
    [SerializeField] private float rollCost = 20f;
    [SerializeField] private float staminaRegenPerSecond = 10f;
    [SerializeField] private float baseMaxStamina = 100f;
    [SerializeField] private float iframeDuration = 0.4f;

    [Header("Timing Windows")]
    [SerializeField] private float lateGraceSeconds = 0.05f;

    private float maxStamina;
    private float currentStamina;
    private float iframeEndTime;
    private float telegraphStartTime = -1f;
    private float impactTime = -1f;
    private BossAttack currentAttack;

    /// <summary>Returns the player's current stamina.</summary>
    public float CurrentStamina => currentStamina;

    /// <summary>Returns the player's max stamina.</summary>
    public float MaxStamina => maxStamina;

    /// <summary>Returns whether the player is currently inside roll iframes.</summary>
    public bool IsInvincible => Time.time < iframeEndTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        maxStamina = baseMaxStamina;
        currentStamina = maxStamina;
    }

    private void OnEnable()
    {
        CombatEventSystem.OnBossAttackStart += HandleBossAttackStart;
        CombatEventSystem.OnDifficultyAdjusted += HandleDifficultyAdjusted;
        CombatEventBus.OnBossAttackEnded += HandleAttackEnded;
        CombatEventBus.OnBossDied += HandleAttackEnded;
        CombatEventBus.OnPlayerDied += HandleAttackEnded;
    }

    private void OnDisable()
    {
        CombatEventSystem.OnBossAttackStart -= HandleBossAttackStart;
        CombatEventSystem.OnDifficultyAdjusted -= HandleDifficultyAdjusted;
        CombatEventBus.OnBossAttackEnded -= HandleAttackEnded;
        CombatEventBus.OnBossDied -= HandleAttackEnded;
        CombatEventBus.OnPlayerDied -= HandleAttackEnded;
    }

    private void Update()
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenPerSecond * Time.deltaTime);
    }

    /// <summary>
    /// Attempts to spend stamina and execute a roll.
    /// </summary>
    public bool TryStartRoll(out RollResolution resolution)
    {
        resolution = new RollResolution
        {
            success = false,
            timingPrecisionMs = 999f,
            playerDamageScale = 1f,
            grade = RollTimingGrade.Miss
        };

        if (currentStamina < rollCost)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play(AudioManager.Instance.sfxOutOfStamina, 0.9f);
            return false;
        }

        currentStamina -= rollCost;
        iframeEndTime = Time.time + iframeDuration;
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.GrantIframes(iframeDuration);

        if (telegraphStartTime < 0f || currentAttack.isUndodgeable)
        {
            CombatEventSystem.RaisePlayerDodge(false, resolution.timingPrecisionMs);
            return true;
        }

        float rollStart = Time.time;
        float rollEnd = rollStart + iframeDuration;
        float deltaToImpact = impactTime - rollStart;
        resolution.timingPrecisionMs = Mathf.Abs(deltaToImpact) * 1000f;

        if (rollStart > impactTime + lateGraceSeconds)
        {
            resolution.grade = RollTimingGrade.Late;
        }
        else if (rollEnd < impactTime)
        {
            resolution.grade = RollTimingGrade.Early;
        }
        else
        {
            resolution.grade = RollTimingGrade.Perfect;
            resolution.success = true;
            resolution.playerDamageScale = 0f;
        }

        CombatEventSystem.RaisePlayerDodge(resolution.success, resolution.timingPrecisionMs);
        CombatEventBus.FirePlayerDodge(resolution.success, resolution.timingPrecisionMs);

        if (resolution.success && AudioManager.Instance != null)
            AudioManager.Instance.Play(AudioManager.Instance.sfxDodgeRoll, 0.95f);

        return true;
    }

    /// <summary>
    /// Applies a new max stamina value from difficulty scaling.
    /// </summary>
    public void SetMaxStamina(float newMaxStamina, bool preservePercentage)
    {
        float clampedMax = Mathf.Max(20f, newMaxStamina);
        float healthPercent = maxStamina <= 0f ? 1f : currentStamina / maxStamina;

        maxStamina = clampedMax;
        currentStamina = preservePercentage ? maxStamina * healthPercent : Mathf.Min(currentStamina, maxStamina);
    }

    private void HandleBossAttackStart(BossAttack attack, float telegraphDuration)
    {
        currentAttack = attack;
        telegraphStartTime = Time.time;
        impactTime = Time.time + telegraphDuration;
    }

    private void HandleDifficultyAdjusted(DifficultySettings settings)
    {
        SetMaxStamina(settings.playerMaxStamina, true);
    }

    private void HandleAttackEnded()
    {
        telegraphStartTime = -1f;
        impactTime = -1f;
        currentAttack = default;
    }
}
