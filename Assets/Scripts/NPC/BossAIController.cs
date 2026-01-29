using System.Collections.Generic;
using UnityEngine;
using AINPC.Systems.Learning;
using AINPC.Systems.Events;

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

    [Header("Learning / Fairness")]
    [Tooltip("Minimum confidence required to hard-counter predicted move.")]
    [Range(0f, 1f)] [SerializeField] private float minCounterConfidence = 0.45f;

    [Tooltip("Chance to ignore the optimal counter (prevents 'cheating' feel).")]
    [Range(0f, 1f)] [SerializeField] private float randomness = 0.20f;

    [Tooltip("Prevents repeating the same counter too often.")]
    [SerializeField] private int repeatCounterLimit = 2;

    private bool isInCombat = false;
    private float lastActionTime;

    // Local “planner”
    private readonly Queue<string> plannedActions = new Queue<string>();

    // Optional pattern tracking (kept from your old design)
    private readonly Dictionary<string, int> playerActionCounter = new Dictionary<string, int>();
    private int consecutiveDodges = 0;
    private int consecutiveAttacks = 0;

    // Used to build transition model locally even if you don't want to rely on GameInitializer yet
    private PlayerEventType _lastSeenPlayerType = PlayerEventType.None;

    // Anti-repeat
    private string _lastCounterAction = "";
    private int _repeatCounterCount = 0;

    // Reference to predictor (stored in GameInitializer singleton)
    private MarkovPredictor Predictor
    {
        get
        {
            // If you made GameInitializer.Instance + Predictor as suggested
            return GameInitializer.Instance != null ? GameInitializer.Instance.Predictor : null;
        }
    }

    void Start()
    {
        if (npcController == null)
            npcController = GetComponent<NPCController>();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        lastActionTime = Time.time;

        // Subscribe to player events so we can track last move + patterns
        EventBus.OnPlayerEvent += OnPlayerEvent;
    }

    private void OnDestroy()
    {
        EventBus.OnPlayerEvent -= OnPlayerEvent;
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange && !isInCombat)
            EnterCombat();

        if (distanceToPlayer > detectionRange * 1.5f && isInCombat)
            ExitCombat();

        if (isInCombat && Time.time - lastActionTime >= actionCooldown)
            ExecuteNextAction(distanceToPlayer);
    }

    private void EnterCombat()
    {
        isInCombat = true;
        plannedActions.Clear();
        Debug.Log($"{GetBossId()} enters combat!");

        // Immediately plan something
        UpdateCombatStrategy();
    }

    private void ExitCombat()
    {
        isInCombat = false;
        plannedActions.Clear();
        Debug.Log($"{GetBossId()} exits combat");
    }

    private void ExecuteNextAction(float distanceToPlayer)
    {
        if (plannedActions.Count == 0)
            UpdateCombatStrategy();

        if (plannedActions.Count == 0)
            return;

        string action = plannedActions.Dequeue();

        // Basic spacing rule: if too far, prefer gap-close
        if (distanceToPlayer > attackRange && action != "charge")
        {
            action = "charge";
        }

        PerformCombatAction(action);
        lastActionTime = Time.time;
    }

    private void UpdateCombatStrategy()
    {
        // 1) Predict next player action from Markov model
        var predictor = Predictor;
        PredictionResult pred = predictor != null
            ? predictor.PredictNext(_lastSeenPlayerType)
            : PredictionResult.None();

        // 2) If low confidence or random chance, do a “neutral” action instead of hard counter
        bool shouldRandomize = Random.value < randomness;
        bool canCounter = pred.hasPrediction && pred.confidence >= minCounterConfidence && !shouldRandomize;

        string chosen = canCounter
            ? ChooseCounterFor(pred.predicted)
            : ChooseFallbackAction();

        // 3) Anti-repeat to avoid feeling unfair/repetitive
        if (!string.IsNullOrEmpty(_lastCounterAction) && chosen == _lastCounterAction)
        {
            _repeatCounterCount++;
            if (_repeatCounterCount > repeatCounterLimit)
            {
                chosen = ChooseFallbackAction();
                _repeatCounterCount = 0;
            }
        }
        else
        {
            _lastCounterAction = chosen;
            _repeatCounterCount = 0;
        }

        plannedActions.Enqueue(chosen);

        if (pred.hasPrediction)
        {
            Debug.Log($"[Boss] Last={_lastSeenPlayerType} Pred={pred.predicted} Conf={pred.confidence:0.00} -> Action={chosen}");
        }
        else
        {
            Debug.Log($"[Boss] No prediction -> Action={chosen}");
        }
    }

    private string ChooseCounterFor(PlayerEventType predicted)
    {
        // Simple “rock-paper-scissors” mapping. You’ll expand this later.
        switch (predicted)
        {
            case PlayerEventType.AttackHigh:
            case PlayerEventType.AttackMid:
            case PlayerEventType.AttackLow:
                // Counter attacks with block/parry or dodge into punish
                return (Random.value < 0.5f) ? "block" : "quick_attack";

            case PlayerEventType.DodgeLeft:
            case PlayerEventType.DodgeRight:
            case PlayerEventType.DodgeBack:
                // Catch dodges with area or charge
                return (Random.value < 0.5f) ? "area_attack" : "charge";

            case PlayerEventType.Block:
                // Break defense with heavy or special
                return (Random.value < 0.5f) ? "heavy_attack" : "special_ability";

            case PlayerEventType.Heal:
                // Punish heal with charge/heavy
                return (Random.value < 0.6f) ? "charge" : "heavy_attack";

            default:
                return ChooseFallbackAction();
        }
    }

    private string ChooseFallbackAction()
    {
        // “Neutral” / varied options
        float r = Random.value;
        if (r < 0.40f) return "quick_attack";
        if (r < 0.65f) return "heavy_attack";
        if (r < 0.80f) return "dodge";
        if (r < 0.92f) return "block";
        return "special_ability";
    }

    // Subscribe to player events (the new system you wired in PlayerCombatController)
    private void OnPlayerEvent(PlayerEvent e)
    {
        // Track transitions locally too (optional). Your GameInitializer already does this,
        // but keeping it here makes the boss self-contained.
        // If you want ONLY GameInitializer to handle it, delete the next 3 lines.
        if (Predictor != null)
            Predictor.Record(_lastSeenPlayerType, e.type);

        _lastSeenPlayerType = e.type;

        // Keep your old pattern counters (optional)
        TrackLegacyPatterns(e);
    }

    private void TrackLegacyPatterns(PlayerEvent e)
    {
        string key = e.type.ToString().ToLower();

        if (!playerActionCounter.ContainsKey(key))
            playerActionCounter[key] = 0;
        playerActionCounter[key]++;

        if (key.Contains("dodge"))
        {
            consecutiveDodges++;
            consecutiveAttacks = 0;
        }
        else if (key.Contains("attack"))
        {
            consecutiveAttacks++;
            consecutiveDodges = 0;
        }
        else
        {
            consecutiveDodges = 0;
            consecutiveAttacks = 0;
        }

        // If player is spamming one thing, push a counter into the queue sooner
        if (isInCombat && plannedActions.Count == 0)
        {
            if (consecutiveDodges >= 3)
                plannedActions.Enqueue("area_attack");
            else if (consecutiveAttacks >= 3)
                plannedActions.Enqueue("block");
        }
    }

    private string GetBossId()
    {
        // If npcController has an ID, use that; else fallback to object name
        return npcController != null ? npcController.GetNPCId() : gameObject.name;
    }

    // Combat action implementations (kept from your original)
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
                QuickAttack();
                break;
        }
    }

    private void HeavyAttack()
    {
        Debug.Log("Boss performs HEAVY ATTACK!");
        // TODO: Implement actual attack logic with animations
    }

    private void QuickAttack()
    {
        Debug.Log("Boss performs quick attack");
        // TODO: Implement quick attack
    }

    private void SpecialAbility()
    {
        Debug.Log("Boss uses SPECIAL ABILITY!");
        // TODO: Implement special move
    }

    private void Dodge()
    {
        Debug.Log("Boss dodges!");
        // TODO: dodge animation / movement
    }

    private void Block()
    {
        Debug.Log("Boss raises block!");
        // TODO: blocking state
    }

    private void ChargeAttack()
    {
        Debug.Log("Boss charges at player!");
        // TODO: move toward player
    }

    private void AreaAttack()
    {
        Debug.Log("Boss performs area attack!");
        // TODO: AOE damage
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log($"Boss took {damage} damage! Health: {health}/{maxHealth}");

        if (health <= 0)
            Die();

        if (health < maxHealth * 0.3f)
            Debug.Log("Boss enters desperate phase!");
    }

    private void Die()
    {
        isInCombat = false;
        Debug.Log($"{GetBossId()} defeated!");
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