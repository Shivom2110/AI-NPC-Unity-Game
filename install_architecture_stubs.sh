#!/bin/bash
set -e

echo "Adding architecture stub files..."

# -------------------------------------------------
# Dialogue system stubs
# -------------------------------------------------
mkdir -p Assets/Scripts/Systems/Dialogue

cat > Assets/Scripts/Systems/Dialogue/DialogueContext.cs <<'EOT'
using System;

namespace AINPC.Systems.Dialogue
{
    [Serializable]
    public class DialogueContext
    {
        public string npcId;
        public int relationshipScore;
        public string lastPlayerAction;
        public string sceneId;
    }
}
EOT

cat > Assets/Scripts/Systems/Dialogue/DialogueResult.cs <<'EOT'
using System;

namespace AINPC.Systems.Dialogue
{
    [Serializable]
    public class DialogueResult
    {
        public string npcId;
        public string text;
        public string tone;
        public int relationshipDelta;
    }
}
EOT

cat > Assets/Scripts/Systems/Dialogue/DialogueRuleEngine.cs <<'EOT'
namespace AINPC.Systems.Dialogue
{
    public static class DialogueRuleEngine
    {
        public static DialogueResult Generate(DialogueContext context)
        {
            return new DialogueResult
            {
                npcId = context.npcId,
                text = "...",
                tone = "neutral",
                relationshipDelta = 0
            };
        }
    }
}
EOT

# -------------------------------------------------
# Local NPC memory stubs
# -------------------------------------------------
mkdir -p Assets/Scripts/NPC

cat > Assets/Scripts/NPC/LocalNPCMemory.cs <<'EOT'
using System;
using System.Collections.Generic;

[Serializable]
public class LocalNPCMemory
{
    public string npcId;
    public int relationshipScore;

    public int timesHelped;
    public int timesInsulted;
    public int timesTraded;
    public int timesAttacked;

    public List<string> recentNotes = new List<string>();
}
EOT

cat > Assets/Scripts/NPC/LocalNPCMemoryManager.cs <<'EOT'
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
EOT

# -------------------------------------------------
# Action vocabulary
# -------------------------------------------------
mkdir -p Assets/Scripts/Systems/Learning

cat > Assets/Scripts/Systems/Learning/ActionVocabulary.cs <<'EOT'
namespace AINPC.Systems.Learning
{
    public static class ActionVocabulary
    {
        // Combat
        public const string LightAttack = "light_attack";
        public const string HeavyAttack = "heavy_attack";
        public const string DodgeLeft  = "dodge_left";
        public const string DodgeRight = "dodge_right";
        public const string DodgeBack  = "dodge_back";
        public const string Block      = "block";

        // Dialogue
        public const string TalkNice   = "talk_nice";
        public const string TalkRude   = "talk_rude";
        public const string TalkTrade  = "talk_trade";
    }
}
EOT

# -------------------------------------------------
# Architecture documentation
# -------------------------------------------------
mkdir -p Assets/Documentation

cat > Assets/Documentation/ARCHITECTURE.md <<'EOT'
# Architecture Overview (Local Learning)

## Core Principles
- No cloud services
- No ML training
- Deterministic, local statistical learning

## Event Flow
Player Action
→ EventBus
→ MarkovPredictor
→ Boss / NPC Decision
→ Combat / Dialogue Output

## Systems
- Systems/Events: PlayerEvent, EventBus
- Systems/Learning: MarkovPredictor
- Systems/Persistence: LocalSaveService
- Systems/Dialogue: Rule + template dialogue (stubbed)
- NPC: Local memory + relationship tracking

## Persistence
- Saved locally via JSON
- Path: Application.persistentDataPath/saves/

## Status
- Architecture locked
- Gameplay, UI, and visuals intentionally deferred
EOT

echo "Architecture stubs added successfully."
echo "Open Unity once to generate .meta files."
