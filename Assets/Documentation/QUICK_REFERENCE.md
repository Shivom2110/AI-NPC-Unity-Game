# 🎯 Quick Reference Cheat Sheet

## 🚀 First-Time Setup (5 minutes)

1. **Firebase** → Create project → Enable Firestore → Download `google-services.json` → Put in `Assets/`
2. **OpenAI** → Get API key from platform.openai.com → Paste in Unity Inspector on `OpenAIService`
3. **Unity** → Import Firebase packages → Create GameManager + Player + NPC → Press Play!

## 📝 Common Tasks

### Create a New NPC
```csharp
1. Add NPCController.cs to GameObject
2. Set npcId (unique, e.g., "merchant_01")
3. Set personality ("friendly", "aggressive", "mysterious")
4. Add Collider (set as Trigger)
5. Assign UI references in Inspector
```

### Create a Boss
```csharp
1. Same as regular NPC, but also:
2. Add BossAIController.cs
3. Check "Is Boss" in NPCController
4. Set combat stats (health, damage)
```

### Test NPC Interaction
```csharp
// In any script
NPCController npc = GetComponent<NPCController>();
npc.InteractWithPlayer("Hello there!");
```

### Check Relationship
```csharp
NPCMemory memory = npc.GetMemory();
Debug.Log($"Relationship: {memory.relationshipScore}");
```

## 🎮 Player Actions That Affect Relationships

| Action | Relationship Change |
|--------|---------------------|
| Help NPC | +5 to +15 |
| Give gift | +5 |
| Complete quest | +10 |
| Insult NPC | -5 |
| Attack NPC | -10 |
| Refuse help | -2 |

## 🤖 Boss Learning System

### What Bosses Learn
- Player's preferred attacks
- Dodge direction patterns  
- Defensive vs aggressive style
- Reaction timing
- Low health behavior

### How to Make Boss Learn
```csharp
// Boss automatically learns when you call:
boss.OnPlayerCombatAction("heavy_attack");
boss.OnPlayerCombatAction("dodge_left");
boss.OnPlayerCombatAction("block");
```

## 💾 Data Structure Quick View

```javascript
NPCMemory {
  npcId: "merchant_01"
  personality: "friendly"
  relationshipScore: 25  // -100 to +100
  interactions: [...]    // Last 10 interactions
  learnedPatterns: {     // Detected patterns
    "dodge_preference": "left",
    "combat_style": "aggressive"
  }
}
```

## 🔧 Configuration Locations

| Setting | File | Default |
|---------|------|---------|
| API Model | OpenAIService.cs | gpt-3.5-turbo |
| Max Interactions Stored | NPCMemoryManager.cs | 10 |
| Boss Health | BossAIController.cs | 1000 |
| Interaction Range | NPCController.cs | 3f |

## 🎨 Available Personalities

- `friendly` - Cheerful, helpful
- `aggressive` - Hostile, suspicious
- `mysterious` - Cryptic, enigmatic
- `wise` - Thoughtful, philosophical
- `neutral` - Professional, distant

## 💰 Cost Breakdown

**Per 1000 player interactions:**
- Regular NPCs: $0.30 (using GPT-3.5)
- Boss encounters: $6.00 (using GPT-4)

**Optimization tips:**
- Use GPT-3.5 for most NPCs
- Cache common responses
- Reduce max_tokens for shorter responses

## 🐛 Quick Fixes

**NPC not responding?**
```
✓ Check GameInitializer is in scene
✓ Verify OpenAI API key has credits
✓ Ensure internet connection
✓ Check Console for errors
```

**Firebase not working?**
```
✓ google-services.json in Assets/
✓ Firebase packages imported
✓ Firestore enabled in console
```

**Boss not learning?**
```
✓ BossAIController attached
✓ "Is Boss" checked in NPCController
✓ OnPlayerCombatAction being called
```

## 📊 Relationship Levels

| Score | Level | Effects |
|-------|-------|---------|
| 50+ | Allied | Discounts, help in combat |
| 20-49 | Friendly | Share info, basic help |
| -19 to 19 | Neutral | Standard service |
| -20 to -49 | Hostile | Higher prices, cold |
| -50 or less | Enemy | Refuses service, may attack |

## 🎯 Team Task Quick List

### Shaan (UI)
- [ ] Design dialogue panels
- [ ] Create interaction prompts  
- [ ] Implement choice buttons
- [ ] Boss health bars

### Ayush (AI Logic)
- [ ] Configure OpenAI prompts
- [ ] Implement pattern detection
- [ ] Test boss learning
- [ ] Fine-tune responses

### Shivom (Backend)
- [ ] Set up Firebase project
- [ ] Design data schema
- [ ] Test memory persistence
- [ ] Monitor costs

## 🚨 Important Reminders

⚠️ Never commit API keys to git!
⚠️ Use test mode in Firebase during dev
⚠️ Monitor OpenAI costs daily
⚠️ Back up Firebase data regularly
⚠️ Test with small number of NPCs first

## 🔗 Quick Links

- Firebase Console: https://console.firebase.google.com/
- OpenAI Dashboard: https://platform.openai.com/
- Unity TextMeshPro Docs: https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0

---

**Need detailed instructions?** → See SETUP_GUIDE.md
**Want examples?** → See NPCPresets.cs
**Having issues?** → Check README.md troubleshooting section
