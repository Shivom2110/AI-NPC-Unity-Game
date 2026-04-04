using System.Collections.Generic;
using UnityEngine;

public class BossAIController : MonoBehaviour
{
    [Header("Boss Identity")]
    [SerializeField] private string bossName = "Boss 1";

    [Header("Boss Settings")]
    [SerializeField] private float maxHealth = 2000f;
    [SerializeField] private int comboReadDepth = 2;
    [SerializeField] private float baseCounterCooldown = 1.2f;

    [Header("References")]
    [SerializeField] private Transform player;

    private float currentHealth;
    private float lastCounterTime = -999f;
    private int currentDifficultyPhase = 1;

    private string lastObservedCombo = "None";
    private string lastCounterUsed = "Idle";

    private void Start()
    {
        currentHealth = maxHealth;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    public void OnPlayerCombatAction(PlayerAttackType attackType, float timeStamp)
    {
        if (ComboTracker.Instance == null)
            return;

        List<AttackEvent> comboSlice = ComboTracker.Instance.GetLastComboSlice(comboReadDepth);
        lastObservedCombo = ComboTracker.Instance.BuildSignature(comboSlice);

        if (Time.time - lastCounterTime < GetCurrentCounterCooldown())
            return;

        lastCounterUsed = BossCounterLibrary.GetCounter(comboSlice);
        lastCounterTime = Time.time;

        Debug.Log($"[{bossName}] observed combo: {lastObservedCombo}");
        Debug.Log($"[{bossName}] counter used: {lastCounterUsed}");
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        UpdateDifficultyPhase();

        Debug.Log($"[{bossName}] took {damage} damage. HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void UpdateDifficultyPhase()
    {
        float lostRatio = (maxHealth - currentHealth) / maxHealth;
        currentDifficultyPhase = Mathf.Clamp(1 + Mathf.FloorToInt(lostRatio * 10f), 1, 10);
    }

    private float GetCurrentCounterCooldown()
    {
        return Mathf.Max(0.35f, baseCounterCooldown - (currentDifficultyPhase - 1) * 0.05f);
    }

    private void Die()
    {
        Debug.Log($"[{bossName}] defeated.");
        Destroy(gameObject, 1f);
    }

    public string GetBossName() => bossName;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public int GetComboReadDepth() => comboReadDepth;
    public string GetLastObservedCombo() => lastObservedCombo;
    public string GetLastCounterUsed() => lastCounterUsed;
    public int GetDifficultyPhase() => currentDifficultyPhase;
}
