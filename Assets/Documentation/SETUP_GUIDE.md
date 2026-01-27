# AI-Driven NPC System - Unity Implementation Guide

## 🎮 Overview
This system creates NPCs that remember past interactions, learn from player behavior, and adapt their responses using OpenAI and Firebase.

## 📦 Required Packages

### Unity Packages (Install via Package Manager)
1. **TextMeshPro** (UI text)
2. **Firebase Unity SDK**
   - Download from: https://firebase.google.com/download/unity
   - Import: FirebaseFirestore.unitypackage
   - Import: FirebaseAuth.unitypackage (optional)

### External Dependencies
- **OpenAI API Account** (https://platform.openai.com/)
- **Firebase Project** (https://console.firebase.google.com/)

---

## 🚀 Setup Instructions

### Step 1: Firebase Setup

1. **Create Firebase Project**
   - Go to https://console.firebase.google.com/
   - Click "Add project"
   - Follow the setup wizard

2. **Enable Firestore Database**
   - In Firebase Console, go to Firestore Database
   - Click "Create database"
   - Start in test mode (for development)
   - Choose your region

3. **Add Unity App to Firebase**
   - In Project Settings, click "Add app" → Unity
   - Download `google-services.json` (Android) or `GoogleService-Info.plist` (iOS)
   - Place file in `Assets/` folder in Unity

4. **Configure Firestore Rules** (for testing)
   ```
   rules_version = '2';
   service cloud.firestore {
     match /databases/{database}/documents {
       match /npc_memories/{document=**} {
         allow read, write: if true; // Change this for production!
       }
     }
   }
   ```

### Step 2: OpenAI Setup

1. **Get API Key**
   - Go to https://platform.openai.com/api-keys
   - Create new secret key
   - Copy and save it securely

2. **Add Credits**
   - Add payment method to your OpenAI account
   - Start with $5-10 for testing

3. **Set API Key in Unity**
   - Select OpenAIService in Hierarchy
   - Paste API key in Inspector
   - **Never commit API key to version control!**

### Step 3: Unity Scene Setup

1. **Create Empty GameObjects:**
   ```
   - GameManager (attach GameInitializer.cs)
   - NPCMemoryManager (auto-created, or create manually)
   - OpenAIService (auto-created, or create manually)
   ```

2. **Create Player:**
   ```
   - Player GameObject with tag "Player"
   - Attach PlayerInteractionManager.cs
   - Add Collider component
   ```

3. **Create Regular NPC:**
   ```
   - NPC GameObject
   - Add Collider (set as Trigger)
   - Attach NPCController.cs
   - Set NPC ID (e.g., "merchant_01")
   - Set personality (friendly, aggressive, mysterious)
   - Create UI Canvas for dialogue
   ```

4. **Create Boss NPC:**
   ```
   - Boss GameObject
   - Add Collider
   - Attach NPCController.cs (set isBoss = true)
   - Attach BossAIController.cs
   - Set boss ID (e.g., "boss_fire_demon")
   ```

---

## 🎨 UI Setup

### Dialogue Panel
Create a Canvas with:
- Panel (background)
- NPC Name Text (TextMeshPro)
- Dialogue Text (TextMeshPro)
- Relationship Text (TextMeshPro)

### Dialogue Choices Panel
- Panel (background)
- 3-4 Buttons with TextMeshPro text
- Assign to PlayerInteractionManager

---

## 💰 Cost Estimates

### OpenAI API Costs
- **GPT-3.5-turbo** (regular NPCs): $0.0015 / 1K tokens
  - ~150 tokens per interaction
  - ~$0.0002 per NPC conversation
  
- **GPT-4** (bosses): $0.03 / 1K tokens
  - ~200 tokens per boss strategy
  - ~$0.006 per boss interaction

**Monthly estimate for 100 active players:**
- Regular NPC chats: ~$2-5
- Boss encounters: ~$5-10
- **Total: ~$10-15/month**

### Firebase Costs
- **Firestore**: Free tier includes
  - 1GB storage
  - 50K reads/day
  - 20K writes/day
- Should be free for small-scale testing

---

## 🔧 How to Use

### Basic NPC Interaction
```csharp
// In your player script
NPCController npc = hitNPC.GetComponent<NPCController>();
npc.InteractWithPlayer("Hello, how are you?");
```

### Boss Combat
```csharp
// In your combat system
BossAIController boss = bossObject.GetComponent<BossAIController>();

// When player attacks
boss.OnPlayerCombatAction("heavy_attack");

// Boss will adapt strategy automatically
```

### Checking Relationships
```csharp
NPCMemory memory = npcController.GetMemory();
int relationshipScore = memory.relationshipScore;

if (relationshipScore >= 50) {
    // NPC is allied - give benefits
}
```

---

## 📊 Data Structure

### Firebase Collections
```
npc_memories/
  ├── npc_guard_01/
  │   ├── npcId: "npc_guard_01"
  │   ├── personality: "friendly"
  │   ├── relationshipScore: 15
  │   ├── interactions: [...]
  │   └── learnedPatterns: {...}
  └── boss_fire_demon/
      └── ...
```

---

## 🎯 Customization Guide

### Adding New Personalities
In `OpenAIService.cs`, modify `BuildSystemPrompt()`:
```csharp
case "mysterious":
    basePrompt += "You speak in riddles and cryptic hints...";
    break;
```

### Adding New Combat Actions
In `BossAIController.cs`, add to `PerformCombatAction()`:
```csharp
case "laser_beam":
    LaserBeamAttack();
    break;
```

### Custom Pattern Detection
In `OpenAIService.cs`, modify `AnalyzePlayerPattern()`:
```csharp
if (playerAction.Contains("magic_spell")) {
    memory.learnedPatterns["prefers_magic"] = "true";
}
```

---

## 🐛 Troubleshooting

### "Firebase not initialized"
- Check google-services.json is in Assets/
- Verify Firebase package is imported
- Check Firebase Console for app configuration

### "OpenAI API Error 401"
- Verify API key is correct
- Check OpenAI account has credits
- Ensure API key hasn't expired

### "NPC not responding"
- Check GameInitializer is in scene
- Verify all managers are initialized
- Check Console for error messages
- Ensure internet connection is active

### "Firebase quota exceeded"
- You've hit free tier limits
- Check Firebase Console usage
- Consider upgrading plan or optimizing queries

### "Responses too slow"
- Use GPT-3.5-turbo instead of GPT-4
- Reduce max_tokens in OpenAIService
- Cache common responses
- Consider local summarization before AI call

---

## 🎓 Example Scenarios

### Scenario 1: Friendly Merchant
```
Player: "Hello!"
NPC: "Welcome back! I remember you helped me last week. Here's a discount!"
[Relationship: +5]
```

### Scenario 2: Boss Learning
```
Turn 1: Player dodges left 3 times
Boss AI: [learns pattern: "player_prefers_left_dodge"]

Turn 2: Boss feints left, attacks right
Player gets hit!

Turn 3: Boss continues exploiting weakness
```

### Scenario 3: Hostile Turn
```
Player: "Get out of my way!"
NPC: "How dare you! I won't forget this insult."
[Relationship: -10 → Becomes hostile]
[Shop closes, future interactions affected]
```

---

## 🚀 Advanced Features (Future Enhancements)

1. **Voice Synthesis**: Add TTS for NPC dialogue
2. **Emotion System**: Track NPC emotional states
3. **Quest Generation**: AI creates dynamic quests
4. **Multi-NPC Conversations**: NPCs reference each other
5. **World Events**: NPCs remember major game events
6. **Player Profiling**: Cross-NPC knowledge sharing

---

## 📝 Team Task Distribution

### Shaan (UI & Gameplay)
- Design dialogue UI panels
- Create interaction prompts
- Implement player input handling
- Design boss combat UI

### Ayush (OpenAI & Logic)
- Integrate OpenAI API
- Design prompt templates
- Implement pattern recognition
- Fine-tune AI responses

### Shivom (Firebase & Integration)
- Set up Firebase project
- Design data schema
- Implement memory persistence
- System integration testing

---

## 📚 Resources

- **OpenAI Documentation**: https://platform.openai.com/docs
- **Firebase Unity Guide**: https://firebase.google.com/docs/unity/setup
- **Unity TextMeshPro**: https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0

---

## ⚠️ Important Notes

1. **Never commit API keys** to version control
2. **Use test mode** for Firebase during development
3. **Monitor API costs** regularly
4. **Implement rate limiting** for production
5. **Test with small number of NPCs** first
6. **Back up Firebase data** regularly

---

## 🎉 Next Steps

1. Complete Firebase and OpenAI setup
2. Create one test NPC
3. Test basic dialogue interaction
4. Implement one boss with learning
5. Expand to full game with 3 bosses
6. Polish UI and add animations
7. Optimize for production

Good luck with your project! 🚀
