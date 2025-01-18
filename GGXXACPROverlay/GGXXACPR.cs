using System.Data;
using System.Dynamic;
using System.Net.Sockets;
using System.Numerics;

namespace GGXXACPROverlay
{
    internal struct Camera
    {
        public int CenterXPos;  // 0x10
        public int BottomEdge;  // 0x14
        public int LeftEdge;    // 0x20
        public int Width;       // 0x28
        public int Height;      // 0x2C
        public float Zoom;      // 0x44
    }

    internal struct Hitbox
    {
        public short XOffset;
        public short YOffset;
        public short Width;
        public short Height;
        public short BoxTypeId;
        public short Filler;    //Should always be 0, used for error checking
    }

    internal struct Player
    {
        public ushort CharId;           // 0x00
        public bool IsFacingRight;      // 0x02
        public StateFlags0x0C Status;   // 0x0C
        public ushort AnimationCounter; // 0x1C
        public PlayerExtra extra;       // 0x2C
        public Hitbox[] HitboxSet;      // 0x54
        public byte BoxCount;           // 0x84
        public int XPos;                // 0xB0
        public int YPos;                // 0xB4
        public byte HitstopCounter;     // 0xFD
        public Hitbox PushBox;          // Derived from state flags at 0x0C and static pointers
    }

    internal readonly struct StateFlags0x0C(uint flags)
    {
        private readonly uint _flags = flags;

        public readonly bool IsAirborne     { get { return (_flags & 0x0010) > 0; } }
        public readonly bool IsInHitstun    { get { return (_flags & 0x0020) > 0; } }
        public readonly bool IsInRecovery   { get { return (_flags & 0x0040) > 0; } }
        public readonly bool IsStrikeInvuln { get { return (_flags & 0x0080) > 0; } }
        public readonly bool IsInBlockstun  { get { return (_flags & 0x0200) > 0; } }
        public readonly bool IsCrouching    { get { return (_flags & 0x0400) > 0; } }
        //public readonly bool Unknown0x0800 { get { return (_flags & 0x0800) > 0; } } // Unknown
        public readonly bool IsPushboxType1 { get { return (_flags & 0x8000) > 0; } }
        //public readonly bool Unknown0x20000 { get { return (_flags & 0x00020000) > 0; } } // Some kind of Invuln flag?
        public readonly bool IsInNeutral    { get { return (_flags & 0x00040000) > 0; } }
        public readonly bool IsThrowInuvln  { get { return (_flags & 0x00800000) > 0; } }

        public static implicit operator StateFlags0x0C(uint flags) { return new StateFlags0x0C(flags); }
    }

    internal struct PlayerExtra
    {
        public byte InvulnCounter; //0x2A
    }

    // Similar to the player struct. It seems both the players and projectile entities are stored in the same array.
    internal struct Projectile
    {
        public ushort ProjId;       // 0x00
        public bool IsActive;       // 0x0C (7th bit, bitmask = 0x40)
        public bool IsFacingRight;  // 0x02
        //public uint retroPtr;       // 0x04 points to previous item in entity array
        //public uint nextPtr;        // 0x08, points to next item in the entity array
        public Hitbox[] HitboxSet;  // 0x54
        public byte BoxCount;       // 0x84
        public int XPos;            // 0xB0
        public int YPos;            // 0xB4
    }

    //internal struct GameState
    //{
    //    public Player p1;
    //    public Player p2;
    //    public Camera cam;
    //    public Projectile[] projectiles;
    //}

    public enum BoxId { DUMMY = 0, HIT = 1, HURT = 2}
    public enum PlayerId { P1 = 0, P2 = 1 }

    internal static class GGXXACPR
    {
        public static readonly int SCREEN_HEIGHT_PIXELS = 480;
        public static readonly int SCREEN_GROUND_PIXEL_OFFSET = 48;

        internal static readonly nint CAMERA_ADDR = 0x006D5CD4;
        internal static readonly nint PLAYER_1_PTR_ADDR = 0x006D1378;
        internal static readonly nint PLAYER_2_PTR_ADDR = 0x006D4C84;
        internal static readonly nint[] PLAYER_PTR_ADDRS = [PLAYER_1_PTR_ADDR, PLAYER_2_PTR_ADDR];

        internal static readonly nint[] PUSHBOX_ADDRESSES = [ 0x00573154, 0x00573B38,
                                                              0x00573B6C, 0x00571E6C,
                                                              0x00571564, 0x00571E6C];

        // Player Struct Offsets
        internal static readonly byte IS_FACING_RIGHT_OFFSET = 0x02;
        internal static readonly byte STATUS_OFFSET = 0x0C;
        internal static readonly byte ANIMATION_FRAME_OFFSET = 0x1C;
        internal static readonly byte PLAYER_EXTRA_PTR_OFFSET = 0X2C;
        internal static readonly byte HITBOX_LIST_OFFSET = 0x54;
        internal static readonly byte HITBOX_LIST_LENGTH_OFFSET = 0x84;
        internal static readonly byte XPOS_OFFSET = 0xB0;
        internal static readonly byte YPOS_OFFSET = 0xB4;
        internal static readonly byte HITSTOP_COUNTER_OFFSET = 0xFD;


