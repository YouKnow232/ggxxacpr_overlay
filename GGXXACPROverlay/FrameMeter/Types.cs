using GGXXACPROverlay.GGXXACPR;

namespace GGXXACPROverlay.FrameMeter
{
    public enum FrameType
    {
        None,
        Neutral,
        Movement,
        CounterHitState,
        Startup,
        Active,
        ActiveThrow,
        Recovery,
        BlockStun,
        HitStun
    }

    /// <summary>
    /// Drawn on bottom of frame meter pip
    /// </summary>
    public enum PrimaryFrameProperty
    {
        Default,
        InvulnFull,
        InvulnStrike,
        InvulnThrow,
        Parry,
        GuardPointFull,
        GuardPointHigh,
        GuardPointLow,
        Armor,
        FRC,
        SlashBack,
        TEST
    }

    /// <summary>
    /// Drawn on top of frame meter pip
    /// </summary>
    public enum SecondaryFrameProperty
    {
        Default,
        FRC
    }

    public struct FrameMeterPip
    {
        public FrameType Type;
        public PrimaryFrameProperty PrimaryProperty1;
        public PrimaryFrameProperty PrimaryProperty2;
        public SecondaryFrameProperty SecondaryProperty;
        public PlayerSnapShot playerState;
    }
    public struct Meter(string name, int length)
    {
        public readonly string Label = name;
        public int Startup = -1;
        public int LastAttackActId = -1;
        public int Total = -1;
        public int Advantage = 0;
        public bool DisplayAdvantage = false;
        public bool Hide = false;
        public FrameMeterPip[] FrameArr = new FrameMeterPip[length];
    }

    public readonly struct PlayerSnapShot(Player p)
    {
        public readonly ActionState Status = p.Status;
        public readonly int ActionId = p.ActionId;
        public readonly int AnimationCounter = p.AnimationCounter;
        public readonly byte HitstopCounter = p.HitstopCounter;
    }

}
