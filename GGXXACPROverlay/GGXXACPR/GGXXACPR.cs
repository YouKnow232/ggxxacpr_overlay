namespace GGXXACPROverlay.GGXXACPR
{
    public static class GGXXACPR
    {
        public const int SCREEN_HEIGHT_PIXELS = 480;
        public const int SCREEN_WIDTH_PIXELS = 640;
        public const int SCREEN_GROUND_PIXEL_OFFSET = 40;
        public const int NUMBER_OF_CHARACTERS = 25;

        public const nint IN_GAME_FLAG = 0x007101F4;
        public const nint GAME_VER_FLAG = 0x006D0538;   // 0=AC, 1=+R

        #region Special case constants
        // Exception to the ActionStatusFlags.IsPlayer1/2 flag. Dizzy bubble is flagged as the opponent's entity while attackable by Dizzy.
        // Makes that flag more of a "Is attackable by" thing. For some reason, Venom balls aren't implemented this way.
        public const int DIZZY_ENTITY_ID = 0x43;
        // The following moves use Player.Mark to denote parry active frames instead of the parry flag
        public const int AXL_TENHOU_SEKI_UPPER_ACT_ID = 188;
        public const int AXL_TENHOU_SEKI_LOWER_ACT_ID = 189;
        public const int DIZZY_EX_NECRO_UNLEASHED_ACT_ID = 247;
        // This move has a special pushbox adjustment
        public const int BRIDGET_SHOOT_ACT_ID = 134;
        public const int BRIDGET_SHOOT_PUSHBOX_ADJUST = 7000;
        // For whatever reason, this throw range is hardcoded and not in the array with everything else
        public const int SPECIAL_CASE_COMMAND_THROW_ID = 0x19;
        public const int SPECIAL_CASE_COMMAND_THROW_RANGE = 11000; // GGXXACPR_Win.exe+12054F
        #endregion

        private const nint CAMERA_ADDR = 0x006D5CD4;
        private const nint PLAYER_1_PTR_ADDR = 0x006D1378;
        private const nint PLAYER_2_PTR_ADDR = 0x006D4C84;
        private static readonly nint[] PLAYER_PTR_ADDRS = [PLAYER_1_PTR_ADDR, PLAYER_2_PTR_ADDR];

        private static readonly nint[] PUSHBOX_ADDRESSES =
        [
            0x00573154, 0x00573B38, // Crouching width/height
            0x00573B6C, 0x00571E6C, // (??) width/height
            0x00571564, 0x00571E6C, // Standing width/height
            0x00573B6C, 0x00573BA0, // Airborne width/height
        ];

        private const nint PUSHBOX_STANDING_WIDTH_ARRAY = 0x00571564;
        private const nint PUSHBOX_STANDING_HEIGHT_ARRAY = 0x00571E6C;
        private const nint PUSHBOX_CROUCHING_WIDTH_ARRAY = 0x00573154;
        private const nint PUSHBOX_CROUCHING_HEIGHT_ARRAY = 0x00573B38;
        private const nint PUSHBOX_AIR_WIDTH_ARRAY = 0x00573B6C;
        private const nint PUSHBOX_AIR_HEIGHT_ARRAY = 0x00573BA0;

        // Y offset values for Airborne pushboxes (Almost always equal to abs(YPos)+4000 except for Kliff)
        private const nint PUSHBOX_P1_JUMP_OFFSET_ADDRESS = 0x006D6378;
        private const nint PUSHBOX_P2_JUMP_OFFSET_ADDRESS = 0x006D637C;

        // short arr, index with CharId*2
        // TODO
        private const nint PLUSR_GROUND_THROW_RANGE_ARRAY = 0x0057005C;
        private const nint AC_GROUND_THROW_RANGE_ARRAY = 0x0056FF6C;
        private const nint PLUSR_AIR_THROW_HORIZONTAL_RANGE_ARRAY = 0x005708DC;
        private const nint AC_AIR_THROW_HORIZONTAL_RANGE_ARRAY = 0x00570174;
        private const nint AIR_THROW_LOWER_RANGE_ARRAY = 0x005709B4;
        private const nint AIR_THROW_UPPER_RANGE_ARRAY = 0x00570A8C;

        #region global data (structless afaik)
        // one byte [P1Throwable, P2Throwable, P1ThrowActive P2ThrowActive]
        private const nint GLOBAL_THROW_FLAGS_ADDR = 0x006D5D7C;
        private const nint COMMAND_GRAB_ID_ADDR = 0x006D6384;
        private const nint COMMAND_GRAB_RANGE_LOOKUP_TABLE = 0x00572110;
        private const int COMMAND_GRAB_RANGE_LOOKUP_TABLE_SIZE = 27;
        // 1 = normal, 0 = do not simulate, -1 = rewinding (stays at 0 for frame stepping)
        private const nint GLOBAL_REPLAY_SIMULATE_ADDR = 0x007D5788;
        // 0 = not paused, 1 or 2 = paused (not sure the difference between 1 and 2)
        private const nint GLOBAL_PAUSE_VAR_ADDR = 0x007109E4;
        #endregion

        #region Struct Field Offsets
        // Player Struct Offsets
        private const int IS_FACING_RIGHT_OFFSET = 0x02;
        private const int STATUS_OFFSET = 0x0C;
        private const int BUFFER_FLAGS_OFFSET = 0x12;
        private const int ACT_ID_OFFSET = 0x18;
        private const int ANIMATION_FRAME_OFFSET = 0x1C;
        private const int PLAYER_INDEX_OFFSET = 0x27;
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
        #endregion

        // Buffer sizes
        private const int ENTITY_STRUCT_BUFFER = 0x130;
        private const int PLAYER_EXTRA_STRUCT_BUFFER = 0x148;
        private const int HITBOX_ARRAY_STEP = 0x0C;

        #region Cached Properties
        // Caches
        private static nint _playerPointerCache = nint.Zero;
        private static short[] _pushBoxStandingWidths = [];
        private static short[] _pushBoxStandingHeights = [];
        private static short[] _pushBoxCrouchingWidths = [];
        private static short[] _pushBoxCrouchingHeights = [];
        private static short[] _pushBoxAirWidths = [];
        private static short[] _pushBoxAirHeights = [];
        private static short[] _cmdGrabRangeArray = [];
        private static short[] _groundThrowRangesPlusR = [];
        private static short[] _groundThrowRangesAC = [];
        private static short[] _airThrowRangesPlusR = [];
        private static short[] _airThrowRangesAC = [];
        private static short[] _airThrowUpperBounds = [];
        private static short[] _airThrowLowerBounds = [];
        private static nint Player1Pointer {
            get
            {
                if (_playerPointerCache == nint.Zero)
                {
                    _playerPointerCache = DereferencePointer(PLAYER_1_PTR_ADDR);
                }
                return _playerPointerCache;
            }
        }
        private static nint Player2Pointer
        {
            get { return Player1Pointer + ENTITY_STRUCT_BUFFER; }
        }
        private static short[] CommandGrabRanges
        {
            get
            {
                if (_cmdGrabRangeArray.Length == 0)
                {
                    var data = Memory.ReadMemoryPlusBaseOffset(COMMAND_GRAB_RANGE_LOOKUP_TABLE,
                        sizeof(short) * COMMAND_GRAB_RANGE_LOOKUP_TABLE_SIZE);
                    if (data.Length == 0) return [];

                    _cmdGrabRangeArray = data.Chunk(sizeof(short)).Select(chunk => BitConverter.ToInt16(chunk)).ToArray();
                }

                return _cmdGrabRangeArray;
            }
        }
        private static short[] PushBoxStandingWidths { get => CharacterWORDArrayCacheAccessor(ref _pushBoxStandingWidths, PUSHBOX_STANDING_WIDTH_ARRAY); }
        private static short[] PushBoxStandingHeights { get => CharacterWORDArrayCacheAccessor(ref _pushBoxStandingHeights, PUSHBOX_STANDING_HEIGHT_ARRAY); }
        private static short[] PushBoxCrouchingWidths { get => CharacterWORDArrayCacheAccessor(ref _pushBoxCrouchingWidths, PUSHBOX_CROUCHING_WIDTH_ARRAY); }
        private static short[] PushBoxCrouchingHeights { get => CharacterWORDArrayCacheAccessor(ref _pushBoxCrouchingHeights, PUSHBOX_CROUCHING_HEIGHT_ARRAY); }
        private static short[] PushBoxAirWidths { get => CharacterWORDArrayCacheAccessor(ref _pushBoxAirWidths, PUSHBOX_AIR_WIDTH_ARRAY); }
        private static short[] PushBoxAirHeights { get => CharacterWORDArrayCacheAccessor(ref _pushBoxAirHeights, PUSHBOX_AIR_HEIGHT_ARRAY); }
        private static short[] GroundThrowRangesPlusR { get => CharacterWORDArrayCacheAccessor(ref _groundThrowRangesPlusR, PLUSR_GROUND_THROW_RANGE_ARRAY); }
        private static short[] GroundThrowRangesAC { get => CharacterWORDArrayCacheAccessor(ref _groundThrowRangesAC, AC_GROUND_THROW_RANGE_ARRAY); }
        private static short[] AirThrowRangesPlusR { get => CharacterWORDArrayCacheAccessor(ref _airThrowRangesPlusR, PLUSR_AIR_THROW_HORIZONTAL_RANGE_ARRAY); }
        private static short[] AirThrowRangesAC { get => CharacterWORDArrayCacheAccessor(ref _airThrowRangesAC, AC_AIR_THROW_HORIZONTAL_RANGE_ARRAY); }
        private static short[] AirThrowUpperBounds { get => CharacterWORDArrayCacheAccessor(ref _airThrowUpperBounds, AIR_THROW_UPPER_RANGE_ARRAY); }
        private static short[] AirThrowLowerBounds { get => CharacterWORDArrayCacheAccessor(ref _airThrowLowerBounds, AIR_THROW_LOWER_RANGE_ARRAY); }
        private static short[] CharacterWORDArrayCacheAccessor(ref short[] cache, nint targetAddress)
        {
            if (cache.Length == 0)
            {
                var data = Memory.ReadMemoryPlusBaseOffset(targetAddress,
                    sizeof(short) * (NUMBER_OF_CHARACTERS + 1));
                if (data.Length == 0) return [];

                cache = data.Chunk(sizeof(short)).Select(chunk => BitConverter.ToInt16(chunk)).ToArray();
            }

            return cache;
        }
        #endregion

        private static nint DereferencePointer(nint pointer)
        {
            byte[] data = Memory.ReadMemoryPlusBaseOffset(pointer, sizeof(int));
            if ( data.Length == 0 ) { return nint.Zero; }
            return BitConverter.ToInt32(data);
        }

        public static bool ShouldRender()
        {
            byte[] data = Memory.ReadMemoryPlusBaseOffset(IN_GAME_FLAG, sizeof(byte));
            if (data.Length == 0) { Memory.HandleSystemError("In-game flag read error"); }

            return data[0] == 1;
        }

        public static short LookUpCommandGrabRange(int cmdGrabId)
        {
            if (CommandGrabRanges.Length == 0) { return 0; }

            return CommandGrabRanges[cmdGrabId];
        }

        public static Hitbox GetThrowBox(GameState state, Player p)
        {
            if (p.Status.IsAirborne)
            {
                short horizontalRange;
                if (state.GlobalFlags.GameVersionFlag == GameVersion.PLUS_R)
                {
                    horizontalRange = AirThrowRangesPlusR[(int)p.CharId];
                }
                else
                {
                    horizontalRange = AirThrowRangesAC[(int)p.CharId];
                }
                short upperBound = AirThrowUpperBounds[(int)p.CharId];
                short lowerBound = AirThrowLowerBounds[(int)p.CharId];

                return new Hitbox()
                {
                    XOffset = (short)(p.PushBox.XOffset - horizontalRange / 100),
                    YOffset = (short)(p.PushBox.YOffset + p.PushBox.Height + upperBound / 100),
                    Width   = (short)(p.PushBox.Width + horizontalRange / 50),
                    Height  = (short)((lowerBound - upperBound) / 100),
                };
            }
            else
            {
                short range;
                if (state.GlobalFlags.GameVersionFlag == GameVersion.PLUS_R)
                {
                    range = GroundThrowRangesPlusR[(int)p.CharId];
                }
                else
                {
                    range = GroundThrowRangesAC[(int)p.CharId];
                }

                return new Hitbox()
                {
                    XOffset = (short)(p.PushBox.XOffset - range / 100),
                    YOffset = p.PushBox.YOffset,
                    Width   = (short)(p.PushBox.Width + range / 50),
                    Height  = p.PushBox.Height,
                };
            }
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
            byte[] data;

            ThrowFlags throwFlags = new();
            data = Memory.ReadMemoryPlusBaseOffset(GLOBAL_THROW_FLAGS_ADDR, 1);
            if (data.Length != 0) {
                throwFlags = new ThrowFlags(data[0]);
            }

            int p1CommandThrowId = 0;
            int p2CommandThrowId = 0;
            data = Memory.ReadMemoryPlusBaseOffset(COMMAND_GRAB_ID_ADDR, sizeof(int) * 2);
            if (data.Length != 0)
            {
                p1CommandThrowId = BitConverter.ToInt32(data);
                p2CommandThrowId = BitConverter.ToInt32(data, sizeof(int));
            }

            data = Memory.ReadMemoryPlusBaseOffset(COMMAND_GRAB_RANGE_LOOKUP_TABLE + p1CommandThrowId*2, sizeof(short));
            int p1CommandThrowRange = 0;
            if (data.Length != 0)
            {
                p1CommandThrowRange = BitConverter.ToInt16(data);
            }
            data = Memory.ReadMemoryPlusBaseOffset(COMMAND_GRAB_RANGE_LOOKUP_TABLE + p2CommandThrowId * 2, sizeof(short));
            int p2CommandThrowRange = 0;
            if (data.Length != 0)
            {
                p2CommandThrowRange = BitConverter.ToInt16(data);
            }

            GameVersion gameVer = GameVersion.PLUS_R;
            data = Memory.ReadMemoryPlusBaseOffset(GAME_VER_FLAG, sizeof(int));
            if (data.Length != 0)
            {
                gameVer = (GameVersion)BitConverter.ToInt32(data);
            }

            int pauseVar = 0;
            data = Memory.ReadMemoryPlusBaseOffset(GLOBAL_PAUSE_VAR_ADDR, sizeof(int));
            if (data.Length != 0)
            {
                pauseVar = BitConverter.ToInt32(data);
            }

            int replaySim = 0;
            data = Memory.ReadMemoryPlusBaseOffset(GLOBAL_REPLAY_SIMULATE_ADDR, sizeof(int));
            if (data.Length != 0)
            {
                replaySim = BitConverter.ToInt32(data);
            }

            return new()
            {
                ThrowFlags            = throwFlags,
                P1ActiveCommandGrabId = p1CommandThrowId,
                P2ActiveCommandGrabId = p2CommandThrowId,
                P1CommandGrabRange    = p1CommandThrowRange,
                P2CommandGrabRange    = p2CommandThrowRange,
                GameVersionFlag       = gameVer,
                PauseState1           = pauseVar,
                ReplaySimState        = replaySim,
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
                LeftEdge   = BitConverter.ToInt32(data, CAMERA_LEFT_EDGE_OFFSET),
                Width      = BitConverter.ToInt32(data, CAMERA_WIDTH_OFFSET),
                Height     = BitConverter.ToInt32(data, CAMERA_HEIGHT_OFFSET),
                Zoom       = BitConverter.ToSingle(data, CAMERA_ZOOM_OFFSET)
            };
        }

        private static Player GetPlayerStruct(nint playerPtr)
        {
            if (playerPtr == nint.Zero) { return new(); }

            byte[] data = Memory.ReadMemory(playerPtr, ENTITY_STRUCT_BUFFER);
            if (data.Length == 0) {
                _playerPointerCache = nint.Zero;
                return new();
            }
            var boxCount = data[HITBOX_LIST_LENGTH_OFFSET];

            nint playerExtraPtr = (nint)BitConverter.ToUInt32(data, PLAYER_EXTRA_PTR_OFFSET);
            nint hitboxArrayPtr = (nint)BitConverter.ToUInt32(data, HITBOX_LIST_OFFSET);

            PlayerExtra extra = new();
            if (playerExtraPtr != 0) { extra = GetPlayerExtra(playerExtraPtr); }
            Hitbox[] boxSet = [];
            if (hitboxArrayPtr != 0) { boxSet = GetHitboxes(hitboxArrayPtr, boxCount); }
            var charId = (CharacterID)BitConverter.ToUInt16(data);
            var status = BitConverter.ToUInt32(data, STATUS_OFFSET);
            var actId = BitConverter.ToUInt16(data, ACT_ID_OFFSET);
            var yPos = BitConverter.ToInt32(data, YPOS_OFFSET);

            var pushBox = GetPushBox((ushort)charId, status, actId, yPos, boxSet);

            return new()
            {
                CharId           = charId,
                IsFacingRight    = data[IS_FACING_RIGHT_OFFSET] == 1,
                Status           = status,
                BufferFlags      = BitConverter.ToUInt16(data, BUFFER_FLAGS_OFFSET),
                ActionId         = actId,
                AnimationCounter = BitConverter.ToUInt16(data, ANIMATION_FRAME_OFFSET),
                PlayerIndex      = data[PLAYER_INDEX_OFFSET],
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
                InvulnCounter        = data[PLAYER_EXTRA_INVULN_COUNTER_OFFSET],
                RCTime               = data[PLAYER_EXTRA_RC_TIME_OFFSET],
                JamParryTime         = data[PLAYER_EXTRA_JAM_PARRY_TIME_OFFSET],
                ComboTime            = BitConverter.ToInt16(data, PLAYER_EXTRA_COMBO_TIME_OFFSET),
                SBTime               = data[PLAYER_EXTRA_SLASH_BACK_TIME_OFFSET]
            };
        }

        private static Hitbox GetPushBox(ushort charId, ActionStateFlags status, ushort actId, int yPos, Hitbox[] boxSet)
        {
            int yOffset = 0;
            short[] widthArr;
            short[] heightArr;
            if (status.IsCrouching)
            {
                widthArr = PushBoxCrouchingWidths;
                heightArr = PushBoxCrouchingHeights;
            }
            else if (status.IsPushboxType1)
            {
                // Not really sure what state this is. Adapting the draw logic from another project.
                widthArr = PushBoxAirWidths;
                heightArr = PushBoxStandingHeights;
            }
            else if (status.IsAirborne)
            {
                widthArr = PushBoxAirWidths;
                heightArr = PushBoxAirHeights;
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
            else    // IsStanding
            {
                widthArr = PushBoxStandingWidths;
                heightArr = PushBoxStandingHeights;
            }

            short w = widthArr[charId];
            short h = heightArr[charId];

            var pushBoxOverrides = boxSet.Where(b => b.BoxTypeId == BoxId.PUSH);

            if (pushBoxOverrides.Any())
            {
                return new Hitbox()
                {
                    XOffset = pushBoxOverrides.First().XOffset,
                    YOffset = (short)((h + yOffset) / -100),
                    Width   = pushBoxOverrides.First().Width,
                    Height  = (short)(h / 100)
                };
            }
            else if (charId == (int)CharacterID.BRIDGET && actId == BRIDGET_SHOOT_ACT_ID)
            {
                return new Hitbox()
                {
                    XOffset = (short)(w / -100),
                    YOffset = (short)((h + yOffset + BRIDGET_SHOOT_PUSHBOX_ADJUST) / -100),
                    Width   = (short)(w / 100 * 2),
                    Height  = (short)((h + BRIDGET_SHOOT_PUSHBOX_ADJUST) / 100)
                };
            }
            else
            {
                return new Hitbox()
                {
                    XOffset = (short)(w / -100),
                    YOffset = (short)((h + yOffset) / -100),
                    Width = (short)(w / 100 * 2),
                    Height = (short)(h / 100)
                };
            }
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
                XOffset   = BitConverter.ToInt16(data, offset),
                YOffset   = BitConverter.ToInt16(data, offset + 0x02),
                Width     = BitConverter.ToInt16(data, offset + 0x04),
                Height    = BitConverter.ToInt16(data, offset + 0x06),
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
                PlayerIndex     = data[offset + PLAYER_INDEX_OFFSET],
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
