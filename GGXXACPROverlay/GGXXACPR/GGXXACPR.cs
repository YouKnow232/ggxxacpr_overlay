using System.Diagnostics;

namespace GGXXACPROverlay.GGXXACPR
{
    public static class GGXXACPR
    {
        public const int SCREEN_HEIGHT_PIXELS = 480;
        public const int SCREEN_WIDTH_PIXELS = 640;
        public const int SCREEN_GROUND_PIXEL_OFFSET = 40;

        // Exception to the ActionStatusFlags.IsPlayer1/2 flag. Dizzy bubble is flagged as the opponent's entity while attackable by Dizzy.
        // Makes that flag more of a "Is attackable by" thing. For some reason, Venom balls aren't implemented this way.
        public const int DIZZY_ENTITY_ID = 0x43;

        private const nint CAMERA_ADDR = 0x006D5CD4;
        private const nint PLAYER_1_PTR_ADDR = 0x006D1378;
        private const nint PLAYER_2_PTR_ADDR = 0x006D4C84;
        private static readonly nint[] PLAYER_PTR_ADDRS = [PLAYER_1_PTR_ADDR, PLAYER_2_PTR_ADDR];

        private static readonly nint[] PUSHBOX_ADDRESSES =
        [
            0x00573154, 0x00573B38, // Standing width/height
            0x00573B6C, 0x00571E6C, // Crouching width/height
            0x00571564, 0x00571E6C, // (??) width/height
            0x00573B6C, 0x00573BA0, // Airborne width/height
        ];

        // Y offset values for Airborne pushboxes (Almost always equal to abs(YPos)+4000 except for Kliff)
        private const nint PUSHBOX_P1_JUMP_OFFSET_ADDRESS = 0x006D6378;
        private const nint PUSHBOX_P2_JUMP_OFFSET_ADDRESS = 0x006D637C;

        // short arr, index with CharId*2
        private const int GROUND_THROW_RANGE_ARRAY_ADDR = 0x0057005C; // TODO

        // one byte [P1Throwable, P2Throwable, P1ThrowActive P2ThrowActive]
        private const nint GLOBAL_THROW_FLAGS_ADDR = 0x006D5D7C;

        // Player Struct Offsets
        private const int IS_FACING_RIGHT_OFFSET = 0x02;
        private const int STATUS_OFFSET = 0x0C;
        private const int BUFFER_FLAGS_OFFSET = 0x12;
        private const int ACT_ID_OFFSET = 0x18;
        private const int ANIMATION_FRAME_OFFSET = 0x1C;
        private const int GUARD_FLAGS_OFFSET = 0x2A;
        private const int PLAYER_EXTRA_PTR_OFFSET = 0X2C;
        private const int ATTACK_FLAGS_OFFSET = 0x34;
        private const int COMMAND_FLAGS_OFFSET = 0X38;
        private const int CORE_X_OFFSET = 0x4C;
        private const int CORE_Y_OFFSET = 0x4E;
        private const int SCALE_X_OFFSET = 0x50;
        private const int SCALE_Y_OFFSET= 0x52;
        private const int HITBOX_LIST_OFFSET = 0x54;
        private const int HITBOX_LIST_LENGTH_OFFSET = 0x84;
        private const int HITBOX_ITERATION_VAR_OFFSET = 0x85;
        private const int XPOS_OFFSET = 0xB0;
        private const int YPOS_OFFSET = 0xB4;
        private const int HITSTOP_TIMER_OFFSET = 0xFD;
        private const int MARK_OFFSET = 0xFF;
        // Projectils Arr (Entity Arr)
        private const nint ENTITY_ARR_HEAD_TAIL_PTR = 0x006D27A8;
        private const nint ENTITY_ARR_HEAD_PTR = 0x006D27A8 + 0x04;
        private const nint ENTITY_ARR_TAIL_PTR = 0x006D27A8 + 0x08;
        private const nint ENTITY_LIST_PTR = 0x006D137C;
        private const int ENTITY_ARRAY_SIZE = 32; // Not a game thing, just a lookup limit
        // Projectile Struct Offsets (Similar to Player)
        private const int PROJECTILE_BACK_PTR_OFFSET = 0x04;
        private const int PROJECTILE_NEXT_PTR_OFFSET = 0x08;
        private const int PROJECTILE_PARENT_PTR_OFFSET = 0x20;
        private const int PROJECTILE_PARENT_FLAG_OFFSET = 0x28;
        // Player Extra Struct Offsets
        private const int PLAYER_EXTRA_THROW_PROTECTION_TIMER_OFFSET = 0x18;
        private const int PLAYER_EXTRA_INVULN_COUNTER_OFFSET = 0x2A;
        private const int PLAYER_EXTRA_RC_TIME_OFFSET = 0x32;
        private const int PLAYER_EXTRA_JAM_PARRY_TIME_OFFSET = 0x90;
        private const int PLAYER_EXTRA_COMBO_TIME_OFFSET = 0xF6;
        private const int PLAYER_EXTRA_SLASH_BACK_TIME_OFFSET = 0x010B;
        // Camera Struct Offsets
        private const int CAMERA_X_CENTER_OFFSET = 0x10;
        private const int CAMERA_BOTTOM_EDGE_OFFSET = 0x14;
        private const int CAMERA_LEFT_EDGE_OFFSET = 0x20;
        private const int CAMERA_WIDTH_OFFSET = 0x28;
        private const int CAMERA_HEIGHT_OFFSET = 0x2C;
        private const int CAMERA_ZOOM_OFFSET = 0x44;
        private const int CAMERA_STRUCT_BUFFER = 0x48;
        // Buffer sizes
        private const int ENTITY_STRUCT_BUFFER = 0x130;
        private const int PLAYER_EXTRA_STRUCT_BUFFER = 0x148;
        private const int HITBOX_ARRAY_STEP = 0x0C;

