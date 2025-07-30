using System.Runtime.InteropServices;

namespace GGXXACPROverlay.GGXXACPR
{
    public enum ThrowDetection : byte
    {
        None = 0,
        Player1ThrowSuccess         = 1 << 0,
        Player2ThrowSuccess         = 1 << 1,
        /// <summary>
        /// Preliminary throw check status. Determines if a full throw check should be made.
        /// </summary>
        Player2Throwable = 1 << 2,
        /// <summary>
        /// Preliminary throw check status. Determines if a full throw check should be made.
        /// </summary>
        Player1Throwable            = 1 << 3,
        Player1CommandThrowSuccess  = 1 << 4,
        Player2CommandThrowSuccess  = 1 << 5,
        Unknown7                    = 1 << 6,
        Unknown8                    = 1 << 7,
    }

    [StructLayout(LayoutKind.Explicit, Size = 0xA4)]
    public readonly ref struct Camera
    {
        [FieldOffset(0x00)] public readonly float PlayerXDiff;
        [FieldOffset(0x04)] public readonly float PlayerYDiff;
        [FieldOffset(0x10)] public readonly int CenterXPos;
        [FieldOffset(0x14)] public readonly int CameraHeight;  // Camera YPos, but anchor point is 1/12th of height from bottom edge
        [FieldOffset(0x20)] public readonly int LeftEdge;
        [FieldOffset(0x24)] public readonly int CameraHeightPlusOffset;   // Height offset
        [FieldOffset(0x28)] public readonly int Width;
        [FieldOffset(0x2C)] public readonly int Height;
        [FieldOffset(0x44)] public readonly float Zoom;
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x0C)]
    public readonly struct Hitbox
    {
        public readonly short XOffset;
        public readonly short YOffset;
        public readonly short Width;
        public readonly short Height;
        public readonly ushort BoxTypeId;
        public readonly short BoxFlags;  // Always 0 for hit and hurt boxes, used for some unknown box types
    }
    public enum BoxId : ushort
    {
        DUMMY     = 0,
        HIT       = 1,
        HURT      = 2,
        /// <summary>
        /// Indicates game should replace this hitbox with the one in Player.HitboxExtraSet at the same index.
        /// </summary>
        USE_EXTRA = 3,
        /// <summary>
        /// An adjustment to the player's pushbox.
        /// </summary>
        PUSH      = 4,
        UNKNOWN_5 = 5,
        UNKNOWN_6 = 6,  // Something to do with drawing particle effects
    }

    public enum CharacterID : ushort
    {
        NONE        = 0,
        SOL         = 1,
        KY          = 2,
        MAY         = 3,
        MILLIA      = 4,
        AXL         = 5,
        POTEMKIN    = 6,
        CHIPP       = 7,
        EDDIE       = 8,
        BAIKEN      = 9,
        FAUST       = 10,
        TESTAMENT   = 11,
        JAM         = 12,
        ANJI        = 13,
        JOHNNY      = 14,
        VENOM       = 15,
        DIZZY       = 16,
        SLAYER      = 17,
        INO         = 18,
        ZAPPA       = 19,
        BRIDGET     = 20,
        ROBOKY      = 21,
        ABA         = 22,
        ORDERSOL    = 23,
        KLIFF       = 24,
        JUSTICE     = 25
    }

    public enum GameVersion
    {
        AC     = 0,
        PLUS_R = 1,
    }
    /// <summary>
    /// Represents players, projectiles, and other entities
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 0x130)]
    public readonly unsafe ref struct BaseEntityRaw
    {
        [FieldOffset(0x00)] public readonly ushort Id;
        [FieldOffset(0x02)] public readonly byte IsFacingRight;
        [FieldOffset(0x03)] public readonly byte IsLeftSide;
        [FieldOffset(0x04)] public readonly BaseEntityRaw* Prev;   // points to previous item in entity linked list
        [FieldOffset(0x08)] public readonly BaseEntityRaw* Next;   // points to next item in the entity linked list
        [FieldOffset(0x0C)] public readonly uint Status;
        [FieldOffset(0x10)] public readonly short BufferedActionId;
        [FieldOffset(0x12)] public readonly ushort BufferFlags;
        [FieldOffset(0x18)] public readonly short ActionId;
        [FieldOffset(0x1C)] public readonly ushort AnimationCounter;
        [FieldOffset(0x1E)] public readonly ushort Health;
        [FieldOffset(0x20)] public readonly nint ParentPtrRaw;
        [FieldOffset(0x2A)] public readonly ushort GuardFlags;
        [FieldOffset(0x2C)] public readonly PlayerExtra* Extra; // Null pointer on non-player entities
        [FieldOffset(0x27)] public readonly byte PlayerIndex;
        [FieldOffset(0x28)] public readonly ushort ParentFlag;
        [FieldOffset(0x34)] public readonly uint AttackFlags;
        [FieldOffset(0x38)] public readonly uint CommandFlags;
        [FieldOffset(0x4C)] public readonly short CoreX;
        [FieldOffset(0x4E)] public readonly short CoreY;
        [FieldOffset(0x50)] public readonly short ScaleX;
        [FieldOffset(0x52)] public readonly short ScaleY;
        [FieldOffset(0x54)] public readonly Hitbox* HitboxSet;
        [FieldOffset(0x58)] public readonly Hitbox* HitboxExtraSet;
        [FieldOffset(0x5D)] public readonly byte HitboxFlag;
        [FieldOffset(0x5E)] public readonly byte HurtboxFlag;
        [FieldOffset(0x84)] public readonly byte BoxCount;
        [FieldOffset(0x85)] public readonly byte BoxIter;
        [FieldOffset(0x88)] public readonly HitParam* HitParam;
        [FieldOffset(0xB0)] public readonly int XPos;
        [FieldOffset(0xB4)] public readonly int YPos;
        [FieldOffset(0xB8)] public readonly int XVelocity;
        [FieldOffset(0xBC)] public readonly int YVelocity;
        [FieldOffset(0xD4)] public readonly int Gravity;
        [FieldOffset(0xFD)] public readonly byte HitstopCounter;
        [FieldOffset(0xFF)] public readonly byte Mark;
    }

    public unsafe readonly ref struct Player
    {
        public readonly BaseEntityRaw* NativePointer;

        public Player(BaseEntityRaw* entity)
        {
            NativePointer = entity;
        }

        public bool IsValid => NativePointer is not null;

        public CharacterID CharId => (CharacterID)NativePointer->Id;
        public byte IsFacingRight => NativePointer->IsFacingRight;
        public byte IsLeftSide => NativePointer->IsLeftSide;
        public ActionState Status => (ActionState)NativePointer->Status;
        public short BufferedActionId => NativePointer->BufferedActionId;
        public BufferState BufferFlags => (BufferState)NativePointer->BufferFlags;
        public short ActionId => NativePointer->ActionId;
        public ushort AnimationCounter => NativePointer->AnimationCounter;
        public ushort Health => NativePointer->Health;
        public byte PlayerIndex => NativePointer->PlayerIndex;
        public GuardState GuardFlags => (GuardState)NativePointer->GuardFlags;
        public PlayerExtra Extra {
            get
            {
                try
                {
                    return NativePointer->Extra is not null ? *NativePointer->Extra : default;
                }
                catch (AccessViolationException)
                {
                    return default;
                }
            }}
        public AttackState AttackFlags => (AttackState)NativePointer->AttackFlags;
        public CommandState CommandFlags => (CommandState)NativePointer->CommandFlags;
        public short CoreX => NativePointer->CoreX;
        public short CoreY => NativePointer->CoreY;
        public short ScaleX => NativePointer->ScaleX;
        public short ScaleY => NativePointer->ScaleY;
        public Span<Hitbox> HitboxSet => NativePointer->HitboxSet is not null ?
            new Span<Hitbox>(NativePointer->HitboxSet, NativePointer->BoxCount) : [];
        public Span<Hitbox> HitboxExtraSet => NativePointer->HitboxExtraSet is not null ?
            new Span<Hitbox>(NativePointer->HitboxSet, NativePointer->BoxCount) : [];
        public byte HitboxFlag => NativePointer->HitboxFlag;
        public byte HurtboxFlag => NativePointer->HurtboxFlag;
        public byte BoxCount => NativePointer->BoxCount;
        public byte BoxIter => NativePointer->BoxIter;
        public HitParam HitParam {
            get
            {
                try
                {
                    return NativePointer->HitParam is not null ? *NativePointer->HitParam : default;
                }
                catch (AccessViolationException)
                {
                    return default;
                }
            }}
        public int XPos => NativePointer->XPos;
        public int YPos => NativePointer->YPos;
        public int XVelocity => NativePointer->XVelocity;
        public int YVelocity => NativePointer->YVelocity;
        public int Gravity => NativePointer->Gravity;
        public byte HitstopCounter => NativePointer->HitstopCounter;
        /// <summary>
        /// Multi-use variable used for move-specific behavior (For Axl, holds parry active state)
        /// </summary>
        public byte Mark => NativePointer->Mark;
    }

    public unsafe readonly ref struct Entity
    {
        public readonly BaseEntityRaw* NativePointer;

        public Entity(BaseEntityRaw* entity)
        {
            NativePointer = entity;
        }

        public bool IsValid => NativePointer is not null;

        public ushort Id => NativePointer->Id;
        public byte IsFacingRight => NativePointer->IsFacingRight;
        public Entity Prev => new Entity(NativePointer->Prev);
        public Entity Next => new Entity(NativePointer->Prev);
        public ActionState Status => (ActionState)NativePointer->Status;
        public byte PlayerIndex => NativePointer->PlayerIndex;
        public ushort ParentFlag => NativePointer->ParentFlag;
        public AttackState AttackFlags => (AttackState)NativePointer->AttackFlags;
        public short CoreX => NativePointer->CoreX;
        public short CoreY => NativePointer->CoreY;
        public short ScaleX => NativePointer->ScaleX;
        public short ScaleY => NativePointer->ScaleY;
        public Span<Hitbox> HitboxSet => NativePointer->HitboxSet is not null ?
            new Span<Hitbox>(NativePointer->HitboxSet, NativePointer->BoxCount) : [];
        public Span<Hitbox> HitboxExtraSet => NativePointer->HitboxExtraSet is not null ?
            new Span<Hitbox>(NativePointer->HitboxSet, NativePointer->BoxCount) : [];
        public byte BoxCount => NativePointer->BoxCount;
        public int XPos => NativePointer->XPos;
        public int YPos => NativePointer->YPos;
        public byte HitstopCounter => NativePointer->HitstopCounter;

        public bool Equals(Entity e) => NativePointer == e.NativePointer;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x148)]
    public readonly ref struct PlayerExtra
    {
        [FieldOffset(0x000)] public readonly short Tension;
        [FieldOffset(0x002)] public readonly byte ExtraHitstun;
        [FieldOffset(0x010)] public readonly int RunInertia;
        [FieldOffset(0x018)] public readonly short ThrowProtectionTimer;
        [FieldOffset(0x01A)] public readonly short GuardBar;
        [FieldOffset(0x01E)] public readonly short UntechTimer;
        [FieldOffset(0x02A)] public readonly byte InvulnCounter;
        [FieldOffset(0x032)] public readonly byte RCTime;
        [FieldOffset(0x032)] public readonly byte RCLockoutTime;
        [FieldOffset(0x03D)] public readonly byte IBTimer;
        [FieldOffset(0x03E)] public readonly byte IBLockoutTimer;
        /// <summary>
        /// Parry is active when 0xFF, after 8F begins counting down from 14 as a lockout timer.
        /// </summary>
        [FieldOffset(0x090)] public readonly byte JamParryTime;
        /// <summary>
        /// Information about how this player should interact with the wall/ground while in hitstun.
        /// </summary>
        [FieldOffset(0x0AC)] public readonly uint HitstunFlags;
        /// <summary>
        /// Number of frames this player has been in hitstun
        /// </summary>
        [FieldOffset(0x0F6)] public readonly short ComboTime;
        [FieldOffset(0x10B)] public readonly byte SBTime;
        [FieldOffset(0x10C)] public readonly byte SBMissGuardLockoutTime;
        [FieldOffset(0x10E)] public readonly byte SBWakeupLockoutTime;
        /// <summary>
        /// Timer for successive lenient slash backs
        /// </summary>
        [FieldOffset(0x10F)] public readonly byte SBEasyFollowupTime;
        /// <summary>
        /// Clean hits received
        /// </summary>
        [FieldOffset(0x11C)] public readonly byte CleanHitCounter;
    }


    [StructLayout(LayoutKind.Explicit, Size = 0x5C)]
    public readonly ref struct HitParam
    {
        [FieldOffset(0x48)] public readonly short CLCenterX;
        [FieldOffset(0x4A)] public readonly short CLCenterY;
        /// <summary>
        /// The base halfwidth of the Clean Hit hitbox.
        /// </summary>
        [FieldOffset(0x4C)] public readonly short CLBaseWidth;
        /// <summary>
        /// The base halfheight of the Clean Hit hitbox.
        /// </summary>
        [FieldOffset(0x4E)] public readonly short CLBaseHeight;
        /// <summary>
        /// -1 if move does not have Clean Hit property. Otherwise holds the consecutive Clean Hit hitbox shrinking factor.
        /// </summary>
        [FieldOffset(0x50)] public readonly short CLScale;
        [FieldOffset(0x52)] public readonly byte ForceCL;
    }

    [Flags]
    public enum ActionState : uint
    {
        None = 0,
        IsEntity            = 1 << 0,
        /// <summary>
        /// Can be attacked by Player 2 (Mostly marks P1 and P1's entities, but Dizzy bubble is an exception)
        /// </summary>
        CollideWithP2 = 1 << 1,
        /// <summary>
        /// Can be attacked by Player 1 (Mostly marks P1 and P1's entities, but Dizzy bubble is an exception)
        /// </summary>
        CollideWithP1       = 1 << 2,
        DrawSprite          = 1 << 3,
        IsAirborne          = 1 << 4,
        IsInHitstun         = 1 << 5,
        DisableHitboxes     = 1 << 6,
        DisableHurtboxes    = 1 << 7,
        KnockedDown         = 1 << 8,
        IsInBlockstun       = 1 << 9,
        IsCrouching         = 1 << 10,
        IsCornered          = 1 << 11,
        /// <summary>
        /// Switches on when landing, but doesn't switch back off until standing or crouching neutral
        /// </summary>
        LandingFlag         = 1 << 12,
        /// <summary>
        /// But not cornered
        /// </summary>
        IsAtScreenLimit     = 1 << 13,
        ProjDisableHitboxes = 1 << 14,
        Wakeup              = 1 << 15,
        /// <summary>
        /// Set on KD when health is 0. Used for KO screen.
        /// </summary>
        StayKnockedDown     = 1 << 16,
        StrikeInvuln        = 1 << 17,
        IsIdle              = 1 << 18,
        /// <summary>
        /// Super flash
        /// </summary>
        Freeze              = 1 << 19,
        NoCollision         = 1 << 20,
        Gravity             = 1 << 21,
        /// <summary>
        /// Assocated with player->0xF4 having value of 0x100 (??)
        /// </summary>
        Unknown22           = 1 << 22,
        IsThrowInvuln       = 1 << 23,
        Unknown24           = 1 << 24,
        Unknown25           = 1 << 25,
        Unknown26           = 1 << 26,
        Unknown27           = 1 << 27,
        Unknown28           = 1 << 28,
        Unknown29           = 1 << 29,
        /// <summary>
        /// Used for Dizzy bubble hurtbox to ignore hitstop
        /// </summary>
        IgnoreReceivedHitEffects = 1 << 30,
        Despawn = 1u << 31
    }

    [Flags]
    public enum BufferState : ushort
    {
        None = 0,
        AirNeutral = 1 << 0,
        Normal = 1 << 3,
        Off = 0xFFFF,
    }

    [Flags]
    public enum AttackState : uint
    {
        None = 0,
        /// <summary>
        /// On when standard attack is animating
        /// </summary>
        IsAttack            = 1 << 0,
        Unknown1            = 1 << 1,
        Unknown2            = 1 << 2,
        Unknown3            = 1 << 3,
        /// <summary>
        /// Doesn't necessarily mean the move can currently gatling or has gatling options
        /// </summary>
        IsInGatlingWindow   = 1 << 4,
        SpecialCancelOkay   = 1 << 5,
        Unknown6            = 1 << 6,
        UnknownDustFlag1    = 1 << 7,
        HomingJumpOkay      = 1 << 8,
        KaraFDOkay          = 1 << 9,
        NoSpecialCancel     = 1 << 10,
        IsInRecovery        = 1 << 11,
        /// <summary>
        /// On during/after hit. Maybe an RC-Okay flag.
        /// </summary>
        HasConnected        = 1 << 12,
        Unknown13           = 1 << 13,
        Unknown14           = 1 << 14,
        Unknown15           = 1 << 15,
        IsJumpCancelable    = 1 << 16,
        /// <summary>
        /// Like HasConnected but only on when move is unblocked
        /// </summary>
        HasHitOpponent      = 1 << 17,
    }


    /// <summary>
    /// Relates to available state transitions. Doesn't handle gatlings, cancels, kara cancels.
    /// </summary>
    [Flags]
    public enum CommandState : uint
    {
        None = 0,

        // The following states aren't directly indicitive of the labeled states,
        //  but these states should have the following command flags enabled
        IsIdle              = 0x0101,
        IsMove              = 0xC05F,
        FreeCancel          = 0xC01F,
        RunDash             = 0xE00F,
        StepDash            = 0xE04F,
        RunDashSkid         = 0xC00F,

        // Base state transition flags
        NoNeutral           = 1 << 0,
        NoForward           = 1 << 1,
        NoBackward          = 1 << 2,
        NoCrouching         = 1 << 3,
        Unknown4            = 1 << 4,
        Unknown5            = 1 << 5,
        NoAttack            = 1 << 6,
        Unknown7            = 1 << 7,
        Unknown8            = 1 << 8,
        Unknown9            = 1 << 9,
        Airdash             = 1 << 10,
        Ukemi               = 1 << 11,
        Prejump             = 1 << 12,
        DisableThrow        = 1 << 13,
        FaustCrawlForward   = 1 << 14,
        FaustCrawlBackward  = 1 << 15,
    }

    [Flags]
    public enum GuardState : ushort
    {
        None = 0,
        IsStandBlocking     = 1 << 0,
        IsCrouchBlocking    = 1 << 1,
        IsAirBlocking       = 1 << 2,
        IsFD                = 1 << 3,
        Unknown4            = 1 << 4,
        Unknown5            = 1 << 5,
        IsInBlockStun       = 1 << 6,
        Unknown7            = 1 << 7,
        Unknown8            = 1 << 8,
        GuardPoint          = 1 << 9,
        Armor               = 1 << 10,
        Parry1              = 1 << 11,
        Parry2              = 1 << 12,
        Unknown13           = 1 << 13,
        Unknown14           = 1 << 14,
    }

    /// <summary>
    /// player->extra.HitstunFlags. player[0x2C][0xAC].
    /// </summary>
    [Flags]
    public enum HitstunState : uint
    {
        None = 0,
        Counterhit  = 1 << 2,
        Cleanhit    = 1 << 17,
    }

    [Flags]
    public enum BackgroundState
    {
        None = 0,
        Unknown1        = 1 << 0,
        HudOff          = 1 << 1,
        Default         = 1 << 2,
        BlackBackground = 1 << 3,
        PostFlashDim    = 1 << 4,
        Unknown6        = 1 << 5,
        SomeCoolEffect  = 1 << 6,
        Unknown8        = 1 << 7,
        Unknown9        = 1 << 8,
        Unknown10       = 1 << 9,
        Unknown11       = 1 << 10,
        SuperFlash      = 1 << 11,
        Unknown13       = 1 << 12,
        Unknown14       = 1 << 13,
        FBSuperFlash    = 1 << 14,
        Lightning       = 1 << 15,
    }
}
