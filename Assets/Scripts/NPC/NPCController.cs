using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string npcId = "merchant_01";
    [SerializeField] private string personality = "merchant";

    [Header("Boss Flag")]
    [SerializeField] private bool isBoss = false;

    private NPCMemory memory;
    private string lastResponse = "";
    private string secondLastResponse = "";

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

        string response = GenerateAdaptiveResponse(playerAction);
        int delta = CalculateRelationshipDelta(playerAction);

        NPCMemoryManager.Instance.RecordInteraction(npcId, playerAction, response, "ok", delta);
        memory = NPCMemoryManager.Instance.LoadNPCMemory(npcId, personality);

        secondLastResponse = lastResponse;
        lastResponse = response;

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
            case "bye": return 0;
            default: return 0;
        }
    }

    private string GenerateAdaptiveResponse(string action)
    {
        int score = memory != null ? memory.relationshipScore : 0;
        string relation = NPCMemoryManager.Instance.GetRelationshipLevel(score);
        string lowerPersonality = personality.ToLowerInvariant();

        string callback = BuildMemoryCallback(action, relation);
        List<string> pool = BuildResponsePool(lowerPersonality, action, relation);

        string baseResponse = PickNonRepeating(pool);

        if (!string.IsNullOrEmpty(callback))
            return callback + " " + baseResponse;

        return baseResponse;
    }

    private string BuildMemoryCallback(string action, string relation)
    {
        if (memory == null || memory.interactions == null || memory.interactions.Count == 0)
            return "";

        NPCInteraction last = memory.interactions[memory.interactions.Count - 1];

        if (action == "trade" && last.playerAction == "trade")
            return "Back for another deal?";
        if (action == "help" && last.playerAction == "help")
            return "You've been reliable.";
        if (action == "greet" && relation == "Friendly")
            return "Good to see you again.";
        if (relation == "Hostile" || relation == "Enemy")
            return "I remember how you behaved before.";
        if (last.playerAction == "threaten")
            return "I haven't forgotten that tone of yours.";

        return "";
    }

    private List<string> BuildResponsePool(string persona, string action, string relation)
    {
        List<string> lines = new List<string>();

        if (persona.Contains("merchant"))
        {
            if (relation == "Allied" || relation == "Friendly")
            {
                if (action == "trade")
                {
                    lines.Add("I've got better goods for friends like you.");
                    lines.Add("Take your time. I'll make sure you get a fair deal.");
                    lines.Add("For you, I might even lower the price.");
                }
                else if (action == "help")
                {
                    lines.Add("You have my thanks. I won't forget your help.");
                    lines.Add("You've earned my trust.");
                    lines.Add("That kindness means something in a place like this.");
                }
                else
                {
                    lines.Add("Welcome back, traveler.");
                    lines.Add("Always good to see a familiar face.");
                    lines.Add("You return at a good time.");
                }
            }
            else if (relation == "Hostile" || relation == "Enemy")
            {
                if (action == "trade")
                {
                    lines.Add("If you're buying, speak quickly.");
                    lines.Add("I don't trust you, but coin is coin.");
                    lines.Add("Keep this brief.");
                }
                else if (action == "threaten")
                {
                    lines.Add("Save your threats for someone easier to scare.");
                    lines.Add("I've had enough of your attitude.");
                    lines.Add("Watch yourself.");
                }
                else
                {
                    lines.Add("What do you want?");
                    lines.Add("State your business.");
                    lines.Add("I'm listening, but not for long.");
                }
            }
            else
            {
                if (action == "trade")
                {
                    lines.Add("Take a look. I may have what you need.");
                    lines.Add("Let's see if anything here interests you.");
                    lines.Add("Browse if you want, but don't waste my time.");
                }
                else if (action == "help")
                {
                    lines.Add("You have my thanks.");
                    lines.Add("That was decent of you.");
                    lines.Add("I appreciate the help.");
                }
                else if (action == "bye")
                {
                    lines.Add("Safe travels.");
                    lines.Add("Come back if you need supplies.");
                    lines.Add("Until next time.");
                }
                else
                {
                    lines.Add("Welcome, traveler. What do you need?");
                    lines.Add("Looking for something?");
                    lines.Add("You've got my attention.");
                }
            }
        }
        else if (persona.Contains("guard"))
        {
            if (relation == "Friendly" || relation == "Allied")
            {
                lines.Add("Stay alert out there.");
                lines.Add("You've done well so far. Keep it that way.");
                lines.Add("You've earned a bit more trust than most.");
            }
            else if (relation == "Hostile" || relation == "Enemy")
            {
                lines.Add("One wrong move and we're done talking.");
                lines.Add("I don't trust you.");
                lines.Add("Keep your hands where I can see them.");
            }
            else
            {
                lines.Add("Move along and keep the peace.");
                lines.Add("Stay out of trouble.");
                lines.Add("I've got my eye on you.");
            }
        }
        else
        {
            if (relation == "Friendly" || relation == "Allied")
            {
                lines.Add("I'm glad you came by.");
                lines.Add("You have my attention, friend.");
                lines.Add("It is good to speak again.");
            }
            else if (relation == "Hostile" || relation == "Enemy")
            {
                lines.Add("I'm not in the mood for this.");
                lines.Add("Be careful with your words.");
                lines.Add("You test my patience.");
            }
            else
            {
                lines.Add("I see.");
                lines.Add("Go on.");
                lines.Add("What is it?");
            }
        }

        return lines;
    }

    private string PickNonRepeating(List<string> pool)
    {
        if (pool == null || pool.Count == 0)
            return "I have nothing to say.";

        List<string> filtered = new List<string>();
        foreach (string line in pool)
        {
            if (line != lastResponse && line != secondLastResponse)
            {
                filtered.Add(line);
            }
        }

        if (filtered.Count == 0)
            filtered = pool;

        int index = Random.Range(0, filtered.Count);
        return filtered[index];
    }
}