        internal static readonly nint PROJECTILE_ARR_HEAD_TAIL_PTR = 0x006D27A8;
        internal static readonly nint PROJECTILE_ARR_HEAD_PTR = 0x006D27A8 + 0x04;
        internal static readonly nint PROJECTILE_ARR_TAIL_PTR = 0x006D27A8 + 0x08;

        internal static readonly nint PROJECTILE_LIST_PTR = 0x006D137C;
        internal static readonly short PROJECTILE_ARRAY_STEP = 0x130;
        internal static readonly byte PROJECTILE_ARRAY_SIZE = 32;

        internal static readonly byte PLAYER_EXTRA_INVULN_COUNTER_OFFSET = 0x2A;

        internal static readonly byte CAMERA_X_CENTER_OFFSET = 0x10;
        internal static readonly byte CAMERA_BOTTOM_EDGE_OFFSET = 0x14;
        internal static readonly byte CAMERA_LEFT_EDGE_OFFSET = 0x20;
        internal static readonly byte CAMERA_WIDTH_OFFSET = 0x28;
        internal static readonly byte CAMERA_HEIGHT_OFFSET = 0x2C;
        internal static readonly byte CAMERA_ZOOM_OFFSET = 0x44;

        internal static readonly byte CAMERA_STRUCT_BUFFER = 0x48;
        internal static readonly short ENTITY_STRUCT_BUFFER = 0x130;
        internal static readonly byte HITBOX_ARRAY_STEP = 0x0C;

        internal static Camera GetCameraStruct()
        {
            byte[] data = Memory.ReadMemoryPlusBaseOffset(CAMERA_ADDR, CAMERA_STRUCT_BUFFER);
            if (data.Length == 0) { return DummyCameraStruct(); }

            return new()
            {
                CenterXPos  = BitConverter.ToInt32(data, CAMERA_X_CENTER_OFFSET),
                BottomEdge  = BitConverter.ToInt32(data, CAMERA_BOTTOM_EDGE_OFFSET),
                LeftEdge    = BitConverter.ToInt32(data, CAMERA_LEFT_EDGE_OFFSET),
                Width       = BitConverter.ToInt32(data, CAMERA_WIDTH_OFFSET),
                Height      = BitConverter.ToInt32(data, CAMERA_HEIGHT_OFFSET),
                Zoom        = BitConverter.ToSingle(data, CAMERA_ZOOM_OFFSET)
            };
        }

        private static Camera DummyCameraStruct()
        {
            return new()
            {
                CenterXPos = 0,
                BottomEdge = 0,
                LeftEdge = 0,
                Width = 0,
                Height = 0,
                Zoom = 1
            };
        }

        internal static Player GetPlayerStruct(PlayerId player)
        {
            byte[] tempData = Memory.ReadMemoryPlusBaseOffset(PLAYER_PTR_ADDRS[(int)player], sizeof(int));
            if (tempData.Length == 0) { return DummyPlayerStruct(); }
            nint playerPtr = (nint)BitConverter.ToUInt32(tempData);

            byte[] data = Memory.ReadMemory(playerPtr, ENTITY_STRUCT_BUFFER);
            if (data.Length == 0) { return DummyPlayerStruct(); }

            Player p = new()
            {
                CharId = BitConverter.ToUInt16(data),
                IsFacingRight = data[IS_FACING_RIGHT_OFFSET] == 1,
                AnimationCounter = BitConverter.ToUInt16(data, ANIMATION_FRAME_OFFSET),
                Status = BitConverter.ToUInt32(data, STATUS_OFFSET),
                BoxCount = data[HITBOX_LIST_LENGTH_OFFSET],
                XPos = BitConverter.ToInt32(data, XPOS_OFFSET),
                YPos = BitConverter.ToInt32(data, YPOS_OFFSET),
                HitstopCounter = data[HITSTOP_COUNTER_OFFSET]
            };

            p.PushBox = GetPushBox(p);

            nint playerExtraPtr = (nint)BitConverter.ToUInt32(data, PLAYER_EXTRA_PTR_OFFSET);
            nint hitboxArrayPtr = (nint)BitConverter.ToUInt32(data, HITBOX_LIST_OFFSET);

            if (playerExtraPtr != 0) p.extra = GetPlayerExtra(playerExtraPtr);
            if (hitboxArrayPtr != 0) p.HitboxSet = GetHitboxes(hitboxArrayPtr, p.BoxCount);

            return p;
        }

        private static Player DummyPlayerStruct()
        {
            return new()
            {
                CharId = 0,
                IsFacingRight = false,
                Status = 0,
                extra = new PlayerExtra() { InvulnCounter = 0},
                HitboxSet = [],
                BoxCount = 0,
                XPos = 0,
                YPos = 0
            };
        }

