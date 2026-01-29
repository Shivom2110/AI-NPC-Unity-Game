using UnityEngine;

public class LocalNPCMemoryManager : MonoBehaviour
{
    [SerializeField] private string npcId = "npc_unknown";
    public LocalNPCMemory Memory { get; private set; }

    private void Awake()
    {
        Memory = new LocalNPCMemory
        {
            npcId = npcId,
            relationshipScore = 0
        };
    }

    public void AddNote(string note)
    {
        if (Memory.recentNotes.Count > 20)
            Memory.recentNotes.RemoveAt(0);

        Memory.recentNotes.Add(note);
    }
}
