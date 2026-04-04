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
