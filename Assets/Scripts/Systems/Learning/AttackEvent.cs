using System;

[Serializable]
public struct AttackEvent
{
    public PlayerAttackType AttackType;
    public float Time;

    public AttackEvent(PlayerAttackType attackType, float time)
    {
        AttackType = attackType;
        Time = time;
    }

    public override string ToString()
    {
        return $"{AttackType}@{Time:F2}";
    }
}
