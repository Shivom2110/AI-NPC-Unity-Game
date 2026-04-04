using System.Collections.Generic;
using UnityEngine;

public class BossAIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NPCController npcController;
    [SerializeField] private Transform player;

    [Header("Combat Settings")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float actionCooldown = 2f;

    [Header("Combat Stats")]
    [SerializeField] private float health = 1000f;

    private bool isInCombat = false;
    private float lastActionTime;

    // Basic local learning: frequency counts
    private readonly Dictionary<string, int> actionCounts = new Dictionary<string, int>();

    private void Start()
    {
        if (npcController == null) npcController = GetComponent<NPCController>();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        lastActionTime = Time.time;
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (!isInCombat && dist <= detectionRange)
        {
            isInCombat = true;
            Debug.Log("[BossAI] Combat started.");
        }

        if (isInCombat && dist > detectionRange * 1.5f)
        {
            isInCombat = false;
            Debug.Log("[BossAI] Combat ended.");
            return;
        }

        if (isInCombat && Time.time - lastActionTime >= actionCooldown)
        {
            string bossAction = ChooseBossAction();
            PerformCombatAction(bossAction);
            lastActionTime = Time.time;
        }
    }

    // This MUST exist because PlayerCombatController calls it
    public void OnPlayerCombatAction(string action)
    {
        if (!actionCounts.ContainsKey(action)) actionCounts[action] = 0;
        actionCounts[action]++;

        // Optional: store a simple learned pattern
        if (NPCMemoryManager.Instance != null && npcController != null)
        {
            string most = MostCommonAction();
            _ = NPCMemoryManager.Instance.UpdateLearnedPattern(npcController.GetNPCId(), "most_common_player_action", most);
        }
    }

    private string MostCommonAction()
    {
        string best = "unknown";
        int bestCount = -1;

        foreach (var kv in actionCounts)
        {
            if (kv.Value > bestCount)
            {
                best = kv.Key;
                bestCount = kv.Value;
            }
        }

        return best;
    }

    private string ChooseBossAction()
    {
        // Predict using most common action (simple baseline)
        string predicted = MostCommonAction();

        // Counter mapping (placeholder)
        switch (predicted)
        {
            case "light_attack": return "block";
            case "heavy_attack": return "dodge";
            case "dodge_left": return "quick_attack";
            case "dodge_right": return "quick_attack";
            case "block": return "heavy_attack";
            default:
                return Random.value < 0.5f ? "quick_attack" : "heavy_attack";
        }
    }

    private void PerformCombatAction(string action)
    {
        Debug.Log($"[BossAI] Boss performs: {action}");
        // TODO: hook animations + hit logic later
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log($"[BossAI] Took {damage} damage. Health={health}");
        if (health <= 0)
        {
            Debug.Log("[BossAI] Boss defeated!");
            Destroy(gameObject);
        }
    }
}
