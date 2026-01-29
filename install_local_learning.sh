#!/bin/bash
set -e

# =========================================================
# AI-NPC-Unity-Game – Local Learning Core Installer
# Run this from the Unity project root (folder with Assets/)
# =========================================================

if [ ! -d "Assets" ]; then
  echo "ERROR: Assets/ folder not found. Run this from project root."
  exit 1
fi

echo "Creating folders..."

mkdir -p Assets/Scripts/Systems/Events
mkdir -p Assets/Scripts/Systems/Learning
mkdir -p Assets/Scripts/Systems/Persistence
mkdir -p Assets/Scripts/Systems/Dialogue
mkdir -p Assets/Scripts/UI
mkdir -p Assets/Resources/Configs

echo "Writing scripts..."

# ---------------- Events ----------------

cat > Assets/Scripts/Systems/Events/PlayerEventType.cs <<'EOT'
namespace AINPC.Systems.Events
{
    public enum PlayerEventType
    {
        None = 0,

        // Combat
        AttackHigh,
        AttackMid,
        AttackLow,
        DodgeLeft,
        DodgeRight,
        DodgeBack,
        Block,
        Parry,
        Heal,
        Wait,

        // Dialogue / Interaction
        TalkNice,
        TalkRude,
        TalkTrade,
        TalkThreaten,
        HelpNPC,
        StealNPC,
        AttackNPC
    }
}
EOT

cat > Assets/Scripts/Systems/Events/PlayerEvent.cs <<'EOT'
using System;

namespace AINPC.Systems.Events
{
    [Serializable]
    public class PlayerEvent
    {
        public PlayerEventType type;
        public long unixMs;
        public string npcId;
        public string bossId;
        public string meta;

        public PlayerEvent(PlayerEventType type, string npcId = "", string bossId = "", string meta = "")
        {
            this.type = type;
            this.unixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this.npcId = npcId ?? "";
            this.bossId = bossId ?? "";
            this.meta = meta ?? "";
        }
    }
}
EOT

cat > Assets/Scripts/Systems/Events/EventBus.cs <<'EOT'
using System;

namespace AINPC.Systems.Events
{
    public static class EventBus
    {
        public static event Action<PlayerEvent> OnPlayerEvent;

        public static void Publish(PlayerEvent e)
        {
            OnPlayerEvent?.Invoke(e);
        }
    }
}
EOT

# ---------------- Learning ----------------

cat > Assets/Scripts/Systems/Learning/MarkovPredictor.cs <<'EOT'
using System;
using System.Collections.Generic;
using System.Linq;
using AINPC.Systems.Events;

namespace AINPC.Systems.Learning
{
    [Serializable]
    public class MarkovPredictor
    {
        private readonly Dictionary<PlayerEventType, Dictionary<PlayerEventType, int>> transitions
            = new Dictionary<PlayerEventType, Dictionary<PlayerEventType, int>>();

        public void Record(PlayerEventType prev, PlayerEventType next)
        {
            if (prev == PlayerEventType.None || next == PlayerEventType.None) return;

            if (!transitions.ContainsKey(prev))
                transitions[prev] = new Dictionary<PlayerEventType, int>();

            if (!transitions[prev].ContainsKey(next))
                transitions[prev][next] = 0;

            transitions[prev][next]++;
        }

        public PlayerEventType Predict(PlayerEventType last)
        {
            if (!transitions.ContainsKey(last)) return PlayerEventType.None;

            return transitions[last]
                .OrderByDescending(kv => kv.Value)
                .First().Key;
        }
    }
}
EOT

# ---------------- Persistence ----------------

cat > Assets/Scripts/Systems/Persistence/LocalSaveService.cs <<'EOT'
using System.IO;
using UnityEngine;

namespace AINPC.Systems.Persistence
{
    public static class LocalSaveService
    {
        private static string Root =>
            Path.Combine(Application.persistentDataPath, "saves");

        public static void Save(string key, string json)
        {
            if (!Directory.Exists(Root))
                Directory.CreateDirectory(Root);

            File.WriteAllText(Path.Combine(Root, key + ".json"), json);
        }

        public static string Load(string key)
        {
            string path = Path.Combine(Root, key + ".json");
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
    }
}
EOT

cat > Assets/Scripts/Systems/Persistence/SaveKeys.cs <<'EOT'
namespace AINPC.Systems.Persistence
{
    public static class SaveKeys
    {
        public const string PlayerProfile = "player_profile";
        public const string MarkovData = "markov_data";
    }
}
EOT

echo "DONE."
echo "Open Unity and let it recompile."
