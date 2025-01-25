namespace GGXXACPROverlay.GGXXACPR
{
    public struct GlobalFlags()
    {
        public ThrowFlags ThrowFlags = 0;
    }

    public readonly struct ThrowFlags(byte flags)
    {
        private readonly byte _flags = flags;

        public readonly bool Player2ThrowActive { get { return (_flags & 0x1) > 0; } } // Needs confirmation
        public readonly bool Player1ThrowActive { get { return (_flags & 0x2) > 0; } } // Needs confirmation
        public readonly bool Player2Throwable { get { return (_flags & 0x4) > 0;} } // Used if a throw check should be made when 4H/6H input.
        public readonly bool Player1Throwable { get { return (_flags & 0x8) > 0; } } // These Flags are off if both players don't share a grounded/airborne state

        public static implicit operator ThrowFlags(int flags) { return new ThrowFlags((byte)flags); }
    }

    public struct Camera()
    {
        /*0x10*/ public int CenterXPos = 0;
        /*0x14*/ public int BottomEdge = 0;
        /*0x20*/ public int LeftEdge = -24000;
        /*0x28*/ public int Width = 48000;
        /*0x2C*/ public int Height = 36000;
        /*0x44*/ public float Zoom = 1f;
    }

    public struct Hitbox()
    {
        public short XOffset = 0;
        public short YOffset = 0;
        public short Width = 0;
        public short Height = 0;
        public BoxId BoxTypeId = BoxId.DUMMY;
        public short Filler = 1; //Should always be 0, used for error checking
    }
    public enum BoxId
    {
        DUMMY = 0,
        HIT = 1,
        HURT = 2,
        UNKNOWN3 = 3,
        UNKNOWN5 = 5,
        UNKNOWN6 = 6,
    }

    public struct Player()
    {
        /*0x00*/ public ushort CharId = 0;
        /*0x02*/ public bool IsFacingRight = false;
        /*0x0C*/ public ActionStateFlags Status = 0;
        /*0x12*/ public BufferFlags BufferFlags = 0;
        /*0x1C*/ public ushort AnimationCounter = 0;
        /*0x2A*/ public GuardStateFlags GuardFlags = 0;
        /*0x2C*/ public PlayerExtra Extra = new();
        /*0x34*/ public AttackStateFlags AttackFlags = 0;
        /*0x38*/ public CommandFlags CommandFlags = 0;
        /*0x4C*/ public short CoreX = 0;
        /*0x4E*/ public short CoreY = 0;
        /*0x50*/ public short ScaleX = -1;
        /*0x52*/ public short ScaleY = -1;
        /*0x54*/ public Hitbox[] HitboxSet = [];
        /*0x84*/ public byte BoxCount = 0;
        /*0x85*/ public byte BoxIter = 255;
        /*0xB0*/ public int XPos = 0;
        /*0xB4*/ public int YPos = 0;
        /*0xFD*/ public byte HitstopCounter = 0;
        /*none*/ public Hitbox PushBox = new();
    }
    public struct PlayerExtra()
    {
        /*0x0018*/ public short ThrowProtectionTimer = 0;
        /*0x002A*/ public byte InvulnCounter = 0;
        /*0x0032*/ public byte RCTime = 0;
        /*0x00F6*/ public short ComboTime = 0;
        /*0x010B*/ public byte SBTime = 0;
    }

    public readonly struct ActionStateFlags(uint flags)
    {
        private readonly uint _flags = flags;

        public readonly bool IsEntity { get { return (_flags & 0x0001) > 0; } }   // Always on?
        public readonly bool IsPlayer1 { get { return (_flags & 0x0002) > 0; } }   // Is or is owned by Player 1
        public readonly bool IsPlayer2 { get { return (_flags & 0x0004) > 0; } }   // Is or is owned by Player 2
        public readonly bool Unknownx08 { get { return (_flags & 0x0008) > 0; } }   // Always on?
        public readonly bool IsAirborne { get { return (_flags & 0x0010) > 0; } }
        public readonly bool IsInHitstun { get { return (_flags & 0x0020) > 0; } }
        public readonly bool DisableHitboxes { get { return (_flags & 0x0040) > 0; } }
        public readonly bool DisableHurtboxes { get { return (_flags & 0x0080) > 0; } }    // Disable hurtboxes?
        public readonly bool IsInBlockstun { get { return (_flags & 0x0200) > 0; } }
        public readonly bool IsCrouching { get { return (_flags & 0x0400) > 0; } }
        //public readonly bool Unknown0x0800 { get { return (_flags & 0x0800) > 0; } } // Unknown
        public readonly bool IsCornered { get { return (_flags & 0x0800) > 0; } }
        // Switches on when landing, but doesn't switch back off until standing or crouching neutral
        public readonly bool LandingFlag { get { return (_flags & 0x1000) > 0; } }
        // But not cornered
        public readonly bool IsAtScreenLimit { get { return (_flags & 0x2000) > 0; } }
        public readonly bool Unknown0x4000 { get { return (_flags & 0x4000) > 0; } } // Round end thing? In a throw animation? In some locked animation?
        public readonly bool IsPushboxType1 { get { return (_flags & 0x8000) > 0; } }
        // Some kind of Invuln flag?
        public readonly bool StrikeInvuln { get { return (_flags & 0x00020000) > 0; } }
        public readonly bool IsIdle { get { return (_flags & 0x00040000) > 0; } }
        public readonly bool Freeze { get { return (_flags & 0x00080000) > 0; } } // Super flash?
        // Duplicate? HasJumped?
        public readonly bool Unknownx00100000 { get { return (_flags & 0x00100000) > 0; } } // Slayer back dash?
        // AirOptions flag?
        public readonly bool JumpRestrict { get { return (_flags & 0x00200000) > 0; } }
        public readonly bool Unknown0x00400000 { get { return (_flags & 0x00400000) > 0; } } // Assocated with player->0xF4 having values 0x100
        public readonly bool IsThrowInuvln { get { return (_flags & 0x00800000) > 0; } }

        public static implicit operator ActionStateFlags(uint flags) { return new ActionStateFlags(flags); }
    }

    public readonly struct BufferFlags(ushort flags)
    {
        private readonly ushort _flags = flags;

        private readonly bool Off { get { return _flags == 0xFFFF; } }
        public readonly bool AirNeutral { get { return (_flags & 0x0001) > 0 && !Off; } }
        public readonly bool Unknown2 { get { return (_flags & 0x0002) > 0 && !Off; } }
        public readonly bool Unknown3 { get { return (_flags & 0x0004) > 0 && !Off; } }
        public readonly bool Normal { get { return (_flags & 0x0008) > 0 && !Off; } }   // base buffer flag / on when normal
        public readonly bool Unknown5 { get { return (_flags & 0x0010) > 0 && !Off; } }
        public readonly bool Unknown6 { get { return (_flags & 0x0020) > 0 && !Off; } }
        public readonly bool Unknown7 { get { return (_flags & 0x0040) > 0 && !Off; } }
        public readonly bool Unknown8 { get { return (_flags & 0x0080) > 0 && !Off; } }
        public readonly bool Unknown9 { get { return (_flags & 0x0100) > 0 && !Off; } }
        public readonly bool Unknown10 { get { return (_flags & 0x0200) > 0 && !Off; } }
        public readonly bool Unknown11 { get { return (_flags & 0x0400) > 0 && !Off; } }
        public readonly bool Unknown12 { get { return (_flags & 0x0800) > 0 && !Off; } }
        public readonly bool Unknown13 { get { return (_flags & 0x1000) > 0 && !Off; } }
        public readonly bool Unknown14 { get { return (_flags & 0x2000) > 0 && !Off; } }
        public readonly bool Unknown15 { get { return (_flags & 0x4000) > 0; } }
        public readonly bool Unknown16 { get { return (_flags & 0x8000) > 0; } }

        public static implicit operator BufferFlags(ushort flags) { return new BufferFlags(flags); }
    }

    public readonly struct AttackStateFlags(uint flags)
    {
        private readonly uint _flags = flags;

        // On when standard attack is animating
        public readonly bool IsAttack { get { return (_flags & 0x0001) > 0; } }
        // Doesn't necessarily mean the move can currently gatling or has gatling options
        public readonly bool IsInGatlingWindow { get { return (_flags & 0x0010) > 0; } }
        public readonly bool SpecialCancelOkay { get { return (_flags & 0x0020) > 0; } }
        public readonly bool UnknownDustFlag1 { get { return (_flags & 0x0080) > 0; } }
        // For Dust
        public readonly bool HomingJumpOkay { get { return (_flags & 0x0100) > 0; } }
        // Kara cancel (?) on for first 2 frames only
        public readonly bool KaraFDOkay { get { return (_flags & 0x0200) > 0; } }
        // on while 5P/2P/2K is active, on while recovery for almost all
        public readonly bool Unknown1 { get { return (_flags & 0x0400) > 0; } }
        public readonly bool IsInRecovery { get { return (_flags & 0x0800) > 0; } }
        // On during/after hit? RC okay?
        public readonly bool HasConnected { get { return (_flags & 0x1000) > 0; } }
        public readonly bool IsJumpCancelable { get { return (_flags & 0x00040000) > 0; } }
        // Like HasConnected but only on when move is unblocked
        public readonly bool HasHitOpponent { get { return (_flags & 0x00080000) > 0; } }

        public static implicit operator AttackStateFlags(uint flags) { return new AttackStateFlags(flags); }
    }

    // Mysterious
    public readonly struct CommandFlags(uint flags)
    {
        public readonly uint _flags = flags;

        public readonly bool IsIdle { get { return _flags == 0x1010; } }
        public readonly bool IsMove { get { return (_flags & 0x0000FFFF) == 0xC05F; } }   // Has this value when any commital action is performed (?)


        public readonly bool GroundNeutral { get { return (_flags & 0x0001) > 0; } }
        public readonly bool Forward { get { return (_flags & 0x0002) > 0; } }
        public readonly bool Backward { get { return (_flags & 0x0004) > 0; } }
        public readonly bool Crouching { get { return (_flags & 0x0008) > 0; } }
        public readonly bool NoFreeAttackCancel { get { return (_flags & 0x0040) > 0; } } // On during most animations, off during cancel period of taunt and in neutral
        public readonly bool Unknown0x0100 { get { return (_flags & 0x0100) > 0; } } // can't block if 0x0300
        public readonly bool Unknown0x0200 { get { return (_flags & 0x0200) > 0; } }
        public readonly bool Unknown0x2000 { get { return (_flags & 0x2000) > 0; } } // Jump cancelable state?
        public readonly bool AirNeutral { get { return (_flags & 0x4000) > 0; } }
        public readonly bool AirtechOkay { get { return (_flags & 0x8000) > 0; } }   // Associated with Airtech

        public static implicit operator CommandFlags(uint flags) { return new CommandFlags(flags); }
    }

    public readonly struct GuardStateFlags(ushort flags)
    {
        private readonly ushort _flags = flags;

        public readonly bool IsStandBlocking { get { return (_flags & 0x01) > 0; } }
        public readonly bool IsCrouchBlocking { get { return (_flags & 0x02) > 0; } }
        public readonly bool IsAirBlocking { get { return (_flags & 0x04) > 0; } }
        public readonly bool IsFD { get { return (_flags & 0x08) > 0; } }
        public readonly bool IsInBlockStun { get { return (_flags & 0x40) == 0; } } // True when SlashBack?
        public readonly bool GuardPoint { get { return (_flags & 0x0200) > 0; } }
        public readonly bool Armor { get { return (_flags & 0x0400) > 0; } }
        public readonly bool Unknownx0800 { get { return (_flags & 0x0800) > 0; } } // Justice parry, Axl parry
        public readonly bool Parry { get { return (_flags & 0x1000) > 0; } } // Testament Warrant, Jam Parry?
        public readonly bool Unknownx4000 { get { return (_flags & 0x4000) > 0; } } // Block stun but stops earlier than Status.IsInBlockstun ??


        public static implicit operator GuardStateFlags(ushort flags) { return new GuardStateFlags(flags); }
    }

    // Similar to the player struct. It seems both the players and projectile entities are stored in the same array.
    public struct Entity()
    {
        /*0x00*/ public ushort Id = 0;
        /*0x02*/ public bool IsFacingRight = false;
        /*0x04*/ public nint BackPtr = nint.Zero;   // points to previous item in entity array
        /*0x08*/ public nint NextPtr = nint.Zero;   // points to next item in the entity array
        /*0x0C*/ public ActionStateFlags Status = new(0);
        /*0x20*/ public nint ParentPtrRaw = nint.Zero;
        /*0x28*/ public ushort ParentFlag = 0;
        /*0x4C*/ public short CoreX = 0;
        /*0x4E*/ public short CoreY = 0;
        /*0x50*/ public short ScaleX = -1;
        /*0x52*/ public short ScaleY = -1;
        /*0x54*/ public nint HitboxSetPtr = nint.Zero;
        /*0x54*/ public Hitbox[] HitboxSet = [];
        /*0x84*/ public byte BoxCount = 0;
        /*0xB0*/ public int XPos = 0;
        /*0xB4*/ public int YPos = 0;
        /*none*/ public int ParentIndex = -1;    // Derived from ParentPtrRaw
    }

    public readonly struct GameState(Player player1, Player player2, Camera camera, Entity[] entities, GlobalFlags globalFlags)
    {
        public readonly Player Player1 = player1;
        public readonly Player Player2 = player2;
        public readonly Camera Camera = camera;
        public readonly Entity[] Entities = entities;
        public readonly GlobalFlags GlobalFlags = globalFlags;
    }
}
