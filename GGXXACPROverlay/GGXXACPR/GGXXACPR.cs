using System.Diagnostics;
using SharpDX.Direct2D1;

namespace GGXXACPROverlay.GGXXACPR
{
    public static class GGXXACPR
    {
        public static readonly int SCREEN_HEIGHT_PIXELS = 480;
        public static readonly int SCREEN_GROUND_PIXEL_OFFSET = 40;

        private static readonly nint CAMERA_ADDR = 0x006D5CD4;
        private static readonly nint PLAYER_1_PTR_ADDR = 0x006D1378;
        private static readonly nint PLAYER_2_PTR_ADDR = 0x006D4C84;
        private static readonly nint[] PLAYER_PTR_ADDRS = [PLAYER_1_PTR_ADDR, PLAYER_2_PTR_ADDR];

        private static readonly nint[] PUSHBOX_ADDRESSES =
        [
            0x00573154, 0x00573B38, // Standing x,y
            0x00573B6C, 0x00571E6C, // Crouching x,y
            0x00571564, 0x00571E6C  // ?? x,y
        ];

        // Player Struct Offsets
        private static readonly int IS_FACING_RIGHT_OFFSET = 0x02;
        private static readonly int STATUS_OFFSET = 0x0C;
        private static readonly int ANIMATION_FRAME_OFFSET = 0x1C;
        private static readonly int GUARD_FLAGS_OFFSET = 0x2A;
        private static readonly int PLAYER_EXTRA_PTR_OFFSET = 0X2C;
        private static readonly int ATTACK_FLAGS_OFFSET = 0x34;
        private static readonly int COMMAND_FLAGS_OFFSET = 0X38;
        private static readonly int HITBOX_LIST_OFFSET = 0x54;
        private static readonly int HITBOX_LIST_LENGTH_OFFSET = 0x84;
        private static readonly int HITBOX_ITERATION_VAR_OFFSET = 0x85;
        private static readonly int XPOS_OFFSET = 0xB0;
        private static readonly int YPOS_OFFSET = 0xB4;
        private static readonly int HITSTOP_TIMER_OFFSET = 0xFD;
        // Projectils Arr (Entity Arr)
        private static readonly nint ENTITY_ARR_HEAD_TAIL_PTR = 0x006D27A8;
        //private static readonly nint ENTITY_ARR_HEAD_PTR = 0x006D27A8 + 0x04;
        //private static readonly nint ENTITY_ARR_TAIL_PTR = 0x006D27A8 + 0x08;
        //private static readonly nint ENTITY_LIST_PTR = 0x006D137C;
        private static readonly int ENTITY_ARRAY_SIZE = 32;
        // Projectile Struct Offsets (Similar to Player)
        private static readonly int PROJECTILE_BACK_PTR_OFFSET = 0x04;
        private static readonly int PROJECTILE_NEXT_PTR_OFFSET = 0x08;
        private static readonly int PROJECTILE_PARENT_PTR_OFFSET = 0x20;
        private static readonly int PROJECTILE_PARENT_FLAG_OFFSET = 0x28;
        // Player Extra Struct Offsets
        private static readonly int PLAYER_EXTRA_INVULN_COUNTER_OFFSET = 0x2A;
        private static readonly int PLAYER_EXTRA_RC_TIME_OFFSET = 0x32;
        private static readonly int PLAYER_EXTRA_COMBO_TIME_OFFSET = 0xF6;
        private static readonly int PLAYER_EXTRA_SLASH_BACK_TIME_OFFSET = 0x010B;
        // Camera Struct Offsets
        private static readonly int CAMERA_X_CENTER_OFFSET = 0x10;
        private static readonly int CAMERA_BOTTOM_EDGE_OFFSET = 0x14;
        private static readonly int CAMERA_LEFT_EDGE_OFFSET = 0x20;
        private static readonly int CAMERA_WIDTH_OFFSET = 0x28;
        private static readonly int CAMERA_HEIGHT_OFFSET = 0x2C;
        private static readonly int CAMERA_ZOOM_OFFSET = 0x44;
        private static readonly int CAMERA_STRUCT_BUFFER = 0x48;
        // Buffer sizes
        private static readonly int ENTITY_STRUCT_BUFFER = 0x130;
        private static readonly int PLAYER_EXTRA_STRUCT_BUFFER = 0x148;
        private static readonly int HITBOX_ARRAY_STEP = 0x0C;

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

