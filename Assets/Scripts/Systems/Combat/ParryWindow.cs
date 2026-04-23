using UnityEngine;

/// <summary>
/// Evaluates parry attempts against the currently telegraphed boss attack.
/// </summary>
public class ParryWindow : MonoBehaviour
{
    public static ParryWindow Instance { get; private set; }

    [Header("Parry Timing Windows (ms)")]
    [SerializeField] private float perfectParryMs = 200f;
    [SerializeField] private float goodParryMs = 400f;
    [SerializeField] private float lateParryMs = 600f;

    [Header("Skill Scaling")]
    [SerializeField] [Range(0.4f, 1f)] private float highSkillWindowScale = 0.6f;

    private BossAttack currentAttack;
    private float telegraphStartTime = -1f;
    private float impactTime = -1f;
    private int currentAttackSerial;
    private int lastResolvedAttackSerial = -1;

    public bool HasActiveTelegraph => telegraphStartTime >= 0f && Time.time <= impactTime + 0.1f;
    public BossAttack CurrentAttack => currentAttack;
    public float TelegraphStartTime => telegraphStartTime;
    public float ImpactTime => impactTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        CombatEventSystem.OnBossAttackStart += HandleBossAttackStart;
        CombatEventBus.OnBossAttackEnded += HandleAttackEnded;
        CombatEventBus.OnBossDied += HandleAttackEnded;
        CombatEventBus.OnPlayerDied += HandleAttackEnded;
    }

    private void OnDisable()
    {
        CombatEventSystem.OnBossAttackStart -= HandleBossAttackStart;
        CombatEventBus.OnBossAttackEnded -= HandleAttackEnded;
        CombatEventBus.OnBossDied -= HandleAttackEnded;
        CombatEventBus.OnPlayerDied -= HandleAttackEnded;
    }

    /// <summary>
    /// Resolves the current parry attempt and publishes its timing.
    /// </summary>
    public ParryResolution ResolveParryAttempt()
    {
        ParryResolution resolution = new ParryResolution
        {
            success = false,
            shouldStaggerBoss = false,
            timingPrecisionMs = 999f,
            playerDamageScale = 1f,
            counterDamageMultiplier = 0f,
            grade = ParryTimingGrade.Miss
        };

        if (telegraphStartTime < 0f || lastResolvedAttackSerial == currentAttackSerial)
        {
            CombatEventSystem.RaisePlayerParry(false, resolution.timingPrecisionMs);
            return resolution;
        }

        lastResolvedAttackSerial = currentAttackSerial;

        // Check both naming conventions — BossAIController uses PascalCase, catalogue uses camelCase.
        bool parryable = currentAttack.IsParryable || currentAttack.isParryable;
        if (!parryable || currentAttack.guaranteedNoCounter)
        {
            CombatEventSystem.RaisePlayerParry(false, resolution.timingPrecisionMs);
            return resolution;
        }

        float timeToImpactMs = Mathf.Max(0f, (impactTime - Time.time) * 1000f);
        resolution.timingPrecisionMs = timeToImpactMs;

        float windowScale = GetSkillWindowScale();
        float perfectThreshold = perfectParryMs * windowScale;
        float goodThreshold = goodParryMs * windowScale;
        float lateThreshold = lateParryMs * windowScale;

        if (Time.time > impactTime + 0.05f)
        {
            CombatEventSystem.RaisePlayerParry(false, timeToImpactMs);
            return resolution;
        }

        if (timeToImpactMs <= perfectThreshold)
        {
            resolution.success = true;
            resolution.shouldStaggerBoss = true;
            resolution.playerDamageScale = 0f;
            resolution.counterDamageMultiplier = 2f;
            resolution.grade = ParryTimingGrade.Perfect;
        }
        else if (timeToImpactMs <= goodThreshold)
        {
            resolution.success = true;
            resolution.shouldStaggerBoss = true;
            resolution.playerDamageScale = 0.2f;
            resolution.counterDamageMultiplier = 1.25f;
            resolution.grade = ParryTimingGrade.Good;
        }
        else if (timeToImpactMs <= lateThreshold)
        {
            resolution.success = false;
            resolution.shouldStaggerBoss = false;
            resolution.playerDamageScale = 0.6f;
            resolution.counterDamageMultiplier = 0f;
            resolution.grade = ParryTimingGrade.Late;
        }

        CombatEventSystem.RaisePlayerParry(resolution.success, resolution.timingPrecisionMs);
        return resolution;
    }

    private void HandleBossAttackStart(BossAttack attack, float telegraphDuration)
    {
        currentAttack = attack;
        telegraphStartTime = Time.time;
        impactTime = Time.time + telegraphDuration;
        currentAttackSerial++;
    }

    private void HandleAttackEnded()
    {
        telegraphStartTime = -1f;
        impactTime = -1f;
    }

    private float GetSkillWindowScale()
    {
        float skillScore = CombatTracker.Instance != null
            ? CombatTracker.Instance.CurrentSnapshot.skillScore
            : 50f;

        return Mathf.Lerp(1f, highSkillWindowScale, skillScore / 100f);
    }
}
