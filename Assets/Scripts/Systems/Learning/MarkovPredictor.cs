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
