using UnityEngine;

/// <summary>
/// Manages the emotional arc of a single boss fight through two invisible systems:
///
/// <b>Heat Mode</b> — rewards skilled play.
/// After <see cref="parriesToActivateHeat"/> consecutive successful parries the player
/// enters Heat Mode: +<see cref="heatDamageBonus"/>% damage for <see cref="heatDuration"/> s.
/// Each additional parry while in Heat Mode extends the timer.
/// A missed parry or boss phase transition resets the streak.
///
/// <b>Hidden Assist</b> — prevents frustration without showing mercy.
/// If the player's HP stays below <see cref="assistHPThreshold"/> for more than
/// <see cref="assistLingerSeconds"/> seconds, boss damage is quietly reduced by
/// <see cref="assistDamageReduction"/>. The player never sees this — it just feels
/// like they caught a break.
///
/// Both systems expose multipliers that <see cref="BossAIController"/> reads every frame.
/// </summary>
public class LegacyFightProgressionManager : MonoBehaviour
{
    public static LegacyFightProgressionManager Instance { get; private set; }

    // ── Heat Mode ──────────────────────────────────────────────────────────────
    [Header("Heat Mode")]
    [SerializeField, Range(2, 8)]     private int   parriesToActivateHeat = 3;
    [SerializeField, Range(5f, 30f)]  private float heatDuration          = 10f;
    [SerializeField, Range(0f, 0.5f)] private float heatDamageBonus       = 0.25f;

    // ── Hidden Assist ──────────────────────────────────────────────────────────
    [Header("Hidden Assist (never visible to player)")]
    [SerializeField, Range(0.1f, 0.5f)] private float assistHPThreshold    = 0.30f;
    [SerializeField, Range(5f,  45f)]   private float assistLingerSeconds   = 20f;
    [SerializeField, Range(0f,  0.3f)]  private float assistDamageReduction = 0.15f;

    // ── Public state ───────────────────────────────────────────────────────────

    /// <summary>True while the player is in Heat Mode.</summary>
    public bool HeatActive   { get; private set; }

    /// <summary>True while hidden assist is softening boss damage.</summary>
    public bool AssistActive { get; private set; }

    /// <summary>
    /// Multiply outgoing player damage by this value.
    /// 1.0 normally; higher during Heat Mode.
    /// </summary>
    public float PlayerDamageMultiplier => HeatActive ? 1f + heatDamageBonus : 1f;

    /// <summary>
    /// Multiply incoming boss damage by this value.
    /// 1.0 normally; lower during hidden assist.
    /// </summary>
    public float BossDamageMultiplier => AssistActive ? 1f - assistDamageReduction : 1f;

    // ── Private state ──────────────────────────────────────────────────────────
    private int   _streak;
    private float _heatEndTime;
    private float _lowHPSince   = -1f;
    private float _fightStart;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        CombatEventBus.OnPlayerParry      += OnParry;
        CombatEventBus.OnBossPhaseChanged += OnBossPhaseChanged;
        CombatEventBus.OnBossDied         += OnBossDefeated;
        CombatEventBus.OnPlayerDied       += OnPlayerDefeated;
    }

    private void OnDisable()
    {
        CombatEventBus.OnPlayerParry      -= OnParry;
        CombatEventBus.OnBossPhaseChanged -= OnBossPhaseChanged;
        CombatEventBus.OnBossDied         -= OnBossDefeated;
        CombatEventBus.OnPlayerDied       -= OnPlayerDefeated;
    }

    private void Update()
    {
        TickHeatMode();
        TickAssistMode();
    }

    // ── Public ─────────────────────────────────────────────────────────────────

    /// <summary>Call when the boss fight officially begins (after entrance roar).</summary>
    public void StartFight()
    {
        _fightStart  = Time.time;
        _streak      = 0;
        HeatActive   = false;
        AssistActive = false;
        _lowHPSince  = -1f;
        Debug.Log("[FightProgressionManager] Fight started.");
    }

    // ── Heat Mode ──────────────────────────────────────────────────────────────

    private void OnParry(bool success, float _)
    {
        if (!success)
        {
            _streak = 0;
            return;
        }

        _streak++;

        if (_streak >= parriesToActivateHeat)
        {
            if (!HeatActive)
            {
                HeatActive = true;
                CombatEventBus.FireHeatModeChanged(true);
                Debug.Log($"[FightProgressionManager] HEAT MODE ON — {_streak} parry streak! " +
                          $"+{heatDamageBonus * 100:F0}% damage for {heatDuration:F0}s");
            }
            // Extend on every additional parry while hot.
            _heatEndTime = Time.time + heatDuration;
        }
    }

    private void TickHeatMode()
    {
        if (!HeatActive) return;
        if (Time.time < _heatEndTime) return;

        HeatActive = false;
        _streak    = 0;
        CombatEventBus.FireHeatModeChanged(false);
        Debug.Log("[FightProgressionManager] Heat mode ended.");
    }

    // ── Hidden Assist ──────────────────────────────────────────────────────────

    private void TickAssistMode()
    {
        if (PlayerHealth.Instance == null) return;

        float ratio = PlayerHealth.Instance.CurrentHealth / PlayerHealth.Instance.MaxHealth;

        if (ratio < assistHPThreshold)
        {
            if (_lowHPSince < 0f) _lowHPSince = Time.time;

            if (!AssistActive && Time.time - _lowHPSince >= assistLingerSeconds)
            {
                AssistActive = true;
                CombatEventBus.FireAssistModeChanged(true);
                Debug.Log("[FightProgressionManager] Hidden assist activated.");
            }
        }
        else
        {
            _lowHPSince = -1f;

            if (AssistActive)
            {
                AssistActive = false;
                CombatEventBus.FireAssistModeChanged(false);
                Debug.Log("[FightProgressionManager] Hidden assist deactivated.");
            }
        }
    }

    // ── Phase changes ──────────────────────────────────────────────────────────

    private void OnBossPhaseChanged(int phase)
    {
        // Phase transition resets the parry streak — boss changes rhythm.
        if (HeatActive)
        {
            HeatActive = false;
            CombatEventBus.FireHeatModeChanged(false);
        }
        _streak = 0;
        Debug.Log($"[FightProgressionManager] Phase {phase} — parry streak reset.");
    }

    // ── Fight end ──────────────────────────────────────────────────────────────

    private void OnBossDefeated()  => EndFight(true);
    private void OnPlayerDefeated() => EndFight(false);

    private void EndFight(bool playerWon)
    {
        float dur   = Time.time - _fightStart;
        float skill = ComboTracker.Instance != null ? ComboTracker.Instance.SkillScore : 0f;
        CombatEventBus.FireFightEnded(playerWon, dur, skill);
        Debug.Log($"[FightProgressionManager] Fight ended — " +
                  $"{(playerWon ? "VICTORY" : "DEFEAT")} in {dur:F1}s | Skill {skill:F0}/100");
    }
}
