using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ComboTracker : MonoBehaviour
{
    public static ComboTracker Instance { get; private set; }

    [SerializeField] private float comboGapThreshold = 0.8f;
    [SerializeField] private int   maxStoredAttacks  = 20;

    // How many recent parry/dodge results and combo lengths to remember
    private const int SkillHistorySize = 8;

    private readonly List<AttackEvent> recentAttacks = new List<AttackEvent>();
    private readonly List<AttackEvent> currentCombo  = new List<AttackEvent>();

    // Skill tracking
    private readonly Queue<bool> _parryHistory = new Queue<bool>();
    private readonly Queue<bool> _dodgeHistory = new Queue<bool>();
    private readonly Queue<int>  _comboLengths = new Queue<int>();
    private int _lastComboLength = 0;

    /// <summary>Player skill level 1–5. Boss reads this to scale its difficulty.</summary>
    public int SkillLevel { get; private set; } = 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // ── Skill reporting (called by PlayerCombatController) ────────
    public void ReportParry(bool success)
    {
        if (_parryHistory.Count >= SkillHistorySize) _parryHistory.Dequeue();
        _parryHistory.Enqueue(success);
        RecalculateSkill();
        Debug.Log($"[ComboTracker] Parry {(success ? "HIT" : "MISS")} | Skill {SkillLevel}");
    }

    public void ReportDodge(bool success)
    {
        if (_dodgeHistory.Count >= SkillHistorySize) _dodgeHistory.Dequeue();
        _dodgeHistory.Enqueue(success);
        RecalculateSkill();
        Debug.Log($"[ComboTracker] Dodge {(success ? "HIT" : "MISS")} | Skill {SkillLevel}");
    }

    private void RecalculateSkill()
    {
        // Parry success rate (0–1)
        float parryRate = SuccessRate(_parryHistory);

        // Dodge success rate (0–1)
        float dodgeRate = SuccessRate(_dodgeHistory);

        // Average combo length normalised (0–1, where 5+ attacks = max)
        float avgCombo = _comboLengths.Count > 0 ? AverageQueue(_comboLengths) : 1f;
        float comboScore = Mathf.Clamp01((avgCombo - 1f) / 4f); // 1 attack = 0, 5+ = 1

        // Weighted average
        float score = parryRate * 0.4f + dodgeRate * 0.35f + comboScore * 0.25f;

        SkillLevel = Mathf.Clamp(Mathf.CeilToInt(score * 5f), 1, 5);
    }

    private float SuccessRate(Queue<bool> history)
    {
        if (history.Count == 0) return 0f;
        int hits = 0;
        foreach (bool b in history) if (b) hits++;
        return (float)hits / history.Count;
    }

    private float AverageQueue(Queue<int> q)
    {
        float sum = 0f;
        foreach (int v in q) sum += v;
        return sum / q.Count;
    }

    // ─────────────────────────────────────────────────────────────

    public void AddAttack(PlayerAttackType attackType, float timeStamp)
    {
        recentAttacks.Add(new AttackEvent(attackType, timeStamp));

        if (recentAttacks.Count > maxStoredAttacks)
            recentAttacks.RemoveAt(0);

        int prevLen = _lastComboLength;
        RebuildCurrentCombo();

        // When a combo ends (gap broke it) record its length
        if (currentCombo.Count < prevLen && prevLen > 1)
        {
            if (_comboLengths.Count >= SkillHistorySize) _comboLengths.Dequeue();
            _comboLengths.Enqueue(prevLen);
            RecalculateSkill();
        }

        _lastComboLength = currentCombo.Count;
        Debug.Log($"[ComboTracker] Combo = {GetCurrentComboSignature()} | Skill {SkillLevel}");
    }

    private void RebuildCurrentCombo()
    {
        currentCombo.Clear();

        if (recentAttacks.Count == 0)
            return;

        currentCombo.Insert(0, recentAttacks[recentAttacks.Count - 1]);

        for (int i = recentAttacks.Count - 2; i >= 0; i--)
        {
            float gap = recentAttacks[i + 1].Time - recentAttacks[i].Time;
            if (gap <= comboGapThreshold)
            {
                currentCombo.Insert(0, recentAttacks[i]);
            }
            else
            {
                break;
            }
        }
    }

    public List<AttackEvent> GetCurrentCombo()
    {
        return new List<AttackEvent>(currentCombo);
    }

    public List<AttackEvent> GetLastComboSlice(int maxDepth)
    {
        List<AttackEvent> copy = new List<AttackEvent>(currentCombo);

        if (copy.Count <= maxDepth)
            return copy;

        return copy.GetRange(copy.Count - maxDepth, maxDepth);
    }

    public string GetCurrentComboSignature()
    {
        return BuildSignature(currentCombo);
    }

    public string BuildSignature(List<AttackEvent> combo)
    {
        if (combo == null || combo.Count == 0)
            return "None";

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < combo.Count; i++)
        {
            if (i > 0) sb.Append(" > ");
            sb.Append(combo[i].AttackType);
        }

        return sb.ToString();
    }

    public void ClearCombo()
    {
        recentAttacks.Clear();
        currentCombo.Clear();
    }
}
