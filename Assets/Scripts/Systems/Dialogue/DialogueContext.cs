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
