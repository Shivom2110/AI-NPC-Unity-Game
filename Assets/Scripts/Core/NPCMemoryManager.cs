using System.Collections.Generic;
using System.Threading.Tasks;
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
        DontDestroyOnLoad(gameObject);
        Debug.Log("[NPCMemoryManager] Local memory ready.");
    }

    // personality is optional now so older calls compile
    public Task<NPCMemory> LoadNPCMemory(string npcId, string personality = "neutral")
    {
        if (!store.TryGetValue(npcId, out var mem))
        {
            mem = new NPCMemory(npcId, personality, 0);
            store[npcId] = mem;
        }
        return Task.FromResult(mem);
    }

    public Task RecordInteraction(string npcId, string action, string response, string outcome, int delta = 0)
    {
        if (!store.TryGetValue(npcId, out var mem))
        {
            mem = new NPCMemory(npcId, "neutral", 0);
            store[npcId] = mem;
        }

        mem.relationshipScore += delta;
        mem.interactions.Add(new NPCInteraction
        {
            timestampIso = System.DateTime.UtcNow.ToString("o"),
            playerAction = action,
            npcResponse = response,
            outcome = outcome,
            relationshipChange = delta
        });

        return Task.CompletedTask;
    }

    public Task UpdateLearnedPattern(string npcId, string key, string value)
    {
        if (!store.TryGetValue(npcId, out var mem))
        {
            mem = new NPCMemory(npcId, "neutral", 0);
            store[npcId] = mem;
        }

        mem.learnedPatterns[key] = value;
        return Task.CompletedTask;
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
