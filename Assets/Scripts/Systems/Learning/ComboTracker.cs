using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ComboTracker : MonoBehaviour
{
    public static ComboTracker Instance { get; private set; }

    [SerializeField] private float comboGapThreshold = 0.8f;
    [SerializeField] private int maxStoredAttacks = 20;

    private readonly List<AttackEvent> recentAttacks = new List<AttackEvent>();
    private readonly List<AttackEvent> currentCombo = new List<AttackEvent>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddAttack(PlayerAttackType attackType, float timeStamp)
    {
        recentAttacks.Add(new AttackEvent(attackType, timeStamp));

        if (recentAttacks.Count > maxStoredAttacks)
        {
            recentAttacks.RemoveAt(0);
        }

        RebuildCurrentCombo();
        Debug.Log($"[ComboTracker] Combo = {GetCurrentComboSignature()}");
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
