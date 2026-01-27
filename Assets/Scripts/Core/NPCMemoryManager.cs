using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

[System.Serializable]
public class NPCMemory
{
    public string npcId;
    public string personality;
    public int relationshipScore;
    public List<InteractionRecord> interactions;
    public Dictionary<string, string> learnedPatterns;
    public DateTime lastInteraction;

    public NPCMemory(string id, string personalityType)
    {
        npcId = id;
        personality = personalityType;
        relationshipScore = 0;
        interactions = new List<InteractionRecord>();
        learnedPatterns = new Dictionary<string, string>();
        lastInteraction = DateTime.UtcNow;
    }
}

[System.Serializable]
public class InteractionRecord
{
    public string timestamp;
    public string playerAction;
    public string npcResponse;
    public string outcome;
    public int relationshipChange;

    public InteractionRecord(string action, string response, string result, int relChange = 0)
    {
        timestamp = DateTime.UtcNow.ToString("o");
        playerAction = action;
        npcResponse = response;
        outcome = result;
        relationshipChange = relChange;
    }
}

public class NPCMemoryManager : MonoBehaviour
{
    private static NPCMemoryManager _instance;
    public static NPCMemoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NPCMemoryManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("NPCMemoryManager");
                    _instance = go.AddComponent<NPCMemoryManager>();
                }
            }
            return _instance;
        }
    }

    private FirebaseFirestore db;
    private Dictionary<string, NPCMemory> npcMemoryCache = new Dictionary<string, NPCMemory>();
    private const int MAX_INTERACTIONS_STORED = 10; // Keep only recent interactions

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize()
    {
        db = FirebaseFirestore.DefaultInstance;
        Debug.Log("NPCMemoryManager initialized with Firebase");
    }

    // Load NPC memory from Firebase
    public async Task<NPCMemory> LoadNPCMemory(string npcId, string defaultPersonality = "neutral")
    {
        // Check cache first
        if (npcMemoryCache.ContainsKey(npcId))
        {
            return npcMemoryCache[npcId];
        }

        try
        {
            DocumentReference docRef = db.Collection("npc_memories").Document(npcId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                NPCMemory memory = snapshot.ConvertTo<NPCMemory>();
                npcMemoryCache[npcId] = memory;
                Debug.Log($"Loaded memory for {npcId} with {memory.interactions.Count} interactions");
                return memory;
            }
            else
            {
                // Create new memory for this NPC
                NPCMemory newMemory = new NPCMemory(npcId, defaultPersonality);
                npcMemoryCache[npcId] = newMemory;
                await SaveNPCMemory(newMemory);
                Debug.Log($"Created new memory for {npcId}");
                return newMemory;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading NPC memory: {e.Message}");
            // Return default memory on error
            return new NPCMemory(npcId, defaultPersonality);
        }
    }

    // Save NPC memory to Firebase
    public async Task SaveNPCMemory(NPCMemory memory)
    {
        try
        {
            // Trim old interactions if too many
            if (memory.interactions.Count > MAX_INTERACTIONS_STORED)
            {
                memory.interactions.RemoveRange(0, memory.interactions.Count - MAX_INTERACTIONS_STORED);
            }

            memory.lastInteraction = DateTime.UtcNow;

            DocumentReference docRef = db.Collection("npc_memories").Document(memory.npcId);
            await docRef.SetAsync(memory);
            
            // Update cache
            npcMemoryCache[memory.npcId] = memory;
            
            Debug.Log($"Saved memory for {memory.npcId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving NPC memory: {e.Message}");
        }
    }

    // Add new interaction to NPC memory
    public async Task RecordInteraction(string npcId, string playerAction, string npcResponse, 
                                       string outcome, int relationshipChange = 0)
    {
        NPCMemory memory = await LoadNPCMemory(npcId);
        
        InteractionRecord record = new InteractionRecord(playerAction, npcResponse, outcome, relationshipChange);
        memory.interactions.Add(record);
        memory.relationshipScore += relationshipChange;

        await SaveNPCMemory(memory);
    }

    // Update learned patterns (e.g., player preferences)
    public async Task UpdateLearnedPattern(string npcId, string patternKey, string patternValue)
    {
        NPCMemory memory = await LoadNPCMemory(npcId);
        memory.learnedPatterns[patternKey] = patternValue;
        await SaveNPCMemory(memory);
    }

    // Get relationship level as string
    public string GetRelationshipLevel(int score)
    {
        if (score >= 50) return "Allied";
        if (score >= 20) return "Friendly";
        if (score >= -20) return "Neutral";
        if (score >= -50) return "Hostile";
        return "Enemy";
    }

    // Generate summary of recent interactions for AI context
    public string GenerateMemorySummary(NPCMemory memory)
    {
        string summary = $"NPC: {memory.npcId}\n";
        summary += $"Personality: {memory.personality}\n";
        summary += $"Relationship: {GetRelationshipLevel(memory.relationshipScore)} (Score: {memory.relationshipScore})\n\n";

        if (memory.learnedPatterns.Count > 0)
        {
            summary += "Learned Patterns:\n";
            foreach (var pattern in memory.learnedPatterns)
            {
                summary += $"- {pattern.Key}: {pattern.Value}\n";
            }
            summary += "\n";
        }

        if (memory.interactions.Count > 0)
        {
            summary += "Recent Interactions:\n";
            int recentCount = Mathf.Min(5, memory.interactions.Count);
            for (int i = memory.interactions.Count - recentCount; i < memory.interactions.Count; i++)
            {
                var interaction = memory.interactions[i];
                summary += $"- Player: {interaction.playerAction} → NPC: {interaction.npcResponse} → {interaction.outcome}\n";
            }
        }
        else
        {
            summary += "No previous interactions.\n";
        }

        return summary;
    }
}
