#!/usr/bin/env bash
set -euo pipefail

BACKUP_DIR=".backup_gameplay_$(date +%Y%m%d_%H%M%S)"
mkdir -p "$BACKUP_DIR"

backup_file () {
  if [ -f "$1" ]; then
    cp "$1" "$BACKUP_DIR/$(basename "$1").bak"
  fi
}

echo "Creating backup in $BACKUP_DIR ..."

backup_file "Assets/Scripts/Core/GameInitializer.cs"
backup_file "Assets/Scripts/Core/NPCMemoryManager.cs"
backup_file "Assets/Scripts/NPC/NPCMemory.cs"
backup_file "Assets/Scripts/NPC/NPCController.cs"
backup_file "Assets/Scripts/NPC/BossAIController.cs"
backup_file "Assets/Scripts/Player/PlayerCombatController.cs"
backup_file "Assets/Scripts/Player/PlayerInteractionManager.cs"

echo "Cleaning junk files ..."
rm -f Assets/Scenes/HubArea.unity.meta.unity
rm -f Assets/Scenes/HubArea.unity.meta.unity.meta
rm -f install_architecture_stubs.sh
rm -f install_local_learning.sh
rm -f install_local_learning.sh.save

mkdir -p Assets/Scripts/Systems/Learning
mkdir -p Assets/Scripts/UI

cat > Assets/Scripts/Systems/Learning/PlayerAttackType.cs <<'CS'
public enum PlayerAttackType
{
    AutoAttack,
    Attack2,
    Attack3,
    Attack4,
    Ultimate
}
CS

cat > Assets/Scripts/Systems/Learning/AttackEvent.cs <<'CS'
using System;

[Serializable]
public struct AttackEvent
{
    public PlayerAttackType AttackType;
    public float Time;

    public AttackEvent(PlayerAttackType attackType, float time)
    {
        AttackType = attackType;
        Time = time;
    }

    public override string ToString()
    {
        return $"{AttackType}@{Time:F2}";
    }
}
CS

cat > Assets/Scripts/Systems/Learning/ComboTracker.cs <<'CS'
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
CS

cat > Assets/Scripts/Systems/Learning/BossCounterLibrary.cs <<'CS'
using System.Collections.Generic;

public static class BossCounterLibrary
{
    private static readonly Dictionary<PlayerAttackType, string> SingleAttackCounters =
        new Dictionary<PlayerAttackType, string>
        {
            { PlayerAttackType.AutoAttack, "Block" },
            { PlayerAttackType.Attack2, "Dodge" },
            { PlayerAttackType.Attack3, "HeavyCounter" },
            { PlayerAttackType.Attack4, "Interrupt" },
            { PlayerAttackType.Ultimate, "SpecialCounter" }
        };

    private static readonly Dictionary<string, string> ComboCounters =
        new Dictionary<string, string>
        {
            { "AutoAttack > AutoAttack > AutoAttack", "QuickPunish" },
            { "AutoAttack > Attack2", "BlockThenPunish" },
            { "Attack2 > Attack3", "DodgeThenStrike" },
            { "Attack2 > Attack3 > Attack4", "HeavyCounterChain" },
            { "Attack3 > Attack4 > Ultimate", "SpecialCounter" },
            { "AutoAttack > Attack2 > Attack3", "ShieldBreakPunish" },
            { "Attack2 > Attack3 > Attack4 > Ultimate", "FullComboCounter" }
        };

    public static string GetCounter(List<AttackEvent> comboSlice)
    {
        if (comboSlice == null || comboSlice.Count == 0)
            return "Idle";

        string signature = BuildSignature(comboSlice);
        if (ComboCounters.TryGetValue(signature, out string comboCounter))
            return comboCounter;

        PlayerAttackType lastAttack = comboSlice[comboSlice.Count - 1].AttackType;
        if (SingleAttackCounters.TryGetValue(lastAttack, out string singleCounter))
            return singleCounter;

        return "QuickAttack";
    }

    private static string BuildSignature(List<AttackEvent> comboSlice)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < comboSlice.Count; i++)
        {
            if (i > 0) sb.Append(" > ");
            sb.Append(comboSlice[i].AttackType);
        }

        return sb.ToString();
    }
}
CS

cat > Assets/Scripts/NPC/NPCMemory.cs <<'CS'
using System.Collections.Generic;

[System.Serializable]
public class NPCInteraction
{
    public string timestampIso;
    public string playerAction;
    public string npcResponse;
    public string outcome;
    public int relationshipChange;
}

[System.Serializable]
public class NPCMemory
{
    public string npcId;
    public string personality;
    public int relationshipScore;
    public List<NPCInteraction> interactions = new List<NPCInteraction>();
    public Dictionary<string, string> learnedPatterns = new Dictionary<string, string>();

