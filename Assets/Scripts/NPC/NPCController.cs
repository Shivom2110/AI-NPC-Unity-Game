using System.Threading.Tasks;
using UnityEngine;
using TMPro;

public class NPCController : MonoBehaviour
{
    [Header("NPC Identity")]
    [SerializeField] private string npcId = "npc_guard_01";
    [SerializeField] private string npcName = "Guard";
    [SerializeField] private string personalityType = "neutral"; // aggressive, friendly, mysterious, etc.
    [SerializeField] private bool isBoss = false;

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI relationshipText;

    private NPCMemory currentMemory;
    private bool isProcessing = false;

    async void Start()
    {
        // Load NPC memory on start
        currentMemory = await NPCMemoryManager.Instance.LoadNPCMemory(npcId, personalityType);
        UpdateRelationshipUI();
    }

    // Main interaction method - call this when player talks to NPC
    public async void InteractWithPlayer(string playerAction)
    {
        if (isProcessing)
        {
            Debug.Log("Already processing interaction...");
            return;
        }

        isProcessing = true;
        ShowDialogue("...");

        try
        {
            // Generate AI response
            string npcResponse = await OpenAIService.Instance.GenerateNPCResponse(
                npcId, 
                playerAction, 
                currentMemory, 
                isBoss
            );

            // Analyze player patterns
            OpenAIService.Instance.AnalyzePlayerPattern(playerAction, currentMemory);

            // Determine relationship change based on action
            int relationshipChange = CalculateRelationshipChange(playerAction, npcResponse);

            // Record the interaction
            await NPCMemoryManager.Instance.RecordInteraction(
                npcId,
                playerAction,
                npcResponse,
                "completed",
                relationshipChange
            );

            // Update memory reference
            currentMemory = await NPCMemoryManager.Instance.LoadNPCMemory(npcId);

            // Show response to player
            ShowDialogue(npcResponse);
            UpdateRelationshipUI();

            // Trigger any gameplay consequences
            HandleInteractionConsequences(relationshipChange);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in NPC interaction: {e.Message}");
            ShowDialogue("...");
        }
        finally
        {
            isProcessing = false;
        }
    }

    // For boss NPCs - get combat strategy
    public async Task<string[]> GetBossCombatStrategy(string playerCombatStyle)
    {
        if (!isBoss) return null;

        string strategyJson = await OpenAIService.Instance.GenerateBossCombatStrategy(
            npcId,
            playerCombatStyle,
            currentMemory
        );

        // Parse JSON array of actions
        try
        {
            // Simple JSON parsing (you might want to use a proper JSON library)
            strategyJson = strategyJson.Trim('[', ']', ' ', '\n');
            string[] actions = strategyJson.Split(',');
            
            for (int i = 0; i < actions.Length; i++)
            {
                actions[i] = actions[i].Trim('"', ' ');
            }

            return actions;
        }
        catch
        {
            Debug.LogWarning("Failed to parse boss strategy, using defaults");
            return new string[] { "attack", "block", "special" };
        }
    }

    // Calculate how much the relationship should change
    private int CalculateRelationshipChange(string playerAction, string npcResponse)
    {
        int change = 0;

        // Positive actions
        if (playerAction.Contains("help") || playerAction.Contains("gift") || playerAction.Contains("compliment"))
            change += 5;
        
        if (playerAction.Contains("agree") || playerAction.Contains("support"))
            change += 3;

        // Negative actions
        if (playerAction.Contains("insult") || playerAction.Contains("threaten") || playerAction.Contains("attack"))
            change -= 5;
        
        if (playerAction.Contains("refuse") || playerAction.Contains("ignore"))
            change -= 2;

        // Analyze NPC response tone (simple sentiment analysis)
        if (npcResponse.Contains("!") && npcResponse.Contains("thank"))
            change += 2;
        
        if (npcResponse.Contains("anger") || npcResponse.Contains("disappointed"))
            change -= 1;

        return change;
    }

    // Handle gameplay consequences of relationship changes
    private void HandleInteractionConsequences(int relationshipChange)
    {
        if (currentMemory.relationshipScore >= 50 && relationshipChange > 0)
        {
            // Unlock allied benefits
            Debug.Log($"{npcName} is now your ally!");
            // TODO: Give player item, unlock quest, etc.
        }
        else if (currentMemory.relationshipScore <= -50 && relationshipChange < 0)
        {
            // Trigger hostile behavior
            Debug.Log($"{npcName} has become hostile!");
            // TODO: Start combat, close shop, etc.
        }
    }

    // UI Methods
    private void ShowDialogue(string text)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (dialogueText != null)
            dialogueText.text = text;

        if (npcNameText != null)
            npcNameText.text = npcName;
    }

    public void HideDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void UpdateRelationshipUI()
    {
        if (relationshipText != null && currentMemory != null)
        {
            string level = NPCMemoryManager.Instance.GetRelationshipLevel(currentMemory.relationshipScore);
            relationshipText.text = $"{level} ({currentMemory.relationshipScore})";
        }
    }

    // Called when player enters interaction range
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Show interaction prompt
            Debug.Log($"Press E to talk to {npcName}");
            // TODO: Show UI prompt
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            HideDialogue();
        }
    }

    // Public getters for other systems
    public NPCMemory GetMemory() => currentMemory;
    public bool IsBoss() => isBoss;
    public string GetNPCId() => npcId;
}
