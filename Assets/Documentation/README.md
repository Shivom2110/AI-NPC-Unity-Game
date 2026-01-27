# 🎮 AI-Driven NPC Memory System for Unity

A complete implementation of AI-powered NPCs that remember player interactions, learn from behavior patterns, and adapt their responses dynamically using OpenAI and Firebase.

## ✨ Features

### Core Capabilities
- **🧠 Persistent Memory**: NPCs remember every interaction across game sessions
- **📈 Relationship Tracking**: Dynamic relationship scores that affect NPC behavior
- **🎯 Pattern Learning**: NPCs detect and adapt to player patterns
- **💬 Dynamic Dialogue**: AI-generated responses based on personality and history
- **🤖 Adaptive Boss AI**: Bosses learn combat strategies and counter player tactics
- **☁️ Cloud Storage**: All memories persist in Firebase Firestore

### NPC Types
1. **Regular NPCs**: Merchants, guards, villagers with personality-driven dialogue
2. **Boss NPCs**: Three intelligent bosses that adapt to player combat style
3. **Companion NPCs**: Allies who develop deeper bonds over time

## 🚀 Quick Start

### Prerequisites
- Unity 2021.3+ 
- Firebase account (free tier works)
- OpenAI API account (~$10/month for 100 players)

### Installation (5 minutes)

1. **Clone/Download** this project into your Unity Assets folder

2. **Install Firebase**:
   ```
   Download: https://firebase.google.com/download/unity
   Import: FirebaseFirestore.unitypackage
   ```

3. **Set up Firebase Project**:
   - Create project at https://console.firebase.google.com/
   - Enable Firestore Database
   - Download `google-services.json` → place in `Assets/`

4. **Get OpenAI API Key**:
   - Get key from https://platform.openai.com/api-keys
   - Paste into `OpenAIService` component in Unity Inspector

5. **Test Scene Setup**:
   ```
   - Create empty GameObject: "GameManager" + attach GameInitializer.cs
   - Create GameObject: "Player" (tag: Player) + attach PlayerInteractionManager.cs
   - Create GameObject: "NPC" + attach NPCController.cs
   - Assign UI references in Inspector
   ```

6. **Press Play!** Talk to NPCs and watch them remember you.

## 📁 Project Structure

```
Scripts/
├── Core/
│   ├── NPCMemoryManager.cs       # Firebase integration & memory handling
│   ├── OpenAIService.cs          # OpenAI API integration
│   └── GameInitializer.cs        # System initialization
│
├── NPC/
│   ├── NPCController.cs          # Main NPC behavior controller
│   ├── NPCPresets.cs             # Predefined NPC personalities
│   └── BossAIController.cs       # Boss-specific combat AI
│
├── Player/
│   ├── PlayerInteractionManager.cs  # Player dialogue system
│   └── PlayerCombatController.cs    # Combat input handling
│
└── Config/
    └── npc_config.json           # JSON configuration template

Documentation/
├── SETUP_GUIDE.md               # Detailed setup instructions
└── README.md                    # This file
```

## 🎯 How It Works

### System Flow
```
Player Action → NPCController → Load Memory (Firebase) 
                                     ↓
              Display Response ← OpenAI API ← Build Context
                                     ↓
                                Save Memory (Firebase)
```

### Memory Structure
```javascript
{
  "npcId": "merchant_01",
  "personality": "friendly",
  "relationshipScore": 25,
  "interactions": [
    {
      "timestamp": "2024-01-15T10:30:00",
      "playerAction": "helped_with_quest",
      "npcResponse": "Thank you! Here's a discount!",
      "outcome": "completed",
      "relationshipChange": 5
    }
  ],
  "learnedPatterns": {
    "prefers_ranged_combat": "true",
    "dodge_direction": "left",
    "play_style": "aggressive"
  }
}
```

## 🎮 Usage Examples

### Basic NPC Interaction
```csharp
// In your player script
NPCController npc = GetComponent<NPCController>();
npc.InteractWithPlayer("Hello, do you have any quests?");

// NPC will respond based on personality and relationship
// Response is AI-generated and considers all past interactions
```

### Boss Combat AI
```csharp
// Boss automatically learns from player
BossAIController boss = GetComponent<BossAIController>();

// When player attacks
boss.OnPlayerCombatAction("heavy_attack");

// Boss adapts strategy
// - If player uses heavy attacks often → Boss will bait and counter
// - If player dodges left → Boss will feint left, attack right
// - If player is aggressive → Boss becomes defensive
```

### Checking Relationships
```csharp
NPCMemory memory = npcController.GetMemory();
int score = memory.relationshipScore;

if (score >= 50) {
    Debug.Log("NPC is now your ally!");
    // Unlock special dialogue, quests, shop discounts
}
else if (score <= -50) {
    Debug.Log("NPC is hostile!");
    // NPC refuses service, may attack
}
```

## 🎨 Creating Custom NPCs

### Method 1: Use Presets
```csharp
// Use predefined personality from NPCPresets.cs
npcId = "my_merchant";
personality = "friendly";
// System automatically uses appropriate prompts
```