        private static nint playerPointerCache = nint.Zero;
        private static nint Player1Pointer {
            get
            {
                if (playerPointerCache == nint.Zero)
                {
                    playerPointerCache = DereferencePointer(PLAYER_1_PTR_ADDR);
                }
                return playerPointerCache;
            }
        }
        private static nint Player2Pointer
        {
            get
            {
                return Player1Pointer + 0x130;
            }
        }

        private static nint DereferencePointer(nint pointer)
        {
            byte[] data = Memory.ReadMemoryPlusBaseOffset(pointer, sizeof(int));
            if ( data.Length == 0 ) { return nint.Zero; }
            return BitConverter.ToInt32(data);
        }

        public static GameState GetGameState()
        {
            return new GameState(
                GetPlayerStruct(Player1Pointer),
                GetPlayerStruct(Player2Pointer),
                GetCameraStruct(),
                GetEntities(),
                GetGlobals()
            );
        }

        private static GlobalFlags GetGlobals()
        {
            ThrowFlags throwFlags = new();
            byte[] data = Memory.ReadMemoryPlusBaseOffset(GLOBAL_THROW_FLAGS_ADDR, 1);
            if (data.Length != 0) {
                throwFlags = new ThrowFlags(data[0]);
            }

            return new()
            {
                ThrowFlags = throwFlags
            };
        }

        private static Camera GetCameraStruct()
        {
            byte[] data = Memory.ReadMemoryPlusBaseOffset(CAMERA_ADDR, CAMERA_STRUCT_BUFFER);
            if (data.Length == 0) { return new(); }

            return new()
            {
                CenterXPos = BitConverter.ToInt32(data, CAMERA_X_CENTER_OFFSET),
                BottomEdge = BitConverter.ToInt32(data, CAMERA_BOTTOM_EDGE_OFFSET),
                LeftEdge = BitConverter.ToInt32(data, CAMERA_LEFT_EDGE_OFFSET),
                Width = BitConverter.ToInt32(data, CAMERA_WIDTH_OFFSET),
                Height = BitConverter.ToInt32(data, CAMERA_HEIGHT_OFFSET),
                Zoom = BitConverter.ToSingle(data, CAMERA_ZOOM_OFFSET)
            };
        }

