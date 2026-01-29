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