        internal static PlayerExtra GetPlayerExtra(nint playerExtraPtr)
        {
            byte[] data = Memory.ReadMemory(playerExtraPtr + PLAYER_EXTRA_INVULN_COUNTER_OFFSET, sizeof(byte));
            if (data.Length == 0) return new PlayerExtra { InvulnCounter = 0 };

            return new()
            {
                InvulnCounter = data[0]
            };
        }

        internal static Hitbox GetPushBox(Player p)
        {
            byte index = 4;
            if (p.Status.IsCrouching) index = 0;
            else if (p.Status.IsPushboxType1) index = 2;

            nint xAddr = Memory.GetBaseAddress() + PUSHBOX_ADDRESSES[index] + p.CharId * 2;
            nint yAddr = Memory.GetBaseAddress() + PUSHBOX_ADDRESSES[index+1] + p.CharId * 2;

            short width = BitConverter.ToInt16(Memory.ReadMemory(xAddr, sizeof(short)));
            short height = BitConverter.ToInt16(Memory.ReadMemory(yAddr, sizeof(short)));

            return new Hitbox()
            {
                XOffset = (short)(width / -100),
                YOffset = (short)(height / -100),
                Width = (short)(width / 100 * 2),
                Height = (short)(height / 100)
            };
        }

        internal static Hitbox[] GetHitboxes(nint hitboxArrPtr, int numBoxes)
        {
            byte[] data = Memory.ReadMemory(hitboxArrPtr, numBoxes * HITBOX_ARRAY_STEP);
            if (data.Length == 0) { return []; }
            Hitbox[] output = new Hitbox[numBoxes];

            for (int i = 0; i < numBoxes; i++)
            {
                Hitbox b = ByteArrToHitbox(data, i * HITBOX_ARRAY_STEP);
                if (b.Filler != 0)
                {
                    output = output.Take(i-1).ToArray();
                    break;
                }
                output[i] = b;
            }

            return output;
        }

        internal static Hitbox ByteArrToHitbox(byte[] data, int offset)
        {
            return new()
            {
                XOffset     = BitConverter.ToInt16(data, offset),
                YOffset     = BitConverter.ToInt16(data, offset + 0x02),
                Width       = BitConverter.ToInt16(data, offset + 0x04),
                Height      = BitConverter.ToInt16(data, offset + 0x06),
                BoxTypeId   = BitConverter.ToInt16(data, offset + 0x08),
                Filler      = BitConverter.ToInt16(data, offset + 0x0A)
            };
        }

        internal static bool IsHitBox(Hitbox h) { return h.BoxTypeId == (short)BoxId.HIT; }
        internal static bool IsHurtBox(Hitbox h) { return h.BoxTypeId == (short)BoxId.HURT; }

        internal static Projectile[] GetProjectiles()
        {
            Projectile[] output = new Projectile[PROJECTILE_ARRAY_SIZE];
            byte i = 0;
            nint headNodeAddr = Memory.GetBaseAddress() + PROJECTILE_ARR_HEAD_TAIL_PTR;
            if (headNodeAddr == nint.Zero) return [];
            nint entityPtr = headNodeAddr;
            do
            {
                if (entityPtr == nint.Zero) { break; }
                entityPtr = (nint)BitConverter.ToUInt32(Memory.ReadMemory(entityPtr + 0x08, sizeof(int)));
                output[i++] = ByteArrToProjectile(Memory.ReadMemory(entityPtr, ENTITY_STRUCT_BUFFER));
            } while (entityPtr != headNodeAddr && i < PROJECTILE_ARRAY_SIZE);

            return output.Take(i).ToArray();
        }

        internal static Projectile ByteArrToProjectile(byte[] data)
        {
            return ByteArrToProjectile(data, 0);
        }
        internal static Projectile ByteArrToProjectile(byte[] data, int offset)
        {
            if (data.Length == 0) { return DummyProjectileStruct(); }
            byte numBoxes = data[offset + 0x84];
            nint boxSetArr = (nint)BitConverter.ToUInt32(data, offset + HITBOX_LIST_OFFSET);
            Hitbox[] hitboxes = GetHitboxes(boxSetArr, numBoxes);

            return new()
            {
                ProjId      = BitConverter.ToUInt16(data, offset),
                IsFacingRight = data[offset + IS_FACING_RIGHT_OFFSET] == 1,
                IsActive    = (data[offset + STATUS_OFFSET] & 0b01000000) == 0,
                HitboxSet   = hitboxes,
                BoxCount    = numBoxes,
                XPos        = BitConverter.ToInt32(data, offset + XPOS_OFFSET),
                YPos        = BitConverter.ToInt32(data, offset + YPOS_OFFSET)
            };
        }

        private static Projectile DummyProjectileStruct()
        {
            return new ()
            {
                ProjId = 0,
                IsActive = false,
                IsFacingRight = false,
                HitboxSet = [],
                BoxCount = 0,
                XPos = 0,
                YPos = 0
            };
        }
    }
}
