using System.Threading.Tasks;
using UnityEngine;

public class NPCController : MonoBehaviour
{
    [Header("Boss Settings")]
    [SerializeField] private bool isBoss = false;

    public bool IsBoss() => isBoss;

    [Header("NPC Identity")]
    [SerializeField] private string npcId = "npc_01";
    [SerializeField] private string personality = "neutral";

    private NPCMemory memory;

    private async void Start()
    {
        if (NPCMemoryManager.Instance == null)
        {
            Debug.LogWarning("[NPCController] NPCMemoryManager missing in scene. Add it to a Systems GameObject.");
            return;
        }

        memory = await NPCMemoryManager.Instance.LoadNPCMemory(npcId, personality);
    }

    public string GetNPCId() => npcId;

    public NPCMemory GetMemory() => memory;

    // Old code expects this async interaction style
    public async Task<string> InteractWithPlayer(string playerAction)
    {
        if (NPCMemoryManager.Instance == null)
            return "(memory system not ready)";

        if (memory == null)
            memory = await NPCMemoryManager.Instance.LoadNPCMemory(npcId, personality);

        string response = GenerateLocalResponse(playerAction);

        int delta = 0;
        if (playerAction.Contains("help")) delta = +5;
        if (playerAction.Contains("threat")) delta = -10;

        await NPCMemoryManager.Instance.RecordInteraction(npcId, playerAction, response, "ok", delta);
        return response;
    }

    private string GenerateLocalResponse(string action)
    {
        int score = memory != null ? memory.relationshipScore : 0;
        string rel = NPCMemoryManager.Instance != null ? NPCMemoryManager.Instance.GetRelationshipLevel(score) : "Neutral";

        // Personality flavored responses (placeholder)
        string p = personality.ToLower();

        if (p.Contains("merchant"))
        {
            if (rel == "Friendly" || rel == "Allied") return "Ah! Always good to see you. Need supplies?";
            if (rel == "Hostile" || rel == "Enemy") return "Browse quickly. I’m watching you.";
            return "Welcome. Take a look around.";
        }

        if (p.Contains("guard"))
        {
            if (rel == "Friendly" || rel == "Allied") return "Stay out of trouble and we’re fine.";
            if (rel == "Hostile" || rel == "Enemy") return "One step wrong and you’re done.";
            return "Move along.";
        }

        if (action.Contains("hello") || action.Contains("greet")) return "Greetings.";
        if (action.Contains("bye") || action.Contains("leave")) return "Farewell.";
        if (action.Contains("help")) return "I appreciate that.";
        if (action.Contains("threat")) return "Careful.";
        return "I see.";
    }
}