    public NPCMemory(string id, string personalityType, int score)
    {
        npcId = id;
        personality = personalityType;
        relationshipScore = score;
    }
}
CS

cat > Assets/Scripts/Core/NPCMemoryManager.cs <<'CS'
using System.Collections.Generic;
using UnityEngine;

public class NPCMemoryManager : MonoBehaviour
{
    public static NPCMemoryManager Instance { get; private set; }

    private readonly Dictionary<string, NPCMemory> store = new Dictionary<string, NPCMemory>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public NPCMemory LoadNPCMemory(string npcId, string personality = "neutral")
    {
        if (!store.TryGetValue(npcId, out NPCMemory memory))
        {
            memory = new NPCMemory(npcId, personality, 0);
            store[npcId] = memory;
        }

        return memory;
    }

    public void RecordInteraction(string npcId, string playerAction, string npcResponse, string outcome, int relationshipDelta = 0)
    {
        if (!store.TryGetValue(npcId, out NPCMemory memory))
        {
            memory = new NPCMemory(npcId, "neutral", 0);
            store[npcId] = memory;
        }

        memory.relationshipScore += relationshipDelta;
        memory.interactions.Add(new NPCInteraction
        {
            timestampIso = System.DateTime.UtcNow.ToString("o"),
            playerAction = playerAction,
            npcResponse = npcResponse,
            outcome = outcome,
            relationshipChange = relationshipDelta
        });
    }

    public void UpdateLearnedPattern(string npcId, string key, string value)
    {
        if (!store.TryGetValue(npcId, out NPCMemory memory))
        {
            memory = new NPCMemory(npcId, "neutral", 0);
            store[npcId] = memory;
        }

        memory.learnedPatterns[key] = value;
    }

    public string GetRelationshipLevel(int score)
    {
        if (score >= 50) return "Allied";
        if (score >= 20) return "Friendly";
        if (score > -20) return "Neutral";
        if (score > -50) return "Hostile";
        return "Enemy";
    }
}
CS

cat > Assets/Scripts/Core/GameInitializer.cs <<'CS'
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    public static GameInitializer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureComponent<NPCMemoryManager>();
        EnsureComponent<ComboTracker>();

        Debug.Log("[GameInitializer] Systems ready.");
    }

    private void EnsureComponent<T>() where T : Component
    {
        T existing = FindObjectOfType<T>();
        if (existing == null)
        {
            gameObject.AddComponent<T>();
        }
    }
}
CS

cat > Assets/Scripts/NPC/NPCController.cs <<'CS'
using UnityEngine;

public class NPCController : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string npcId = "merchant_01";
    [SerializeField] private string personality = "merchant";

    [Header("Boss Flag")]
    [SerializeField] private bool isBoss = false;

    private NPCMemory memory;

    private void Start()
    {
        if (NPCMemoryManager.Instance != null)
        {
            memory = NPCMemoryManager.Instance.LoadNPCMemory(npcId, personality);
        }
    }

    public bool IsBoss()
    {
        return isBoss;
    }

    public string GetNPCId()
    {
        return npcId;
    }

    public NPCMemory GetMemory()
    {
        if (memory == null && NPCMemoryManager.Instance != null)
        {
            memory = NPCMemoryManager.Instance.LoadNPCMemory(npcId, personality);
        }

        return memory;
    }

    public string InteractWithPlayer(string playerAction)
    {
        if (NPCMemoryManager.Instance == null)
            return "Systems are not ready.";

        if (memory == null)
            memory = NPCMemoryManager.Instance.LoadNPCMemory(npcId, personality);

        string response = GenerateLocalResponse(playerAction);
        int delta = CalculateRelationshipDelta(playerAction);

        NPCMemoryManager.Instance.RecordInteraction(npcId, playerAction, response, "ok", delta);
        memory = NPCMemoryManager.Instance.LoadNPCMemory(npcId, personality);

        return response;
    }

    private int CalculateRelationshipDelta(string action)
    {
        switch (action)
        {
            case "greet": return 1;
            case "help": return 5;
            case "trade": return 2;
            case "threaten": return -10;
            default: return 0;
        }
    }

    private string GenerateLocalResponse(string action)
    {
        int score = memory != null ? memory.relationshipScore : 0;
        string relation = NPCMemoryManager.Instance.GetRelationshipLevel(score);
        string p = personality.ToLowerInvariant();

        if (p.Contains("merchant"))
        {
            if (relation == "Friendly" || relation == "Allied")
                return "Welcome back! I've set aside some of my best goods for you.";
            if (relation == "Hostile" || relation == "Enemy")
                return "Buy something or move along.";
            if (action == "trade")
                return "Take a look. I might have what you need.";
            return "Greetings, traveler. Care to browse my wares?";
        }

        if (p.Contains("guard"))
        {
            if (relation == "Friendly" || relation == "Allied")
                return "Stay alert out there. The roads are not safe.";
            if (relation == "Hostile" || relation == "Enemy")
                return "State your business and be quick about it.";
            return "Keep the peace and we won't have a problem.";
        }

        if (action == "help")
            return "I appreciate your help.";
        if (action == "threaten")
            return "Watch your tone.";
        if (action == "bye")
            return "Safe travels.";

        return "I see.";
    }
}
CS

