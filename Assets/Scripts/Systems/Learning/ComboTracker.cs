using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Tracks every player combat action and derives four real-time skill metrics:
/// <list type="bullet">
///   <item><b>SkillScore</b> (0–100) — weighted combination of parry precision, dodge precision,
///   damage ratio, and combo variety. Used by <see cref="DifficultyEngine"/> to scale the fight.</item>
///   <item><b>AggressionIndex</b> (0–1) — attacks per second over the last 10 s.</item>
///   <item><b>PatternPredictability</b> (0–1) — how repetitive the player's attack trigrams are.</item>
///   <item><b>PredictedNextAction</b> — most probable next attack label based on the last two actions.</item>
/// </list>
///
/// Metrics are recalculated every <see cref="metricRecalcInterval"/> seconds on a timer —
/// no per-frame allocations after warmup.  The attack ring buffer is a fixed-size array,
/// so GC pressure is zero in steady state.
/// </summary>
public class ComboTracker : MonoBehaviour
{
    public static ComboTracker Instance { get; private set; }

    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Combo Detection")]
    [SerializeField, Range(0.2f, 2.0f)] private float comboGapThreshold   = 0.8f;
    [SerializeField, Range(10,   60)]   private int   maxStoredAttacks     = 30;

    [Header("Skill Tracking")]
    [SerializeField, Range(1f, 10f)]    private float metricRecalcInterval = 5f;
    [SerializeField, Range(0.05f, 0.5f)]private float skillEmaAlpha        = 0.25f;
    [SerializeField, Range(4,   20)]    private int   timingHistorySize     = 10;

    // ── Ring buffer (fixed-size, zero GC after warmup) ────────────────────────
    private AttackEvent[] _ring;
    private int           _ringHead;   // next write index
    private int           _ringCount;  // valid entries [0, _ring.Length)

    // ── Timing history ────────────────────────────────────────────────────────
    // Positive value = reaction ms from window-open to key press.
    // –1 = the player missed the window entirely (counts as worst-case in scoring).
    private float[] _parryMs;
    private float[] _dodgeMs;
    private int     _parryHead, _parryCount;
    private int     _dodgeHead, _dodgeCount;

    // Set by OnBossHitboxOpened so ReportParry/Dodge can compute reaction time.
    private float _parryWindowOpenTime = -1f;
    private float _dodgeWindowOpenTime = -1f;

    // ── Damage rolling window (reset every 60 s) ──────────────────────────────
    private float _damageDealt, _damageTaken;
    private float _damageWindowStart;

    // ── Legacy list (kept for GetLastComboSlice / BossCounterLibrary) ─────────
    private readonly List<AttackEvent> _recent  = new List<AttackEvent>(32);
    private readonly List<AttackEvent> _current = new List<AttackEvent>(16);
    private int _lastComboLength;

    // ── Combo frequency (entropy-based variety score) ─────────────────────────
    private readonly Dictionary<string, int> _comboFreq = new Dictionary<string, int>(32);

    // ── Trigram pattern detection ─────────────────────────────────────────────
    // Sliding window of last 20 attack labels.
    private readonly Queue<string>           _actionWindow = new Queue<string>(24);
    // Key = "A>B>C", Value = occurrence count.
    private readonly Dictionary<string, int> _trigramFreq  = new Dictionary<string, int>(64);

    // ── Public metrics ────────────────────────────────────────────────────────
    /// <summary>Overall skill score 0–100. Drives DifficultyEngine.</summary>
    public float  SkillScore             { get; private set; }
    /// <summary>1–5 band (backwards-compat shim over SkillScore).</summary>
    public int    SkillLevel             => Mathf.Clamp(1 + Mathf.FloorToInt(SkillScore / 20f), 1, 5);
    /// <summary>Attack frequency 0–1 (1 = ≥10 attacks in last 10 s).</summary>
    public float  AggressionIndex        { get; private set; }
    /// <summary>0 = totally random, 1 = always the same trigram.</summary>
    public float  PatternPredictability  { get; private set; }
    /// <summary>Most probable next attack label; empty string if not yet confident.</summary>
    public string PredictedNextAction    { get; private set; } = string.Empty;

