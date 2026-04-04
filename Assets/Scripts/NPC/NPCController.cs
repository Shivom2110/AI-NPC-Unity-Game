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