        // TODO: cache player struct ptrs, they shouldn't change
        private static Player GetPlayerStruct(nint playerPtr)
        {
            if (playerPtr == nint.Zero) { return new(); }

            byte[] data = Memory.ReadMemory(playerPtr, ENTITY_STRUCT_BUFFER);
            if (data.Length == 0) {
                playerPointerCache = nint.Zero;
                return new();
            }

            var charId = BitConverter.ToUInt16(data);
            var status = BitConverter.ToUInt32(data, STATUS_OFFSET);
            var boxCount = data[HITBOX_LIST_LENGTH_OFFSET];
            var yPos = BitConverter.ToInt32(data, YPOS_OFFSET);
            var pushBox = GetPushBox(charId, status, yPos);

            nint playerExtraPtr = (nint)BitConverter.ToUInt32(data, PLAYER_EXTRA_PTR_OFFSET);
            nint hitboxArrayPtr = (nint)BitConverter.ToUInt32(data, HITBOX_LIST_OFFSET);

            PlayerExtra extra = new();
            if (playerExtraPtr != 0) { extra = GetPlayerExtra(playerExtraPtr); }
            Hitbox[] boxSet = [];
            if (hitboxArrayPtr != 0) { boxSet = GetHitboxes(hitboxArrayPtr, boxCount); }

            return new()
            {
                CharId           = charId,
                IsFacingRight    = data[IS_FACING_RIGHT_OFFSET] == 1,
                Status           = status,
                BufferFlags      = BitConverter.ToUInt16(data, BUFFER_FLAGS_OFFSET),
                ActionId         = BitConverter.ToUInt16(data, ACT_ID_OFFSET),
                AnimationCounter = BitConverter.ToUInt16(data, ANIMATION_FRAME_OFFSET),
                GuardFlags       = BitConverter.ToUInt16(data, GUARD_FLAGS_OFFSET),
                Extra            = extra,
                AttackFlags      = BitConverter.ToUInt32(data, ATTACK_FLAGS_OFFSET),
                CommandFlags     = BitConverter.ToUInt16(data, COMMAND_FLAGS_OFFSET),
                CoreX            = BitConverter.ToInt16(data, CORE_X_OFFSET),
                CoreY            = BitConverter.ToInt16(data, CORE_Y_OFFSET),
                ScaleX           = BitConverter.ToInt16(data, SCALE_X_OFFSET),
                ScaleY           = BitConverter.ToInt16(data, SCALE_Y_OFFSET),
                HitboxSet        = boxSet,
                BoxCount         = boxCount,
                BoxIter          = data[HITBOX_ITERATION_VAR_OFFSET],
                XPos             = BitConverter.ToInt32(data, XPOS_OFFSET),
                YPos             = yPos,
                HitstopCounter   = data[HITSTOP_TIMER_OFFSET],
                Mark             = data[MARK_OFFSET],
                PushBox          = pushBox
            };
        }

        private static PlayerExtra GetPlayerExtra(nint playerExtraPtr)
        {
            byte[] data = Memory.ReadMemory(playerExtraPtr, PLAYER_EXTRA_STRUCT_BUFFER);
            if (data.Length == 0) { return new(); }

            return new()
            {
                ThrowProtectionTimer = BitConverter.ToInt16(data, PLAYER_EXTRA_THROW_PROTECTION_TIMER_OFFSET),
                InvulnCounter = data[PLAYER_EXTRA_INVULN_COUNTER_OFFSET],
                RCTime = data[PLAYER_EXTRA_RC_TIME_OFFSET],
                JamParryTime = data[PLAYER_EXTRA_JAM_PARRY_TIME_OFFSET],
                ComboTime = BitConverter.ToInt16(data, PLAYER_EXTRA_COMBO_TIME_OFFSET),
                SBTime = data[PLAYER_EXTRA_SLASH_BACK_TIME_OFFSET]
            };
        }

        // TODO: Cache the data at the pushbox addresses. They shouldn't change.
        private static Hitbox GetPushBox(ushort charId, ActionStateFlags status, int yPos)
        {
            int yOffset = 0;
            byte index = 4;
            if (status.IsCrouching) index = 0;
            else if (status.IsPushboxType1) index = 2;
            else if (status.IsAirborne)
            {
                index = 6;
                // Special offsets for pushbox collision checks
                if (status.IsPlayer1)
                {
                    yOffset = BitConverter.ToInt32(Memory.ReadMemoryPlusBaseOffset(PUSHBOX_P1_JUMP_OFFSET_ADDRESS, sizeof(int))) + yPos;
                }
                else if (status.IsPlayer2)
                {
                    yOffset = BitConverter.ToInt32(Memory.ReadMemoryPlusBaseOffset(PUSHBOX_P2_JUMP_OFFSET_ADDRESS, sizeof(int))) + yPos;
                }
            }

            nint xPtr = Memory.GetBaseAddress() + PUSHBOX_ADDRESSES[index] + charId * 2;
            nint yPtr = Memory.GetBaseAddress() + PUSHBOX_ADDRESSES[index + 1] + charId * 2;

            short w = BitConverter.ToInt16(Memory.ReadMemory(xPtr, sizeof(short)));
            short h = BitConverter.ToInt16(Memory.ReadMemory(yPtr, sizeof(short)));

            return new Hitbox()
            {
                XOffset = (short)(w / -100),
                YOffset = (short)((h + yOffset) / -100),
                Width   = (short)(w / 100 * 2),
                Height  = (short)(h / 100)
            };
        }