### Method 2: Custom Configuration
```csharp
// In NPCController Inspector:
NPC ID: "village_elder"
Personality: "wise_elder"
Initial Relationship: 0

// In OpenAI system prompt (auto-generated):
"You are a wise elder who speaks thoughtfully..."
```

### Method 3: JSON Configuration
```json
{
  "id": "custom_npc",
  "personality": "mysterious",
  "systemPrompt": "You are a mysterious figure who...",
  "traits": ["cryptic", "helpful", "enigmatic"]
}
```

## 🤖 Boss Design Examples

### Boss 1: The Adaptive Warrior
- **Learning Focus**: Combat patterns
- **Behavior**: Analyzes player's preferred attacks and counters them
- **Dialogue**: "I see you favor heavy attacks. How predictable."

### Boss 2: The Illusionist  
- **Learning Focus**: Psychological patterns
- **Behavior**: Creates fake openings, punishes hesitation
- **Dialogue**: "You hesitated there. Just like last time."

### Boss 3: The Culmination
- **Learning Focus**: Everything from previous encounters
- **Behavior**: Combines all learned strategies
- **Dialogue**: "I've watched your entire journey. I know you."

## 💰 Cost Estimation

### OpenAI API (per 100 active players/month)
- Regular NPCs (GPT-3.5-turbo): $2-5
- Boss encounters (GPT-4): $5-10
- **Total: ~$10-15/month**

### Firebase (Free Tier)
- 50K reads/day
- 20K writes/day
- 1GB storage
- **Cost: $0** (for small games)

## 🔧 Configuration

### OpenAI Settings
```csharp
// In OpenAIService.cs
modelForRegularNPCs = "gpt-3.5-turbo";  // Cheaper, fast
modelForBosses = "gpt-4";                // Smarter, strategic
temperature = 0.7;                       // Creativity level
maxTokens = 150;                         // Response length
```

### Memory Management
```csharp
// In NPCMemoryManager.cs
MAX_INTERACTIONS_STORED = 10;  // Keep last 10 interactions
// Older interactions are auto-summarized
```

## 🐛 Troubleshooting

| Issue | Solution |
|-------|----------|
| "Firebase not initialized" | Check `google-services.json` in Assets/ |
| "OpenAI API Error 401" | Verify API key has credits |
| "NPC not responding" | Ensure GameInitializer is in scene |
| "Responses too slow" | Use GPT-3.5 instead of GPT-4 |
| "Cost too high" | Reduce maxTokens, cache common responses |

## 📊 Performance Tips

1. **Reduce API Calls**:
   - Cache responses for common interactions
   - Use cheaper model (GPT-3.5) for regular NPCs
   - Summarize memory before sending to AI

2. **Optimize Firebase**:
   - Batch write operations
   - Load NPC memory only when needed
   - Clear old interactions after summarization

3. **Smart Caching**:
   ```csharp
   // Cache in memory during gameplay
   Dictionary<string, NPCMemory> npcCache;
   // Only write to Firebase on important changes
   ```

## 🎓 Learning Resources

- **OpenAI Prompt Engineering**: https://platform.openai.com/docs/guides/prompt-engineering
- **Firebase Unity Guide**: https://firebase.google.com/docs/unity/setup
- **Unity TextMeshPro**: https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0

## 🚀 Roadmap / Future Features

- [ ] Voice synthesis for NPC dialogue
- [ ] Multi-NPC conversations (NPCs talk to each other)
- [ ] Emotion visualization (facial expressions)
- [ ] Dynamic quest generation based on player history
- [ ] Cross-NPC memory sharing (NPCs gossip about player)
- [ ] Local AI model support (no API costs)
- [ ] Analytics dashboard for player interaction patterns

## 👥 Team

- **Shaan**: UI & Gameplay Interaction
- **Ayush**: OpenAI API & NPC Behavior Logic  
- **Shivom**: Firebase Backend & System Integration

## 📝 License

This is a learning project. Feel free to use and modify for your own games!

## 🤝 Contributing

Found a bug? Want to add features? 
1. Create an issue describing the problem/feature
2. Fork the repo
3. Submit a pull request

## ⚠️ Important Notes

- **Never commit API keys** to version control
- Use **test mode** in Firebase during development
- **Monitor costs** on OpenAI dashboard regularly
- **Test thoroughly** before deploying to players
- **Back up** Firebase data before major changes

## 🎉 Getting Started Checklist

- [ ] Firebase project created and connected
- [ ] OpenAI API key added (with credits)
- [ ] Test scene set up with one NPC
- [ ] Successfully tested one conversation
- [ ] Verified memory persists across sessions
- [ ] Created first boss with adaptive AI
- [ ] Team members have access to Firebase/OpenAI

---

**Ready to build intelligent NPCs?** Start with the [SETUP_GUIDE.md](SETUP_GUIDE.md) for detailed instructions!

**Questions?** Check the troubleshooting section or create an issue.

**Good luck with your project! 🎮✨**
