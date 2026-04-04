using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayerInteractionManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialogueChoicesPanel;
    [SerializeField] private Button[] choiceButtons; // Assign 3-4 buttons
    [SerializeField] private TextMeshProUGUI[] choiceTexts;
    
    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionRange = 3f;

    private NPCController currentNPC;
    private bool canInteract = false;

    void Update()
    {
        // Check for nearby NPCs
        CheckForNearbyNPCs();

        // Handle interaction input
        if (Input.GetKeyDown(interactKey) && canInteract && currentNPC != null)
        {
            ShowDialogueOptions();
        }
    }

    private void CheckForNearbyNPCs()
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, interactionRange);
        
        NPCController closestNPC = null;
        float closestDistance = interactionRange;

        foreach (Collider col in nearbyObjects)
        {
            NPCController npc = col.GetComponent<NPCController>();
            if (npc != null)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNPC = npc;
                }
            }
        }

        if (closestNPC != currentNPC)
        {
            currentNPC = closestNPC;
            canInteract = (currentNPC != null);
            
            if (canInteract)
            {
                Debug.Log($"Can interact with {currentNPC.GetNPCId()}");
                // TODO: Show interaction prompt UI
            }
        }
    }

    private void ShowDialogueOptions()
    {
        if (currentNPC == null) return;

        // Generate context-appropriate dialogue options
        NPCMemory memory = currentNPC.GetMemory();
        List<string> options = GenerateDialogueOptions(memory);

        // Display options in UI
        dialogueChoicesPanel.SetActive(true);

        for (int i = 0; i < choiceButtons.Length && i < options.Count; i++)
        {
            choiceTexts[i].text = options[i];
            choiceButtons[i].gameObject.SetActive(true);
            
            // Set up button click handler
            int optionIndex = i; // Capture for closure
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => SelectDialogueOption(options[optionIndex]));
        }

        // Hide unused buttons
        for (int i = options.Count; i < choiceButtons.Length; i++)
        {
            choiceButtons[i].gameObject.SetActive(false);
        }
    }

    private List<string> GenerateDialogueOptions(NPCMemory memory)
    {
        List<string> options = new List<string>();

        // Get relationship level
        string relationship = NPCMemoryManager.Instance.GetRelationshipLevel(memory.relationshipScore);

        // Generate context-appropriate options
        switch (relationship)
        {
            case "Allied":
                options.Add("Need any help with anything?");
                options.Add("Share information about a quest");
                options.Add("Ask for their assistance");
                break;
            
            case "Friendly":
                options.Add("How have you been?");
                options.Add("Tell me about yourself");
                options.Add("Any advice for me?");
                break;
            
            case "Neutral":
                options.Add("Greet them politely");
                options.Add("Ask about local rumors");
                options.Add("Inquire about their work");
                break;
            
            case "Hostile":
                options.Add("Try to make amends");
                options.Add("Challenge them");
                options.Add("Leave quietly");
                break;
            
            case "Enemy":
                options.Add("Threaten them");
                options.Add("Attempt diplomacy");
                options.Add("Prepare for combat");
                break;
        }

        // Add a generic exit option
        options.Add("Say goodbye");

        return options;
    }

    private void SelectDialogueOption(string choice)
    {
        // Hide choices panel
        dialogueChoicesPanel.SetActive(false);

        // Send interaction to NPC
        if (currentNPC != null)
        {
            currentNPC.InteractWithPlayer(choice);
        }
    }

    // Public method to force interaction (for scripted events)
    public void ForceInteraction(NPCController npc, string playerAction)
    {
        npc.InteractWithPlayer(playerAction);
    }

    // For boss combat - track player's combat actions
    public void RecordCombatAction(string action)
    {
        if (currentNPC != null && currentNPC.IsBoss())
        {
            // Record this combat action for the boss to learn from
            currentNPC.InteractWithPlayer($"combat_action: {action}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