        private static Hitbox[] GetHitboxes(nint hitboxArrPtr, int numBoxes)
        {
            byte[] data = Memory.ReadMemory(hitboxArrPtr, numBoxes * HITBOX_ARRAY_STEP);
            if (data.Length == 0) { return []; }
            Hitbox[] output = new Hitbox[numBoxes];

            for (int i = 0; i < numBoxes; i++)
            {
                Hitbox b = ByteArrToHitbox(data, i * HITBOX_ARRAY_STEP);
                output[i] = b;
            }

            return output;
        }

        private static Hitbox ByteArrToHitbox(byte[] data, int offset)
        {
            return new()
            {
                XOffset = BitConverter.ToInt16(data, offset),
                YOffset = BitConverter.ToInt16(data, offset + 0x02),
                Width   = BitConverter.ToInt16(data, offset + 0x04),
                Height  = BitConverter.ToInt16(data, offset + 0x06),
                BoxTypeId = (BoxId)BitConverter.ToInt16(data, offset + 0x08),
                BoxFlags  = BitConverter.ToInt16(data, offset + 0x0A)
            };
        }

        private static Entity[] GetEntities()
        {
            Entity[] entityArray = ByteArrayToEntities(Memory.ReadMemory(Player1Pointer, ENTITY_STRUCT_BUFFER * ENTITY_ARRAY_SIZE));

            return entityArray.Skip(2).Where(e => e.Id != 0).ToArray();
        }

        private static Entity[] ByteArrayToEntities(byte[] data)
        {
            int total = data.Length / ENTITY_STRUCT_BUFFER;
            Entity[] output = new Entity[total];
            for (int i = 0; i < total; i++)
            {
                output[i] = ByteArrToEntity(data, ENTITY_STRUCT_BUFFER * i);
            }

            return output;
        }

        private static Entity ByteArrToProjectile(byte[] data)
        {
            return ByteArrToEntity(data, 0);
        }
        private static Entity ByteArrToEntity(byte[] data, int offset)
        {
            if (data.Length == 0) { return new(); }
            byte numBoxes = data[offset + HITBOX_LIST_LENGTH_OFFSET];
            nint boxSetArr = (nint)BitConverter.ToUInt32(data, offset + HITBOX_LIST_OFFSET);
            Hitbox[] hitboxes = [];
            if (boxSetArr != nint.Zero)
            {
                hitboxes = GetHitboxes(boxSetArr, numBoxes);
            }

            return new()
            {
                Id              = BitConverter.ToUInt16(data, offset),
                IsFacingRight   = data[offset + IS_FACING_RIGHT_OFFSET] == 1,
                BackPtr         = (nint)BitConverter.ToUInt32(data, offset + PROJECTILE_BACK_PTR_OFFSET),
                NextPtr         = (nint)BitConverter.ToUInt32(data, offset + PROJECTILE_NEXT_PTR_OFFSET),
                Status          = BitConverter.ToUInt32(data, offset + STATUS_OFFSET),
                ParentPtrRaw    = (nint)BitConverter.ToUInt32(data, offset + PROJECTILE_PARENT_PTR_OFFSET),
                ParentFlag      = BitConverter.ToUInt16(data, offset + PROJECTILE_PARENT_FLAG_OFFSET),
                CoreX           = BitConverter.ToInt16(data, CORE_X_OFFSET + offset),
                CoreY           = BitConverter.ToInt16(data, CORE_Y_OFFSET + offset),
                ScaleX          = BitConverter.ToInt16(data, SCALE_X_OFFSET + offset),
                ScaleY          = BitConverter.ToInt16(data, SCALE_Y_OFFSET + offset),
                HitboxSetPtr    = (nint)BitConverter.ToUInt32(data, offset + HITBOX_LIST_OFFSET),
                HitboxSet       = hitboxes,
                BoxCount        = numBoxes,
                XPos            = BitConverter.ToInt32(data, offset + XPOS_OFFSET),
                YPos            = BitConverter.ToInt32(data, offset + YPOS_OFFSET)
            };
        }
    }
}
