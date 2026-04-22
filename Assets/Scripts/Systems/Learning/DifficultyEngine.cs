using UnityEngine;

/// <summary>
/// Translates the player's live <see cref="ComboTracker.SkillScore"/> (0–100) into a
/// <see cref="DifficultySettings"/> struct consumed by every combat system.
///
/// Every parameter is driven by an <see cref="AnimationCurve"/> — fully tweakable in the
/// Inspector without touching code.  The X-axis of every curve is SkillScore 0–100.
///
/// Subscribe to <see cref="CombatEventBus.OnDifficultyAdjusted"/> to receive updates,
/// or poll <see cref="Current"/> directly.
/// </summary>
public class DifficultyEngine : MonoBehaviour
{
    public static DifficultyEngine Instance { get; private set; }

    // ── Boss damage ────────────────────────────────────────────────────────────
    [Header("Boss Damage (× base damage)")]
    [SerializeField] private AnimationCurve bossDamageCurve = new AnimationCurve(
        new Keyframe(  0f, 0.50f, 0f, 0f),
        new Keyframe( 20f, 0.80f, 0f, 0f),
        new Keyframe( 40f, 1.00f, 0f, 0f),
        new Keyframe( 60f, 1.30f, 0f, 0f),
        new Keyframe( 80f, 1.50f, 0f, 0f),
        new Keyframe(100f, 1.70f, 0f, 0f));

    // ── Player damage ──────────────────────────────────────────────────────────
    [Header("Player Damage (× base damage)  — reward mastery")]
    [SerializeField] private AnimationCurve playerDamageCurve = new AnimationCurve(
        new Keyframe(  0f, 0.70f, 0f, 0f),
        new Keyframe( 40f, 1.00f, 0f, 0f),
        new Keyframe( 80f, 1.30f, 0f, 0f),
        new Keyframe(100f, 1.50f, 0f, 0f));

    // ── Attack interval ────────────────────────────────────────────────────────
    [Header("Attack Interval (seconds between attacks)")]
    [SerializeField] private AnimationCurve attackIntervalMinCurve = new AnimationCurve(
        new Keyframe(  0f, 3.5f, 0f, 0f),
        new Keyframe( 50f, 1.8f, 0f, 0f),
        new Keyframe(100f, 1.0f, 0f, 0f));

    [SerializeField] private AnimationCurve attackIntervalMaxCurve = new AnimationCurve(
        new Keyframe(  0f, 5.5f, 0f, 0f),
        new Keyframe( 50f, 3.0f, 0f, 0f),
        new Keyframe(100f, 2.0f, 0f, 0f));

    // ── Reaction windows ───────────────────────────────────────────────────────
    [Header("Parry Window (seconds the window stays open)")]
    [SerializeField] private AnimationCurve parryWindowCurve = new AnimationCurve(
        new Keyframe(  0f, 1.00f, 0f, 0f),
        new Keyframe( 50f, 0.60f, 0f, 0f),
        new Keyframe(100f, 0.30f, 0f, 0f));

    [Header("Dodge Window (seconds the window stays open)")]
    [SerializeField] private AnimationCurve dodgeWindowCurve = new AnimationCurve(
        new Keyframe(  0f, 1.20f, 0f, 0f),
        new Keyframe( 50f, 0.75f, 0f, 0f),
        new Keyframe(100f, 0.40f, 0f, 0f));

    // ── Parryable ratio ────────────────────────────────────────────────────────
    [Header("Parryable Attack Ratio (1 = all parryable, 0 = all must-dodge)")]
    [SerializeField] private AnimationCurve parryableRatioCurve = new AnimationCurve(
        new Keyframe(  0f, 0.80f, 0f, 0f),
        new Keyframe( 20f, 0.60f, 0f, 0f),
        new Keyframe( 40f, 0.40f, 0f, 0f),
        new Keyframe( 60f, 0.20f, 0f, 0f),
        new Keyframe( 80f, 0.10f, 0f, 0f),
        new Keyframe(100f, 0.05f, 0f, 0f));

