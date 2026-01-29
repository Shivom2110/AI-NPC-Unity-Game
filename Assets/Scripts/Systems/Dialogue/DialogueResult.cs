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
