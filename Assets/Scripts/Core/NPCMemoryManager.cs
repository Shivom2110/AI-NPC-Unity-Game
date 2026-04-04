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
