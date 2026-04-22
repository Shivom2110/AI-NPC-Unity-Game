using System;

/// <summary>
/// Synchronous event hub for every combat subsystem.
/// All communication between BossAIController, ComboTracker, DifficultyEngine,
/// FightProgressionManager, and PlayerCombatController flows through here.
/// No system holds a direct component reference to another — subscribe, don't couple.
/// </summary>
/// <remarks>
/// Call <see cref="ClearAll"/> from a scene-lifecycle manager on scene unload
/// to prevent stale delegate references across scene loads.
/// </remarks>
public static class CombatEventBus
{
    // ── Player ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired each time the player executes an attack.
    /// Parameters: (attackType, landed, rawDamage)
    /// </summary>
    public static event Action<PlayerAttackType, bool, float> OnPlayerAttack;

    /// <summary>
    /// Fired when the player attempts a parry (Q key).
    /// Parameters: (success, reactionMs — time from hitbox open to key press)
    /// </summary>
    public static event Action<bool, float> OnPlayerParry;

    /// <summary>
    /// Fired when the player executes a dodge roll (double Space).
    /// Parameters: (success, reactionMs — time from hitbox open to roll input)
    /// </summary>
    public static event Action<bool, float> OnPlayerDodge;

    /// <summary>
    /// Fired whenever the player receives damage.
    /// Parameters: (damage, sourceAttackName)
    /// </summary>
    public static event Action<float, string> OnPlayerDamaged;

    /// <summary>Fired when player HP reaches zero.</summary>
    public static event Action OnPlayerDied;

    // ── Boss ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired when the boss commits to an attack and its telegraph animation begins.
    /// Systems should prepare visual/audio cues here — the hitbox is NOT active yet.
    /// Parameters: (attack definition, scaled telegraph duration in seconds)
    /// </summary>
    public static event Action<BossAttack, float> OnBossAttackTelegraph;

    /// <summary>
    /// Fired the moment the boss hitbox activates (telegraph over).
    /// ComboTracker timestamps this to compute parry/dodge reaction times.
    /// Parameter: attack definition (IsParryable determines which window opens)
    /// </summary>
    public static event Action<BossAttack> OnBossAttackHitbox;

    /// <summary>
    /// Fired when a boss attack deals damage (player failed to react in time).
    /// Parameter: final damage value after all multipliers.
    /// </summary>
    public static event Action<float> OnBossAttackLanded;

    /// <summary>
    /// Fired when the boss transitions between HP phases (1 → 2 → 3 → 4).
    /// Parameter: new phase index (1–4).
    /// </summary>
    public static event Action<int> OnBossPhaseChanged;

    /// <summary>Fired when boss HP reaches zero.</summary>
    public static event Action OnBossDied;

    /// <summary>
    /// Fired when the boss attack hitbox phase closes (window expired, attack landed, or
    /// attack was interrupted by a heavy player hit during telegraph).
    /// UI uses this to hide the parry/dodge window indicator.
    /// </summary>
    public static event Action OnBossAttackEnded;

    // ── Skill & Difficulty ────────────────────────────────────────────────────

    /// <summary>
    /// Fired by ComboTracker when SkillScore shifts by more than 0.5 points.
    /// Parameters: (previousScore, newScore)
    /// </summary>
    public static event Action<float, float> OnSkillScoreChanged;

    /// <summary>
    /// Fired by DifficultyEngine after computing a new DifficultySettings snapshot.
    /// BossAIController subscribes to apply the new values immediately.
    /// </summary>
    public static event Action<DifficultySettings> OnDifficultyAdjusted;

    // ── Fight State ───────────────────────────────────────────────────────────

    /// <summary>
    /// Fired by FightProgressionManager when heat mode starts or ends.
    /// Heat mode activates after N consecutive successful parries, granting bonus player damage.
    /// Parameter: true = activated, false = deactivated.
    /// </summary>
    public static event Action<bool> OnHeatModeChanged;

    /// <summary>
    /// Fired by FightProgressionManager when hidden assist starts or ends.
    /// Assist silently reduces boss damage when the player is critically low for too long.
    /// Parameter: true = activated, false = deactivated.
    /// </summary>
    public static event Action<bool> OnAssistModeChanged;

    /// <summary>
    /// Fired when the fight concludes (either combatant dies).
    /// Parameters: (playerWon, fightDurationSeconds, finalSkillScore 0–100)
    /// </summary>
    public static event Action<bool, float, float> OnFightEnded;

    // ── Fire helpers (null-safe, one-liner calls) ─────────────────────────────

    public static void FirePlayerAttack(PlayerAttackType t, bool landed, float dmg)
        => OnPlayerAttack?.Invoke(t, landed, dmg);

    public static void FirePlayerParry(bool success, float reactionMs)
        => OnPlayerParry?.Invoke(success, reactionMs);

    public static void FirePlayerDodge(bool success, float reactionMs)
        => OnPlayerDodge?.Invoke(success, reactionMs);

    public static void FirePlayerDamaged(float dmg, string src)
        => OnPlayerDamaged?.Invoke(dmg, src);

    public static void FirePlayerDied()
        => OnPlayerDied?.Invoke();

    public static void FireBossAttackTelegraph(BossAttack atk, float telegraphDur)
        => OnBossAttackTelegraph?.Invoke(atk, telegraphDur);

    public static void FireBossAttackHitbox(BossAttack atk)
        => OnBossAttackHitbox?.Invoke(atk);

    public static void FireBossAttackLanded(float dmg)
        => OnBossAttackLanded?.Invoke(dmg);

    public static void FireBossPhaseChanged(int phase)
        => OnBossPhaseChanged?.Invoke(phase);

    public static void FireBossDied()
        => OnBossDied?.Invoke();

    public static void FireBossAttackEnded()
        => OnBossAttackEnded?.Invoke();

    public static void FireSkillScoreChanged(float oldScore, float newScore)
        => OnSkillScoreChanged?.Invoke(oldScore, newScore);

    public static void FireDifficultyAdjusted(DifficultySettings s)
        => OnDifficultyAdjusted?.Invoke(s);

    public static void FireHeatModeChanged(bool active)
        => OnHeatModeChanged?.Invoke(active);

    public static void FireAssistModeChanged(bool active)
        => OnAssistModeChanged?.Invoke(active);

    public static void FireFightEnded(bool won, float dur, float skill)
        => OnFightEnded?.Invoke(won, dur, skill);

    // ── Cleanup ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Removes all subscribers from every event.
    /// Call this on scene unload to prevent ghost delegates from destroyed objects.
    /// </summary>
    public static void ClearAll()
    {
        OnPlayerAttack        = null;
        OnPlayerParry         = null;
        OnPlayerDodge         = null;
        OnPlayerDamaged       = null;
        OnPlayerDied          = null;
        OnBossAttackTelegraph = null;
        OnBossAttackHitbox    = null;
        OnBossAttackLanded    = null;
        OnBossPhaseChanged    = null;
        OnBossDied            = null;
        OnBossAttackEnded     = null;
        OnSkillScoreChanged   = null;
        OnDifficultyAdjusted  = null;
        OnHeatModeChanged     = null;
        OnAssistModeChanged   = null;
        OnFightEnded          = null;
    }
}