cat > Assets/Scripts/NPC/BossAIController.cs <<'CS'
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
CS

cat > Assets/Scripts/Player/PlayerCombatController.cs <<'CS'
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float bossSearchRadius = 15f;

    [Header("Keys")]
    [SerializeField] private KeyCode autoAttackKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode attack2Key = KeyCode.Mouse1;
    [SerializeField] private KeyCode attack3Key = KeyCode.Q;
    [SerializeField] private KeyCode attack4Key = KeyCode.R;
    [SerializeField] private KeyCode ultimateKey = KeyCode.F;

    private readonly Dictionary<PlayerAttackType, float> damages = new Dictionary<PlayerAttackType, float>();
    private readonly Dictionary<PlayerAttackType, float> cooldowns = new Dictionary<PlayerAttackType, float>();
    private readonly Dictionary<PlayerAttackType, float> lastUsedTimes = new Dictionary<PlayerAttackType, float>();

    private BossAIController currentBoss;

    private void Start()
    {
        damages[PlayerAttackType.AutoAttack] = 10f;
        damages[PlayerAttackType.Attack2] = 50f;
        damages[PlayerAttackType.Attack3] = 100f;
        damages[PlayerAttackType.Attack4] = 150f;
        damages[PlayerAttackType.Ultimate] = 300f;

        cooldowns[PlayerAttackType.AutoAttack] = 0f;
        cooldowns[PlayerAttackType.Attack2] = 3f;
        cooldowns[PlayerAttackType.Attack3] = 5f;
        cooldowns[PlayerAttackType.Attack4] = 7f;
        cooldowns[PlayerAttackType.Ultimate] = 10f;

        foreach (PlayerAttackType attack in damages.Keys)
        {
            lastUsedTimes[attack] = -999f;
        }
    }

    private void Update()
    {
        FindNearestBoss();
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(autoAttackKey)) TryAttack(PlayerAttackType.AutoAttack);
        if (Input.GetKeyDown(attack2Key)) TryAttack(PlayerAttackType.Attack2);
        if (Input.GetKeyDown(attack3Key)) TryAttack(PlayerAttackType.Attack3);
        if (Input.GetKeyDown(attack4Key)) TryAttack(PlayerAttackType.Attack4);
        if (Input.GetKeyDown(ultimateKey)) TryAttack(PlayerAttackType.Ultimate);
    }

    private void TryAttack(PlayerAttackType attackType)
    {
        float now = Time.time;
        float remainingCooldown = GetRemainingCooldown(attackType);

        if (remainingCooldown > 0f)
        {
            Debug.Log($"[Player] {attackType} on cooldown: {remainingCooldown:F1}s left");
            return;
        }

        lastUsedTimes[attackType] = now;

        if (ComboTracker.Instance != null)
        {
            ComboTracker.Instance.AddAttack(attackType, now);
        }

        if (currentBoss != null)
        {
            currentBoss.OnPlayerCombatAction(attackType, now);

            if (IsInRange(currentBoss.transform))
            {
                currentBoss.TakeDamage(damages[attackType]);
            }
        }

        Debug.Log($"[Player] Used {attackType} | damage={damages[attackType]} | cooldown={cooldowns[attackType]}");
    }

    private float GetRemainingCooldown(PlayerAttackType attackType)
    {
        float elapsed = Time.time - lastUsedTimes[attackType];
        return Mathf.Max(0f, cooldowns[attackType] - elapsed);
    }

    private void FindNearestBoss()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, bossSearchRadius);

        BossAIController best = null;
        float bestDistance = float.MaxValue;

        foreach (Collider col in nearby)
        {
            BossAIController boss = col.GetComponent<BossAIController>();
            if (boss == null) continue;

            float distance = Vector3.Distance(transform.position, boss.transform.position);
            if (distance < bestDistance)
            {
                best = boss;
                bestDistance = distance;
            }
        }

        currentBoss = best;
    }

    private bool IsInRange(Transform target)
    {
        return Vector3.Distance(transform.position, target.position) <= attackRange;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bossSearchRadius);
    }
}
CS