        public static nint DereferencePointer(nint pointer)
        {
            return BitConverter.ToInt32(Memory.ReadMemoryPlusBaseOffset(pointer, sizeof(int)));
        }

        public static GameState GetGameState()
        {
            return new GameState(
                GetPlayerStruct(Player1Pointer),
                GetPlayerStruct(Player2Pointer),
                GetCameraStruct(),
                GetEntities()
            );
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
            // Testing new code
            //byte[] tempData = Memory.ReadMemoryPlusBaseOffset(PLAYER_PTR_ADDRS[player], sizeof(int));
            //if (tempData.Length == 0) { return new(); }
            //nint playerPtr = (nint)BitConverter.ToUInt32(tempData);

            byte[] data = Memory.ReadMemory(playerPtr, ENTITY_STRUCT_BUFFER);
            if (data.Length == 0) {
                playerPointerCache = nint.Zero;
                return new();
            }

            var charId = BitConverter.ToUInt16(data);
            var status = BitConverter.ToUInt32(data, STATUS_OFFSET);
            var boxCount = data[HITBOX_LIST_LENGTH_OFFSET];
            var pushBox = GetPushBox(charId, status);

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
                AnimationCounter = BitConverter.ToUInt16(data, ANIMATION_FRAME_OFFSET),
                GuardFlags       = BitConverter.ToUInt16(data, GUARD_FLAGS_OFFSET),
                Extra            = extra,
                AttackFlags      = BitConverter.ToUInt32(data, ATTACK_FLAGS_OFFSET),
                CommandFlags     = BitConverter.ToUInt16(data, COMMAND_FLAGS_OFFSET),
                HitboxSet        = boxSet,
                BoxCount         = boxCount,
                BoxIter          = data[HITBOX_ITERATION_VAR_OFFSET],
                XPos             = BitConverter.ToInt32(data, XPOS_OFFSET),
                YPos             = BitConverter.ToInt32(data, YPOS_OFFSET),
                HitstopCounter   = data[HITSTOP_TIMER_OFFSET],
                PushBox          = pushBox
            };
        }

        private static PlayerExtra GetPlayerExtra(nint playerExtraPtr)
        {
            byte[] data = Memory.ReadMemory(playerExtraPtr, PLAYER_EXTRA_STRUCT_BUFFER);
            if (data.Length == 0) { return new(); }

            return new()
            {
                InvulnCounter = data[PLAYER_EXTRA_INVULN_COUNTER_OFFSET],
                RCTime = data[PLAYER_EXTRA_RC_TIME_OFFSET],
                ComboTime = BitConverter.ToInt16(data, PLAYER_EXTRA_COMBO_TIME_OFFSET),
                SBTime = data[PLAYER_EXTRA_SLASH_BACK_TIME_OFFSET]
            };
        }