    // ── Telegraph scale ────────────────────────────────────────────────────────
    [Header("Telegraph Scale (× base telegraph duration per attack)")]
    [SerializeField] private AnimationCurve telegraphScaleCurve = new AnimationCurve(
        new Keyframe(  0f, 1.40f, 0f, 0f),
        new Keyframe( 50f, 0.90f, 0f, 0f),
        new Keyframe(100f, 0.50f, 0f, 0f));

    // ── HP values ──────────────────────────────────────────────────────────────
    [Header("Player Max HP  (better player = less HP = higher risk/reward)")]
    [SerializeField] private AnimationCurve playerMaxHPCurve = new AnimationCurve(
        new Keyframe(  0f, 200f, 0f, 0f),
        new Keyframe( 20f, 175f, 0f, 0f),
        new Keyframe( 40f, 150f, 0f, 0f),
        new Keyframe( 60f, 125f, 0f, 0f),
        new Keyframe( 80f, 110f, 0f, 0f),
        new Keyframe(100f, 100f, 0f, 0f));

    [Header("Boss Max HP  (better player = tougher boss)")]
    [SerializeField] private AnimationCurve bossMaxHPCurve = new AnimationCurve(
        new Keyframe(  0f,  500f, 0f, 0f),
        new Keyframe( 20f,  750f, 0f, 0f),
        new Keyframe( 40f, 1000f, 0f, 0f),
        new Keyframe( 60f, 1400f, 0f, 0f),
        new Keyframe( 80f, 1700f, 0f, 0f),
        new Keyframe(100f, 2000f, 0f, 0f));

    // ── Edge multiplier ────────────────────────────────────────────────────────
    [Header("Edge — hidden boss advantage at elite skill")]
    [SerializeField, Range(50f, 100f)] private float edgeActivationScore  = 85f;
    [SerializeField, Range(1.0f, 1.5f)] private float edgeMultiplier      = 1.20f;

    /// <summary>Most recently computed settings. Cached for any system that prefers polling.</summary>
    public DifficultySettings Current { get; private set; }

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Apply(0f); // initialise with skill-floor values
    }

    private void OnEnable()  => CombatEventBus.OnSkillScoreChanged += OnSkillChanged;
    private void OnDisable() => CombatEventBus.OnSkillScoreChanged -= OnSkillChanged;

    private void OnSkillChanged(float _, float newScore) => Apply(newScore);

    // ── Core ───────────────────────────────────────────────────────────────────

    private void Apply(float skill)
    {
        float edge = skill >= edgeActivationScore ? edgeMultiplier : 1.0f;

        Current = new DifficultySettings
        {
            BossDamageMultiplier   = bossDamageCurve.Evaluate(skill) * edge,
            PlayerDamageMultiplier = playerDamageCurve.Evaluate(skill),
            AttackIntervalMin      = attackIntervalMinCurve.Evaluate(skill),
            AttackIntervalMax      = attackIntervalMaxCurve.Evaluate(skill),
            ParryWindowSeconds     = parryWindowCurve.Evaluate(skill),
            DodgeWindowSeconds     = dodgeWindowCurve.Evaluate(skill),
            ParryableRatio         = parryableRatioCurve.Evaluate(skill),
            TelegraphScale         = telegraphScaleCurve.Evaluate(skill),
            PlayerMaxHP            = playerMaxHPCurve.Evaluate(skill),
            BossMaxHP              = bossMaxHPCurve.Evaluate(skill),
            EdgeMultiplier         = edge,
        };

        CombatEventBus.FireDifficultyAdjusted(Current);

        Debug.Log($"[DifficultyEngine] Skill={skill:F0} → " +
                  $"BossDmg×{Current.BossDamageMultiplier:F2}  " +
                  $"PlrDmg×{Current.PlayerDamageMultiplier:F2}  " +
                  $"Interval={Current.AttackIntervalMin:F1}–{Current.AttackIntervalMax:F1}s  " +
                  $"ParryWin={Current.ParryWindowSeconds:F2}s  " +
                  $"Edge×{edge:F2}");
    }
}
