using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class OpenAIRequest
{
    public string model;
    public List<Message> messages;
    public float temperature;
    public int max_tokens;
}

[System.Serializable]
public class Message
{
    public string role;
    public string content;

    public Message(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

[System.Serializable]
public class OpenAIResponse
{
    public List<Choice> choices;
}

[System.Serializable]
public class Choice
{
    public Message message;
}

public class OpenAIService : MonoBehaviour
{
    private static OpenAIService _instance;
    public static OpenAIService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<OpenAIService>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("OpenAIService");
                    _instance = go.AddComponent<OpenAIService>();
                }
            }
            return _instance;
        }
    }

    [Header("API Configuration")]
    [SerializeField] private string apiKey = "YOUR_OPENAI_API_KEY"; // Set in Inspector or load from config
    private const string API_URL = "https://api.openai.com/v1/chat/completions";

    [Header("Model Settings")]
    [SerializeField] private string modelForRegularNPCs = "gpt-3.5-turbo";
    [SerializeField] private string modelForBosses = "gpt-4";
    [SerializeField] private float temperature = 0.7f;
    [SerializeField] private int maxTokens = 150;

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

    // Generate NPC response based on context and memory
    public async Task<string> GenerateNPCResponse(string npcId, string playerAction, 
                                                   NPCMemory memory, bool isBoss = false)
    {
        string systemPrompt = BuildSystemPrompt(memory, isBoss);
        string userPrompt = $"Player action: {playerAction}\n\nRespond in character and adapt your behavior based on what you've learned about the player.";

        try
        {
            string response = await CallOpenAI(systemPrompt, userPrompt, isBoss);
            return response;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error generating NPC response: {e.Message}");
            return GetFallbackResponse(memory.personality);
        }
    }

    // Generate boss combat strategy based on learned patterns
    public async Task<string> GenerateBossCombatStrategy(string bossId, string playerCombatStyle, 
                                                          NPCMemory memory)
    {
        string systemPrompt = BuildBossCombatPrompt(memory);
        string userPrompt = $"The player is currently using this combat approach: {playerCombatStyle}\n\n" +
                          $"Based on what you've learned, suggest your next 3 combat actions as a JSON array. " +
                          $"Format: [\"action1\", \"action2\", \"action3\"]";

        try
        {
            string response = await CallOpenAI(systemPrompt, userPrompt, true);
            return response;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error generating boss strategy: {e.Message}");
            return "[\"heavy_attack\", \"dodge\", \"special_ability\"]"; // Fallback
        }
    }

    // Build system prompt with NPC personality and memory
    private string BuildSystemPrompt(NPCMemory memory, bool isBoss)
    {
        string memorySummary = NPCMemoryManager.Instance.GenerateMemorySummary(memory);
        
        string basePrompt = $@"You are {memory.npcId}, an NPC in a video game with a {memory.personality} personality.

{memorySummary}

IMPORTANT INSTRUCTIONS:
- Stay in character based on your personality and relationship with the player
- Reference past interactions naturally (don't just list them)
- Adapt your responses based on learned patterns
- Keep responses concise (1-3 sentences for dialogue)
- Show how the relationship level affects your tone
";

        if (isBoss)
        {
            basePrompt += @"
- You are a BOSS character, so be more strategic and challenging
- Reference previous encounters to show you're learning
- Your behavior should feel intelligent and adaptive, not scripted
";
        }

        return basePrompt;
    }

    // Build specialized prompt for boss combat AI
    private string BuildBossCombatPrompt(NPCMemory memory)
    {
        string memorySummary = NPCMemoryManager.Instance.GenerateMemorySummary(memory);

        return $@"You are {memory.npcId}, a boss in a video game that learns from player behavior.

{memorySummary}

COMBAT AI INSTRUCTIONS:
- Analyze the player's combat patterns from past interactions
- Exploit weaknesses you've discovered
- Avoid strategies that failed before
- Mix up your approach to stay unpredictable
- Use abilities the player struggles with more frequently
- Counter the player's preferred tactics
";
    }

    // Make actual API call to OpenAI
    private async Task<string> CallOpenAI(string systemPrompt, string userPrompt, bool useBossModel)
    {
        OpenAIRequest request = new OpenAIRequest
        {
            model = useBossModel ? modelForBosses : modelForRegularNPCs,
            messages = new List<Message>
            {
                new Message("system", systemPrompt),
                new Message("user", userPrompt)
            },
            temperature = temperature,
            max_tokens = maxTokens
        };

        string jsonRequest = JsonUtility.ToJson(request);

        using (UnityWebRequest webRequest = new UnityWebRequest(API_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string responseText = webRequest.downloadHandler.text;
                OpenAIResponse response = JsonUtility.FromJson<OpenAIResponse>(responseText);
                
                if (response.choices != null && response.choices.Count > 0)
                {
                    return response.choices[0].message.content.Trim();
                }
                else
                {
                    throw new Exception("No response from OpenAI");
                }
            }
            else
            {
                throw new Exception($"API Error: {webRequest.error}");
            }
        }
    }

    // Fallback responses if API fails
    private string GetFallbackResponse(string personality)
    {
        switch (personality.ToLower())
        {
            case "aggressive":
                return "I'll show you no mercy!";
            case "friendly":
                return "Good to see you again!";
            case "mysterious":
                return "Interesting... you've returned.";
            default:
                return "...";
        }
    }

    // Helper method to extract pattern from player actions
    public void AnalyzePlayerPattern(string playerAction, NPCMemory memory)
    {
        // Example pattern detection
        if (playerAction.Contains("dodge_left"))
        {
            if (!memory.learnedPatterns.ContainsKey("dodge_preference"))
            {
                memory.learnedPatterns["dodge_preference"] = "left";
            }
        }

        if (playerAction.Contains("ranged_attack"))
        {
            if (!memory.learnedPatterns.ContainsKey("combat_style"))
            {
                memory.learnedPatterns["combat_style"] = "ranged";
            }
        }

        // Add more pattern detection logic as needed
    }
}
