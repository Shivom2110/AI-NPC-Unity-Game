using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BossAIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NPCController npcController;
    [SerializeField] private Transform player;

    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float actionCooldown = 2f;

    [Header("Combat Stats")]
    [SerializeField] private float health = 1000f;
    [SerializeField] private float maxHealth = 1000f;

    // Combat state
    private bool isInCombat = false;
    private float lastActionTime;
    private Queue<string> plannedActions = new Queue<string>();
    private Dictionary<string, int> playerActionCounter = new Dictionary<string, int>();
    
    // Pattern tracking
    private string currentPlayerPattern = "unknown";
    private int consecutiveDodges = 0;
    private int consecutiveAttacks = 0;

    void Start()
    {
        if (npcController == null)
            npcController = GetComponent<NPCController>();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        lastActionTime = Time.time;
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Enter combat if player is in range
        if (distanceToPlayer <= detectionRange && !isInCombat)
        {
            EnterCombat();
        }

        // Exit combat if player is far away
        if (distanceToPlayer > detectionRange * 1.5f && isInCombat)
        {
            ExitCombat();
        }

        // Execute combat actions
        if (isInCombat && Time.time - lastActionTime >= actionCooldown)
        {
            ExecuteNextAction();
        }
    }

    private async void EnterCombat()
    {
        isInCombat = true;
        Debug.Log($"{npcController.GetNPCId()} enters combat!");

        // Greet player based on relationship and past interactions
        await npcController.InteractWithPlayer("player_entered_combat");

        // Request initial combat strategy from AI
        await UpdateCombatStrategy();
    }

    private void ExitCombat()
    {
        isInCombat = false;
        plannedActions.Clear();
        Debug.Log($"{npcController.GetNPCId()} exits combat");
    }

    private async void ExecuteNextAction()
    {
        // If no planned actions, get new strategy
        if (plannedActions.Count == 0)
        {
            await UpdateCombatStrategy();
        }

        // Execute next action if available
        if (plannedActions.Count > 0)
        {
            string action = plannedActions.Dequeue();
            PerformCombatAction(action);
            lastActionTime = Time.time;

            // Record this action
            NPCMemory memory = npcController.GetMemory();
            await NPCMemoryManager.Instance.RecordInteraction(
                npcController.GetNPCId(),
                $"boss_used: {action}",
                "combat_action_executed",
                "in_progress"
            );
        }
    }

    // Get new combat strategy from AI based on player patterns
    private async Task UpdateCombatStrategy()
    {
        string playerPattern = AnalyzePlayerPattern();
        
        string[] newActions = await npcController.GetBossCombatStrategy(playerPattern);
        
        if (newActions != null)
        {
            foreach (string action in newActions)
            {
                plannedActions.Enqueue(action);
            }
            
            Debug.Log($"Boss planned actions: {string.Join(", ", newActions)}");
        }
    }

    // Actually perform the combat action
    private void PerformCombatAction(string action)
    {
        Debug.Log($"Boss performs: {action}");

        switch (action.ToLower())
        {
            case "heavy_attack":
            case "heavy attack":
                HeavyAttack();
                break;
            
            case "quick_attack":
            case "quick attack":
                QuickAttack();
                break;
            
            case "special_ability":
            case "special ability":
            case "special":
                SpecialAbility();
                break;
            
            case "dodge":
            case "evade":
                Dodge();
                break;
            
            case "block":
            case "defend":
                Block();
                break;
            
            case "charge":
            case "rush":
                ChargeAttack();
                break;
            
            case "area_attack":
            case "area attack":
            case "aoe":
                AreaAttack();
                break;
            
            default:
                QuickAttack(); // Default to basic attack
                break;
        }
    }

    // Analyze what the player has been doing
    private string AnalyzePlayerPattern()
    {
        // Build description of player behavior
        List<string> patterns = new List<string>();

        if (consecutiveDodges >= 3)
            patterns.Add("dodging frequently");
        
        if (consecutiveAttacks >= 3)
            patterns.Add("aggressive attacking");

        // Check action frequency
        int totalActions = 0;
        foreach (var count in playerActionCounter.Values)
            totalActions += count;

        if (totalActions > 0)
        {
            foreach (var kvp in playerActionCounter)
            {
                float percentage = (float)kvp.Value / totalActions;
                if (percentage > 0.4f) // If they use this action more than 40% of the time
                {
                    patterns.Add($"favoring {kvp.Key}");
                }
            }
        }

        return patterns.Count > 0 ? string.Join(", ", patterns) : "balanced combat style";
    }

    // Public method called when player performs an action
    public async void OnPlayerCombatAction(string action)
    {
        // Track action frequency
        if (!playerActionCounter.ContainsKey(action))
            playerActionCounter[action] = 0;
        playerActionCounter[action]++;

        // Track consecutive patterns
        if (action.Contains("dodge"))
        {
            consecutiveDodges++;
            consecutiveAttacks = 0;
        }
        else if (action.Contains("attack"))
        {
            consecutiveAttacks++;
            consecutiveDodges = 0;
        }
        else
        {
            consecutiveDodges = 0;
            consecutiveAttacks = 0;
        }

        // Update learned patterns in memory
        NPCMemory memory = npcController.GetMemory();
        
        if (consecutiveDodges >= 3)
        {
            await NPCMemoryManager.Instance.UpdateLearnedPattern(
                npcController.GetNPCId(),
                "dodge_pattern",
                "player_dodges_frequently"
            );
        }

        if (consecutiveAttacks >= 3)
        {
            await NPCMemoryManager.Instance.UpdateLearnedPattern(
                npcController.GetNPCId(),
                "attack_pattern",
                "player_attacks_aggressively"
            );
        }

        // Occasionally adapt strategy mid-combat
        if (Random.value < 0.2f) // 20% chance to adapt after player action
        {
            await UpdateCombatStrategy();
        }
    }

    // Combat action implementations
    private void HeavyAttack()
    {
        Debug.Log("Boss performs HEAVY ATTACK!");
        // TODO: Implement actual attack logic with animations
        // DealDamageToPlayer(50f);
    }

    private void QuickAttack()
    {
        Debug.Log("Boss performs quick attack");
        // TODO: Implement quick attack
        // DealDamageToPlayer(20f);
    }

    private void SpecialAbility()
    {
        Debug.Log("Boss uses SPECIAL ABILITY!");
        // TODO: Implement special move
    }

    private void Dodge()
    {
        Debug.Log("Boss dodges!");
        // TODO: Play dodge animation, grant invincibility frames
    }

    private void Block()
    {
        Debug.Log("Boss raises block!");
        // TODO: Activate blocking state
    }

    private void ChargeAttack()
    {
        Debug.Log("Boss charges at player!");
        // TODO: Rush toward player
    }

    private void AreaAttack()
    {
        Debug.Log("Boss performs area attack!");
        // TODO: AOE damage around boss
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log($"Boss took {damage} damage! Health: {health}/{maxHealth}");

        if (health <= 0)
        {
            Die();
        }

        // React to taking damage
        if (health < maxHealth * 0.3f) // Below 30% health
        {
            // Boss might get more aggressive or use desperate tactics
            Debug.Log("Boss enters desperate phase!");
        }
    }

    private async void Die()
    {
        isInCombat = false;
        Debug.Log($"{npcController.GetNPCId()} defeated!");

        // Record defeat
        await NPCMemoryManager.Instance.RecordInteraction(
            npcController.GetNPCId(),
            "player_defeated_boss",
            "boss_defeated",
            "defeat",
            -50
        );

        // TODO: Play death animation, drop loot, etc.
        Destroy(gameObject, 3f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
