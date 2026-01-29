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
