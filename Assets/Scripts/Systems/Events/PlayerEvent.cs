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
