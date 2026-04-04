using System;
using System.Collections.Generic;

[Serializable]
public class NPCInteraction
{
    public string timestampIso;
    public string playerAction;
    public string npcResponse;
    public string outcome;
    public int relationshipChange;
}

[Serializable]
public class NPCMemory
{
    public string npcId;
    public string personality;
    public int relationshipScore;

    public List<NPCInteraction> interactions = new List<NPCInteraction>();
    public Dictionary<string, string> learnedPatterns = new Dictionary<string, string>();

    public NPCMemory(string id, string personalityName, int startingRelationship = 0)
    {
        npcId = id;
        personality = personalityName;
        relationshipScore = startingRelationship;
    }
}
