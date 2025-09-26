using System.ComponentModel;
using GGXXACPROverlay.GGXXACPR;

namespace GGXXACPROverlay.FrameMeter
{
    public enum FrameType
    {
        None,
        Neutral,
        Movement,
        [Description("Counter Hit")]
        CounterHitState,
        Startup,
        Active,
        [Description("Throw")]
        ActiveThrow,
        Recovery,
        [Description("Blockstun")]
        Blockstun,
        [Description("Hitstun")]
        Hitstun,
        [Description("Techable Hitstun")]
        TechableHitstun,
        [Description("Knocked Down")]
        KnockDownHitstun,
    }

    /// <summary>
    /// Drawn on bottom of frame meter pip
    /// </summary>
    public enum PrimaryFrameProperty
    {
        Default,
        [Description("Full Invuln")]
        InvulnFull,
        [Description("Strike Invuln")]
        InvulnStrike,
        [Description("Throw Invuln")]
        InvulnThrow,
        Parry,
        [Description("Full Guard Point")]
        GuardPointFull,
        [Description("High Guard Point")]
        GuardPointHigh,
        [Description("Low Guard Point")]
        GuardPointLow,
        Armor,
        FRC,
        Slashback,
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
        public FrameType Type { get; set; }
        public PrimaryFrameProperty PrimaryProperty1 { get; set; }
        public PrimaryFrameProperty PrimaryProperty2 { get; set; }
        public SecondaryFrameProperty SecondaryProperty { get; set; }
        public PlayerSnapShot PlayerState { get; set; }
        public int RunSum { get; set; }
    }
    public struct Meter(string name, int length)
    {
        public readonly string Label = name;
        public int Startup { get; set; } = -1;
        public int LastAttackActId { get; set; } = -1;
        public int Total { get; set; } = -1;
        public int Advantage { get; set; } = 0;
        public bool DisplayAdvantage { get; set; } = false;
        public bool Hide { get; set; } = false;
        public readonly FrameMeterPip[] FrameArr = new FrameMeterPip[length];
    }

    public readonly struct StateSnapShot(Player p1, Player p2, int frameNumber = 0)
    {
        public readonly int frameNumber = frameNumber;
        public readonly PlayerSnapShot p1 = new(p1);
        public readonly PlayerSnapShot p2 = new(p2);
    }


    public readonly struct PlayerSnapShot(Player p)
    {
        public readonly ActionState Status = p.Status;
        public readonly int ActionId = p.ActionId;
        public readonly int AnimationCounter = p.AnimationCounter;
        public readonly byte HitstopCounter = p.HitstopCounter;
    }

}
