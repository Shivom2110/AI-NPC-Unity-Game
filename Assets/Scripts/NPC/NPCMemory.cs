using System.Collections.Generic;

[System.Serializable]
public class NPCInteraction
{
    public string timestampIso;
    public string playerAction;
    public string npcResponse;
    public string outcome;
    public int relationshipChange;
}

[System.Serializable]
public class NPCMemory
{
    public string npcId;
    public string personality;
    public int relationshipScore;
    public List<NPCInteraction> interactions = new List<NPCInteraction>();

    public NPCMemory(string id, string personalityType, int score)
    {
        npcId = id;
        personality = personalityType;
        relationshipScore = score;
    }
}