        // TODO: Cache the data at the pushbox addresses. They shouldn't change.
        private static Hitbox GetPushBox(ushort charId, ActionStateFlags status)
        {
            byte index = 4;
            if (status.IsCrouching) index = 0;
            else if (status.IsPushboxType1) index = 2;

            nint xPtr = Memory.GetBaseAddress() + PUSHBOX_ADDRESSES[index] + charId * 2;
            nint yPtr = Memory.GetBaseAddress() + PUSHBOX_ADDRESSES[index + 1] + charId * 2;

            short w = BitConverter.ToInt16(Memory.ReadMemory(xPtr, sizeof(short)));
            short h = BitConverter.ToInt16(Memory.ReadMemory(yPtr, sizeof(short)));

            return new Hitbox()
            {
                XOffset = (short)(w / -100),
                YOffset = (short)(h / -100),
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

        internal static Hitbox ByteArrToHitbox(byte[] data, int offset)
        {
            return new()
            {
                XOffset = BitConverter.ToInt16(data, offset),
                YOffset = BitConverter.ToInt16(data, offset + 0x02),
                Width   = BitConverter.ToInt16(data, offset + 0x04),
                Height  = BitConverter.ToInt16(data, offset + 0x06),
                BoxTypeId = (BoxId)BitConverter.ToInt16(data, offset + 0x08),
                Filler  = BitConverter.ToInt16(data, offset + 0x0A)
            };
        }

        private static Entity[] GetEntities()
        {
            Entity[] entityArray = ByteArrayToEntities(Memory.ReadMemory(Player1Pointer, ENTITY_STRUCT_BUFFER * ENTITY_ARRAY_SIZE));
            Entity[] validEntities = entityArray.Where(e => e.Id != 0).ToArray();

            // Deriving player ownership by tracing the parent pointer chain to a player pointer
            // Assumes the lower 6 bytes are a factor of 0x130 and that player 1's pointer begins with 0x000 (safe assumptions for +R)
            var parentIndices = entityArray.Select(e => (int)(e.ParentPtrRaw & 0xFFF) / 0x130).ToArray();
            parentIndices[0] = 0;   // P1
            parentIndices[1] = 1;   // P2

            int nextParentIndex;
            for (int j = 0; j < validEntities.Length; j++)
            {
                for(int i = 0; i < entityArray.Length; i++)
                {
                    nextParentIndex = parentIndices[i];
                    if (nextParentIndex > 1)
                    {
                        parentIndices[i] = parentIndices[nextParentIndex];
                    }
                }
            }

            // DEBUG
            bool reduced = !parentIndices.Any(i => i > 1);
            Debug.WriteLine($"Ent arr has completely reduced: {reduced}");

            // Assigning ownership
            for (int i = 0; i < entityArray.Length; i++)
            {
                entityArray[i].ParentIndex = parentIndices[i];
            }

            // Skip player entries and filter out blank entries
            return entityArray.Skip(2).Where(e => e.Id != 0).ToArray();
        }

        private static Entity[] ByteArrayToEntities(byte[] data)
        {
            int total = data.Length / ENTITY_STRUCT_BUFFER;
            Entity[] output = new Entity[total];
            for (int i = 0; i < total; i++)
            {
                output[i] = ByteArrToProjectile(data, ENTITY_STRUCT_BUFFER * i);
            }

            return output;
        }

        private static Entity ByteArrToProjectile(byte[] data)
        {
            return ByteArrToProjectile(data, 0);
        }
        private static Entity ByteArrToProjectile(byte[] data, int offset)
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
                Id = BitConverter.ToUInt16(data, offset),
                IsFacingRight = data[offset + IS_FACING_RIGHT_OFFSET] == 1,
                BackPtr = (nint)BitConverter.ToUInt32(data, offset + PROJECTILE_BACK_PTR_OFFSET),
                NextPtr = (nint)BitConverter.ToUInt32(data, offset + PROJECTILE_NEXT_PTR_OFFSET),
                Status = BitConverter.ToUInt32(data, offset + STATUS_OFFSET),
                ParentPtrRaw = (nint)BitConverter.ToUInt32(data, offset + PROJECTILE_PARENT_PTR_OFFSET),
                ParentFlag = BitConverter.ToUInt16(data, offset + PROJECTILE_PARENT_FLAG_OFFSET),
                HitboxSetPtr = (nint)BitConverter.ToUInt32(data, offset + HITBOX_LIST_OFFSET),
                HitboxSet = hitboxes,
                BoxCount = numBoxes,
                XPos = BitConverter.ToInt32(data, offset + XPOS_OFFSET),
                YPos = BitConverter.ToInt32(data, offset + YPOS_OFFSET)
            };
        }
    }
}
