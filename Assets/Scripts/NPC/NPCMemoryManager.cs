using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Singleton that owns all NPC memories, persists them to disk, and provides
/// query/record helpers used by NPCController.
/// </summary>
public class NPCMemoryManager : MonoBehaviour
{
    public static NPCMemoryManager Instance { get; private set; }

    private const string SaveFileName = "npc_memories.json";

    // In-memory store keyed by npcId
    private readonly Dictionary<string, NPCMemory> _memories = new Dictionary<string, NPCMemory>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAllFromDisk();
    }

    private void OnApplicationQuit() => SaveAllToDisk();

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the memory for a given NPC, creating a fresh one if none exists.
    /// </summary>
    public NPCMemory LoadNPCMemory(string npcId, string personality)
    {
        if (!_memories.TryGetValue(npcId, out NPCMemory mem))
        {
            mem = new NPCMemory(npcId, personality, 0);
            _memories[npcId] = mem;
        }
        return mem;
    }

    /// <summary>
    /// Records a player→NPC interaction and saves it.
    /// </summary>
    public void RecordInteraction(string npcId, string playerAction,
                                  string npcResponse, string outcome, int relationshipDelta)
    {
        if (!_memories.TryGetValue(npcId, out NPCMemory mem)) return;

        mem.interactions.Add(new NPCInteraction
        {
            timestampIso     = DateTime.UtcNow.ToString("o"),
            playerAction     = playerAction,
            npcResponse      = npcResponse,
            outcome          = outcome,
            relationshipChange = relationshipDelta
        });

        mem.relationshipScore = Mathf.Clamp(mem.relationshipScore + relationshipDelta, -100, 100);

        // Keep interaction list bounded
        if (mem.interactions.Count > 50)
            mem.interactions.RemoveAt(0);

        SaveAllToDisk();
    }

    /// <summary>
    /// Updates an NPC's relationship score directly.
    /// </summary>
    public void UpdateRelationship(string npcId, int delta)
    {
        if (_memories.TryGetValue(npcId, out NPCMemory mem))
        {
            mem.relationshipScore = Mathf.Clamp(mem.relationshipScore + delta, -100, 100);
            SaveAllToDisk();
        }
    }

    /// <summary>
    /// Human-readable relationship tier for a given score.
    /// </summary>
    public static string GetRelationshipLevel(int score)
    {
        if (score >= 40)  return "Allied";
        if (score >= 15)  return "Friendly";
        if (score >= -10) return "Neutral";
        if (score >= -30) return "Hostile";
        return "Enemy";
    }

    // ── Persistence ────────────────────────────────────────────────────────────

    private void SaveAllToDisk()
    {
        try
        {
            SaveData wrapper = new SaveData();
            foreach (var pair in _memories)
                wrapper.entries.Add(new MemorySaveEntry
                {
                    npcId             = pair.Key,
                    personality       = pair.Value.personality,
                    relationshipScore = pair.Value.relationshipScore,
                    interactions      = pair.Value.interactions
                });

            File.WriteAllText(GetSavePath(), JsonUtility.ToJson(wrapper, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[NPCMemoryManager] Save failed: {e.Message}");
        }
    }

    private void LoadAllFromDisk()
    {
        string path = GetSavePath();
        if (!File.Exists(path)) return;

        try
        {
            SaveData wrapper = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (wrapper?.entries == null) return;

            foreach (var entry in wrapper.entries)
            {
                var mem = new NPCMemory(entry.npcId, entry.personality, entry.relationshipScore);
                if (entry.interactions != null)
                    mem.interactions.AddRange(entry.interactions);
                _memories[entry.npcId] = mem;
            }

            Debug.Log($"[NPCMemoryManager] Loaded memories for {_memories.Count} NPCs.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[NPCMemoryManager] Load failed: {e.Message}");
        }
    }

    private static string GetSavePath() =>
        Path.Combine(Application.persistentDataPath, SaveFileName);

    // ── Serialization helpers ─────────────────────────────────────────────────

    [Serializable]
    private class SaveData
    {
        public List<MemorySaveEntry> entries = new List<MemorySaveEntry>();
    }

    [Serializable]
    private class MemorySaveEntry
    {
        public string npcId;
        public string personality;
        public int    relationshipScore;
        public List<NPCInteraction> interactions = new List<NPCInteraction>();
    }
}