cat > Assets/Scripts/Player/PlayerInteractionManager.cs <<'CS'
using UnityEngine;

public class PlayerInteractionManager : MonoBehaviour
{
    [SerializeField] private float interactionRadius = 4f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private DialogueUIController dialogueUI;

    private NPCController currentNpc;
    private int interactionCount = 0;

    private readonly string[] actionCycle = new string[]
    {
        "greet",
        "trade",
        "help",
        "bye"
    };

    private void Update()
    {
        FindNearestNpc();

        if (currentNpc != null)
        {
            if (dialogueUI != null)
                dialogueUI.ShowPrompt("Press E to interact");

            if (Input.GetKeyDown(interactKey))
            {
                Interact();
            }
        }
        else
        {
            if (dialogueUI != null)
                dialogueUI.HidePrompt();
        }
    }

    private void FindNearestNpc()
    {
        NPCController[] npcs = FindObjectsOfType<NPCController>();
        NPCController best = null;
        float bestDistance = float.MaxValue;

        foreach (NPCController npc in npcs)
        {
            if (npc == null || npc.IsBoss()) continue;

            float distance = Vector3.Distance(transform.position, npc.transform.position);
            if (distance <= interactionRadius && distance < bestDistance)
            {
                best = npc;
                bestDistance = distance;
            }
        }

        currentNpc = best;
    }

    private void Interact()
    {
        if (currentNpc == null) return;

        string action = actionCycle[interactionCount % actionCycle.Length];
        string response = currentNpc.InteractWithPlayer(action);
        NPCMemory memory = currentNpc.GetMemory();
        string relation = memory != null && NPCMemoryManager.Instance != null
            ? NPCMemoryManager.Instance.GetRelationshipLevel(memory.relationshipScore)
            : "Unknown";

        if (dialogueUI != null)
        {
            dialogueUI.ShowDialogue(currentNpc.GetNPCId(), response, relation);
        }

        Debug.Log($"[Interaction] action={action} npc={currentNpc.GetNPCId()} response={response} relation={relation}");
        interactionCount++;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
CS

cat > Assets/Scripts/UI/DialogueUIController.cs <<'CS'
using UnityEngine;
using UnityEngine.UI;

public class DialogueUIController : MonoBehaviour
{
    [SerializeField] private Text npcNameText;
    [SerializeField] private Text dialogueText;
    [SerializeField] private Text relationshipText;
    [SerializeField] private Text promptText;

    public void ShowPrompt(string message)
    {
        if (promptText != null)
        {
            promptText.text = message;
            promptText.gameObject.SetActive(true);
        }
    }

    public void HidePrompt()
    {
        if (promptText != null)
        {
            promptText.text = "";
            promptText.gameObject.SetActive(false);
        }
    }

    public void ShowDialogue(string npcName, string dialogue, string relation)
    {
        if (npcNameText != null) npcNameText.text = npcName;
        if (dialogueText != null) dialogueText.text = dialogue;
        if (relationshipText != null) relationshipText.text = $"Relationship: {relation}";
    }

    public void ClearDialogue()
    {
        if (npcNameText != null) npcNameText.text = "";
        if (dialogueText != null) dialogueText.text = "";
        if (relationshipText != null) relationshipText.text = "";
    }
}
CS

cat > Assets/Scripts/UI/BossDebugUIController.cs <<'CS'
using UnityEngine;
using UnityEngine.UI;

public class BossDebugUIController : MonoBehaviour
{
    [SerializeField] private BossAIController targetBoss;
    [SerializeField] private Text bossNameText;
    [SerializeField] private Text bossHealthText;
    [SerializeField] private Text comboText;
    [SerializeField] private Text counterText;
    [SerializeField] private Text phaseText;

    private void Update()
    {
        if (targetBoss == null)
        {
            targetBoss = FindObjectOfType<BossAIController>();
            if (targetBoss == null) return;
        }

        if (bossNameText != null)
            bossNameText.text = targetBoss.GetBossName();

        if (bossHealthText != null)
            bossHealthText.text = $"HP: {targetBoss.GetCurrentHealth():0}/{targetBoss.GetMaxHealth():0}";

        if (comboText != null)
            comboText.text = $"Observed Combo: {targetBoss.GetLastObservedCombo()}";

        if (counterText != null)
            counterText.text = $"Counter: {targetBoss.GetLastCounterUsed()}";

        if (phaseText != null)
            phaseText.text = $"Phase: {targetBoss.GetDifficultyPhase()}";
    }
}
CS

echo
echo "Done."
echo "Open Unity and let it reimport scripts."
echo "Then create / configure scenes and inspector references."