    private float  _nextRecalcTime;
    private string _lastComboSig = "None";

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _ring            = new AttackEvent[maxStoredAttacks];
        _parryMs         = new float[timingHistorySize];
        _dodgeMs         = new float[timingHistorySize];
        _damageWindowStart = Time.time;
    }

    private void OnEnable()
    {
        CombatEventBus.OnBossAttackHitbox += OnBossHitboxOpened;
        CombatEventBus.OnPlayerDamaged    += OnPlayerDamaged;
        CombatEventBus.OnPlayerAttack     += OnPlayerAttackLanded;
    }

    private void OnDisable()
    {
        CombatEventBus.OnBossAttackHitbox -= OnBossHitboxOpened;
        CombatEventBus.OnPlayerDamaged    -= OnPlayerDamaged;
        CombatEventBus.OnPlayerAttack     -= OnPlayerAttackLanded;
    }

    private void Update()
    {
        if (Time.time >= _nextRecalcTime)
        {
            _nextRecalcTime = Time.time + metricRecalcInterval;
            RecalculateMetrics();
        }
    }

    // ── Event handlers ─────────────────────────────────────────────────────────

    // Boss hitbox just opened — stamp the time so ReportParry/Dodge can diff against it.
    private void OnBossHitboxOpened(BossAttack atk)
    {
        if (atk.IsParryable) _parryWindowOpenTime = Time.time;
        else                 _dodgeWindowOpenTime  = Time.time;
    }

    private void OnPlayerDamaged(float dmg, string _)     => _damageTaken += dmg;
    private void OnPlayerAttackLanded(PlayerAttackType _, bool landed, float dmg)
    {
        if (landed) _damageDealt += dmg;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Record a player attack. Called by <see cref="PlayerCombatController"/> on every attack input.
    /// </summary>
    public void AddAttack(PlayerAttackType type, float timestamp)
    {
        // 1. Ring buffer write — O(1), no allocation.
        _ring[_ringHead] = new AttackEvent(type, timestamp);
        _ringHead        = (_ringHead + 1) % _ring.Length;
        _ringCount       = Mathf.Min(_ringCount + 1, _ring.Length);

        // 2. Legacy list (trimmed to maxStoredAttacks).
        _recent.Add(new AttackEvent(type, timestamp));
        if (_recent.Count > maxStoredAttacks) _recent.RemoveAt(0);

        // 3. Trigram — one dict lookup, not a full rebuild.
        string label = type.ToString();
        _actionWindow.Enqueue(label);
        if (_actionWindow.Count > 20) _actionWindow.Dequeue();
        UpdateTrigrams(label);

        // 4. Combo length change detection.
        int prevLen = _lastComboLength;
        RebuildCurrentCombo();

        if (_current.Count < prevLen && prevLen > 1)
        {
            // A combo just concluded — record its signature for variety scoring.
            string sig = BuildSignature(_current);
            _comboFreq.TryGetValue(sig, out int existing);
            _comboFreq[sig] = existing + 1;
        }

        _lastComboLength = _current.Count;
        _lastComboSig    = BuildSignature(_current);

        Debug.Log($"[ComboTracker] {_lastComboSig} | Skill {SkillScore:F0}/100 (L{SkillLevel}) " +
                  $"Aggr={AggressionIndex:F2} Pred='{PredictedNextAction}'");
    }

    /// <summary>
    /// Report the outcome of a parry attempt (Q key).
    /// Reaction time is computed automatically from when the boss last opened a parry window.
    /// </summary>
    public void ReportParry(bool success)
    {
        float ms = _parryWindowOpenTime > 0f
            ? Mathf.Max(0f, (Time.time - _parryWindowOpenTime) * 1000f)
            : 999f; // window wasn't open — counts as miss-timing

        _parryMs[_parryHead] = success ? ms : -1f;
        _parryHead           = (_parryHead + 1) % timingHistorySize;
        _parryCount          = Mathf.Min(_parryCount + 1, timingHistorySize);

        CombatEventBus.FirePlayerParry(success, ms);
        Debug.Log($"[ComboTracker] Parry {(success ? "HIT" : "MISS")} {ms:F0}ms | Skill {SkillScore:F0}");
    }

    /// <summary>
    /// Report the outcome of a dodge roll (double Space).
    /// Reaction time is computed from when the boss last opened a dodge window.
    /// </summary>
    public void ReportDodge(bool success)
    {
        float ms = _dodgeWindowOpenTime > 0f
            ? Mathf.Max(0f, (Time.time - _dodgeWindowOpenTime) * 1000f)
            : 999f;

        _dodgeMs[_dodgeHead] = success ? ms : -1f;
        _dodgeHead           = (_dodgeHead + 1) % timingHistorySize;
        _dodgeCount          = Mathf.Min(_dodgeCount + 1, timingHistorySize);

        CombatEventBus.FirePlayerDodge(success, ms);
        Debug.Log($"[ComboTracker] Dodge {(success ? "HIT" : "MISS")} {ms:F0}ms | Skill {SkillScore:F0}");
    }

    // ── Metric recalculation ───────────────────────────────────────────────────

    private void RecalculateMetrics()
    {
        float parryScore  = TimingScore(_parryMs, _parryCount);   // 0–1
        float dodgeScore  = TimingScore(_dodgeMs, _dodgeCount);   // 0–1
        float dmgRatio    = DamageRatioScore();                    // 0–1
        float variety     = ComboVarietyScore();                   // 0–1

        // Weighted target (0–100). Parry precision matters most — it's the hardest skill.
        float target = (parryScore * 0.38f +
                        dodgeScore * 0.27f +
                        dmgRatio   * 0.22f +
                        variety    * 0.13f) * 100f;

        float old = SkillScore;
        // Exponential moving average: fast reaction to improvement, smooth in steady state.
        SkillScore = Mathf.Lerp(old, target, skillEmaAlpha);

        AggressionIndex      = ComputeAggressionIndex();
        PatternPredictability = ComputePatternPredictability();

        // Reset rolling damage window every 60 s.
        if (Time.time - _damageWindowStart > 60f)
        {
            _damageDealt = _damageTaken = 0f;
            _damageWindowStart = Time.time;
        }

        if (Mathf.Abs(SkillScore - old) > 0.5f)
            CombatEventBus.FireSkillScoreChanged(old, SkillScore);
    }

    // ── Scoring components ─────────────────────────────────────────────────────

    /// <summary>
    /// Maps reaction time in ms to a quality score.
    /// Perfect 0–200ms = 1.0 · Good 200–400ms → 0.5 · Late 400–600ms → 0.1 · Miss/overtime = 0.
    /// </summary>
    private static float TimingScore(float[] buf, int count)
    {
        if (count == 0) return 0f;
        float sum = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = buf[i];
            if      (t < 0f)    sum += 0.00f;
            else if (t <= 200f) sum += 1.00f;
            else if (t <= 400f) sum += Mathf.Lerp(1.00f, 0.50f, (t - 200f) / 200f);
            else if (t <= 600f) sum += Mathf.Lerp(0.50f, 0.10f, (t - 400f) / 200f);
            // > 600ms or -1 (miss): contributes 0
        }
        return sum / count;
    }

    private float DamageRatioScore()
    {
        float total = _damageDealt + _damageTaken;
        // Return neutral 0.5 until we have meaningful data.
        return total < 5f ? 0.5f : Mathf.Clamp01(_damageDealt / total);
    }

    /// <summary>Shannon entropy of combo usage — more unique combos used = higher score.</summary>
    private float ComboVarietyScore()
    {
        if (_comboFreq.Count < 2) return 0f;

        int total = 0;
        foreach (var kvp in _comboFreq) total += kvp.Value;
        if (total == 0) return 0f;

        float entropy  = 0f;
        float log2inv  = 1f / Mathf.Log(2f);
        foreach (var kvp in _comboFreq)
        {
            float p = (float)kvp.Value / total;
            if (p > 0f) entropy -= p * Mathf.Log(p) * log2inv;
        }

        float maxEntropy = Mathf.Log(_comboFreq.Count, 2f);
        return maxEntropy > 0f ? Mathf.Clamp01(entropy / maxEntropy) : 0f;
    }

    private float ComputeAggressionIndex()
    {
        // Count ring entries from the last 10 seconds, walking backwards from head.
        float cutoff = Time.time - 10f;
        int   recent = 0;
        for (int i = 0; i < _ringCount; i++)
        {
            int idx = (_ringHead - 1 - i + _ring.Length) % _ring.Length;
            if (_ring[idx].Time < cutoff) break;
            recent++;
        }
        return Mathf.Clamp01(recent / 10f);
    }

    private float ComputePatternPredictability()
    {
        if (_trigramFreq.Count == 0) return 0f;
        int max = 0, total = 0;
        foreach (var kvp in _trigramFreq)
        {
            total += kvp.Value;
            if (kvp.Value > max) max = kvp.Value;
        }
        return total > 0 ? Mathf.Clamp01((float)max / total) : 0f;
    }

    // ── Trigram detection ──────────────────────────────────────────────────────

    private void UpdateTrigrams(string newAction)
    {
        string[] w = _actionWindow.ToArray();
        int n = w.Length;
        if (n < 3) return;

        // Record the trigram that just completed.
        string key = string.Concat(w[n - 3], ">", w[n - 2], ">", w[n - 1]);
        _trigramFreq.TryGetValue(key, out int cnt);
        _trigramFreq[key] = cnt + 1;

        // Predict: find the trigram starting with the last 2 actions with highest count.
        if (n >= 2)
        {
            string prefix    = string.Concat(w[n - 2], ">", w[n - 1], ">");
            string bestNext  = string.Empty;
            int    bestCount = 0;

            foreach (var kvp in _trigramFreq)
            {
                if (kvp.Value > bestCount &&
                    kvp.Key.Length > prefix.Length &&
                    kvp.Key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    bestNext  = kvp.Key.Substring(prefix.Length);
                    bestCount = kvp.Value;
                }
            }

            // Require at least 3 occurrences before trusting the prediction.
            PredictedNextAction = bestCount >= 3 ? bestNext : string.Empty;
        }
    }

    // ── Legacy combo building ──────────────────────────────────────────────────

    private void RebuildCurrentCombo()
    {
        _current.Clear();
        if (_recent.Count == 0) return;

        _current.Insert(0, _recent[_recent.Count - 1]);
        for (int i = _recent.Count - 2; i >= 0; i--)
        {
            if (_recent[i + 1].Time - _recent[i].Time <= comboGapThreshold)
                _current.Insert(0, _recent[i]);
            else
                break;
        }
    }

    // ── Public getters (backwards compatibility) ───────────────────────────────

    public List<AttackEvent> GetCurrentCombo()          => new List<AttackEvent>(_current);
    public string            GetCurrentComboSignature() => _lastComboSig;

    public List<AttackEvent> GetLastComboSlice(int maxDepth)
    {
        var copy = new List<AttackEvent>(_current);
        return copy.Count <= maxDepth ? copy : copy.GetRange(copy.Count - maxDepth, maxDepth);
    }

    public string BuildSignature(List<AttackEvent> combo)
    {
        if (combo == null || combo.Count == 0) return "None";
        var sb = new StringBuilder(combo.Count * 14);
        for (int i = 0; i < combo.Count; i++)
        {
            if (i > 0) sb.Append(" > ");
            sb.Append(combo[i].AttackType);
        }
        return sb.ToString();
    }

    public void ClearCombo()
    {
        _recent.Clear();
        _current.Clear();
        _comboFreq.Clear();
        _trigramFreq.Clear();
        _actionWindow.Clear();
        _ringCount = _ringHead = 0;
        _parryCount = _parryHead = 0;
        _dodgeCount = _dodgeHead = 0;
    }
}
