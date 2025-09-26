using System.Buffers;
using System.Numerics;
using System.Text;
using Vortice.Mathematics;

namespace GGXXACPROverlay.GGXXACPR
{
    /// <summary>
    /// Exposes game data. Handles pointer dereferencing as well as
    /// some slightly higher level functions for visualization.
    /// </summary>
    public static unsafe class GGXXACPR
    {
        public const int SCREEN_HEIGHT_PIXELS = 480;
        public const int SCREEN_WIDTH_PIXELS = 640;
        public const int SCREEN_GROUND_PIXEL_OFFSET = 40;
        public const int NUMBER_OF_CHARACTERS = 25;
        public const float MAX_DISPLAY_HEIGHT = 100000;

        #region HSD
        // See HSD function at GGXXACPR_Win.exe+132570
        public static readonly int[] HSD_BREAK_POINTS = [0, 180, 300, 420, 600, 860];
        public static readonly float[] HSD_PENALTIES = [0.0f, 0.95f, 0.9f, 0.8f, 0.7f, 0.6f];
        #endregion

        #region Special case constants
        // Exception to the ActionStatusFlags.IsPlayer1/2 flag. Dizzy bubble is flagged as the opponent's entity while attackable by Dizzy.
        // Makes that flag more of a "Is attackable by" thing. For some reason, Venom balls aren't implemented this way.
        public const int DIZZY_ENTITY_ID = 0x43;
        // The following moves use Player.Mark to denote parry active frames instead of the parry flag
        public const int AXL_TENHOU_SEKI_UPPER_ACT_ID = 188;
        public const int AXL_TENHOU_SEKI_LOWER_ACT_ID = 189;
        public const int DIZZY_EX_NECRO_UNLEASHED_ACT_ID = 247;
        /// <summary>
        /// This move has a special pushbox adjustment (see GGXXACPR_Win.exe+14BDE9 ~ +14BE0D)
        /// </summary>
        public const int BRIDGET_SHOOT_ACT_ID = 134;
        public const int BRIDGET_SHOOT_PUSHBOX_ADJUST = 7000;
        // Slide head uses a grounded status check for hit detection in addition to a special hitbox
        //  The frame meter will check for this act id to see if it should mark the frame as an active frame
        public const int SLIDE_HEAD_ACT_ID = 181;
        public const uint SLIDE_HEAD_UNBLOCKABLE_ACT_HEADER_VALUE = 0x10102015;
        // For whatever reason, this throw range is hardcoded and not in the array with everything else
        public const int SPECIAL_CASE_COMMAND_THROW_ID = 0x19;
        public const int SPECIAL_CASE_COMMAND_THROW_RANGE = 11000; // GGXXACPR_Win.exe+12054F
        // 170 unit offset hardcoded into CL function (see GGXXACPR_Win.exe+132129)
        public const int CLEAN_HIT_Y_OFFSET = 170;
        #endregion

        #region Range Checks
        public const int ROBO_KY_MAT_ID = 0x62; // Lot of overlap on entity IDs
        public const int ROBO_KY_MAT_COLLISION_RANGE = 13200;   // GGXXACPR_Win.exe+382AE7

        public const int TESTAMENT_ENTITY_ID = 0x50;
        public const int TESTAMENT_HITOMI_ACTIVATION_RANGE_AC = 7000;   // GGXXACPR_Win.exe+246BA8
        public const int TESTAMENT_HITOMI_ACTIVATION_RANGE_PR = 5500;   // GGXXACPR_Win.exe+246BA1

        // Checked against PUSHBOX_EDGE_DISTANCE so visual is pushbox + 10000 unit halfwidth
        public const int FAUST_HACK_N_SLASH_RANGE = 10000;  // GGXXACPR_Win.exe+292DFF
        public const int FAUST_HACK_N_SLASH_FAIL_ACT_ID = 190;
        public const int FAUST_HACK_N_SLASH_UNBLOCKABLE_ACT_ID = 122;

        public const int FAUST_ENTITY_ID = 0x2B;
        public const int FAUST_DONUT_PICKUP_ACT_ID = 18;
        public const int FAUST_CHOCOLATE_PICKUP_ACT_ID = 19;
        public const int FAUST_CHIKUWA_PICKUP_ACT_ID = 53;
        // GGXXACPR_Win.exe+28DE55 (Donut range)
        // GGXXACPR_Win.exe+28E2AD (Chocolate range)
        // GGXXACPR_Win.exe+28C571 (Chikuwa range)
        public const int FAUST_FOOD_PICKUP_RANGE = 4800;    // All 4800

        // Horizontal check is made by multiplying Eddie's Mark var by 100;
        // Horizontal check only made if Eddie's localid == 1 && trans == 0
        public const int EDDIE_ENTITY_ID = 0x23;
        public const int EDDIE_PUDDLE_VERTICAL_RANGE = 4000;   // GGXXACPR_Win.exe+22BF88

        // This range check is based on PUSHBOX_EDGE_DISTANCE when Mark != 0
        public const int POTEMKIN_SLIDE_HEAD_RANGE = 17000; // GGXXACPR_Win.exe+25BD41
        public const int POTEMKIN_SLIDE_HEAD_ACT_ID = 0x78;
        #endregion

        // DirectX
        // Dereferencing pointer at class init since pointer is not expected to change
        public static readonly nint Direct3D9DevicePointer = *(nint*)(Memory.BaseAddress + Offsets.DIRECT3D9_DEVICE);

        /// <summary>
        /// Dereferences and returns a snapshot of the player 1 struct. If null, will return a dummy struct with default values.
        /// </summary>
        public static Player Player1 => new(*_Player1);
        private static readonly BaseEntityRaw** _Player1 = (BaseEntityRaw**)(Memory.BaseAddress + Offsets.PLAYER_1_PTR);
        /// <summary>
        /// Dereferences and returns a snapshot of the player 2 struct. If null, will return a dummy struct with default values.
        /// </summary>
        public static Player Player2 => new(*_Player2);
        private static readonly BaseEntityRaw** _Player2 = (BaseEntityRaw**)(Memory.BaseAddress + Offsets.PLAYER_2_PTR);
        /// <summary>
        /// The fields Entity.Prev and Entity.Next form a circular doubly-linked list. This dummy entity is a root node for this list.
        /// </summary>
        public static Entity RootEntity => new(_RootEntity);
        private static readonly BaseEntityRaw* _RootEntity = (BaseEntityRaw*)(Memory.BaseAddress + Offsets.ENTITY_ARR_HEAD_TAIL_PTR);

        /// <summary>
        /// Dereferences and returns a snapshot of the camera struct.
        /// </summary>
        public static Camera Camera => *_camera;
        private static readonly Camera* _camera = (Camera*)(Memory.BaseAddress + Offsets.CAMERA);

        private static readonly int* _viewHeight = (int*)(Memory.BaseAddress + Offsets.VIEW_HEIGHT);
        private static readonly int* _viewWidth = (int*)(Memory.BaseAddress + Offsets.VIEW_WIDTH);

        /// <summary>
        /// Dereferences and returns a snapshot of the throw flags struct.
        /// </summary>
        public static ThrowDetection ThrowFlags => (ThrowDetection)(*_throwFlags);
        private static readonly byte* _throwFlags = (byte*)(Memory.BaseAddress + Offsets.GLOBAL_THROW_FLAGS);
        public static readonly int CommandThrowIDP1 = *(int*)(Memory.BaseAddress + Offsets.COMMAND_GRAB_ID_P1);
        public static readonly int CommandThrowIDP2 = *(int*)(Memory.BaseAddress + Offsets.COMMAND_GRAB_ID_P2);
        /// <summary>
        /// Index with CommandThrowIDP1 or CommandThrowIDP2
        /// </summary>
        public static readonly short* CommandThrowRangeArr = (short*)(Memory.BaseAddress + Offsets.COMMAND_GRAB_RANGE_LOOKUP_TABLE);
        public static GameVersion GameVersion => Enum.IsDefined(typeof(GameVersion), *_gameVersion) ?
            (GameVersion)(*_gameVersion) : GameVersion.PLUS_R;
        private static readonly int* _gameVersion = (int*)(Memory.BaseAddress + Offsets.GAME_VER_FLAG);
        public static bool IsPlusR => GameVersion == GameVersion.PLUS_R;
        public static bool IsAccentCore => GameVersion == GameVersion.AC;
        public static bool IsInGame => *_inGameFlag != 0;
        public static readonly byte* _inGameFlag = (byte*)(Memory.BaseAddress + Offsets.IN_GAME_FLAG);
        /// <summary>
        /// The current game mode. This variable is updated when selecting a mode from the main menu.
        /// When backing out to the main menu, the previous game mode value will be retained until selecting a new game mode. Defaults to Arcade mode on boot.
        /// </summary>
        public static GameMode GameMode => *(GameMode*)(Memory.BaseAddress + Offsets.GAME_MODE);
        public static bool ShouldRender => *_inGameFlag != 0 && !(*TrainingPauseDisplay == 1 && *TrainingPauseState != 0);
        public static bool ShouldUpdate => *_inGameFlag != 0 && *TrainingPauseState == 0;
        /// <summary>
        /// 0 = Unpaused, 1 = Pause transiiton, 2 = Paused
        /// </summary>
        public static readonly int* TrainingPauseState = (int*)(Memory.BaseAddress + Offsets.TRAINING_MODE_PAUSE_STATE);
        /// <summary>
        /// 0 = Hide pause menu, 1 = Show pause menu
        /// </summary>
        public static readonly int* TrainingPauseDisplay = (int*)(Memory.BaseAddress + Offsets.TRAINING_MODE_PAUSE_DISPLAY);
        public static int ReplaySimState => *(int*)(Memory.BaseAddress + Offsets.GLOBAL_REPLAY_SIMULATE);
        public static int ReplayFrameCount => *(int*)(Memory.BaseAddress + Offsets.REPLAY_FRAME_COUNT);
        public static readonly int* BackgroundState = (int*)(Memory.BaseAddress + Offsets.BACKGROUND_STATE);

        private static readonly delegate* unmanaged[Cdecl]<int, int, float, byte, float, int> _RenderText =
            (delegate* unmanaged[Cdecl]<int, int, float, byte, float, int>)(Memory.BaseAddress + Offsets.RENDER_TEXT);

        /// <summary>
        /// Wrapper method that attempts to invoke native GGXXACPR function.
        /// </summary>
        public static unsafe int RenderText(string text, int xPos, int yPos, byte alpha)
        {
            var utf8Bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(text.ToUpper()));
            fixed (byte* pText = &utf8Bytes[0])
            {
                Hooks.Util.CustomCallingConventionParameters(0, (uint)pText, 0);
                return _RenderText(xPos, yPos, 365f, alpha, 1f);
            }
        }

        public static Player GetOpponent(Player p)
        {
            if (!p.IsValid) return new();

            return p.PlayerIndex == 0 ? Player2 : Player1;
        }

        //private static readonly Hitbox[] _hitboxBuffer = new Hitbox[100];
        public static RentedArraySlice<Hitbox> GetHitboxes(BoxId type, Player p)
            => GetHitboxes([type, BoxId.USE_EXTRA], p);
        public static RentedArraySlice<Hitbox>GetHitboxes(BoxId type, Entity e)
            => GetHitboxes([type, BoxId.USE_EXTRA], e);
        public static RentedArraySlice<Hitbox> GetHitboxes(BoxId[] types, Player p)
            => GetHitboxes(types, p.NativePointer);
        public static RentedArraySlice<Hitbox> GetHitboxes(BoxId[] types, Entity e)
            => GetHitboxes(types, e.NativePointer);
        private static RentedArraySlice<Hitbox> GetHitboxes(BoxId[] types, BaseEntityRaw* e)
        {
            if (e is null || e->HitboxSet is null) return new([]);

            Hitbox[] temp = ArrayPool<Hitbox>.Shared.Rent(128);
            PlayerExtra pExtra = e->Extra is null ? default : *e->Extra;

            int bufferIndex = 0;
            for (int i = 0; i < e->BoxCount; i++)
            {
                if (types.Contains((BoxId)e->HitboxSet[i].BoxTypeId))
                {
                    // hurtbox discard checks
                    if (e->HitboxSet[i].BoxTypeId == (ushort)BoxId.HURT && (
                        (e->Status & (uint)ActionState.DisableHurtboxes) > 0 ||
                        (e->Status & (uint)ActionState.StrikeInvuln) > 0 ||
                        pExtra.InvulnCounter > 0))
                        continue;

                    // hitbox discard checks
                    if (e->HitboxSet[i].BoxTypeId == (ushort)BoxId.HIT &&
                        // Discard if disabled hitboxes is flagged and not ignoring that flag in settings
                        ((e->Status & (uint)ActionState.DisableHitboxes) > 0 && !Settings.Misc.IgnoreDisableHitboxFlag) &&
                        // but only if not in hitstop after an attack has connected
                        !(e->HitstopCounter > 0 && (e->AttackFlags & (uint)AttackState.HasConnected) > 0))
                        continue;


                    if (e->HitboxSet[i].BoxTypeId == (ushort)BoxId.USE_EXTRA)
                    {
                        if (e->HitboxExtraSet is not null && types.Contains((BoxId)e->HitboxExtraSet[i].BoxTypeId))
                            temp[bufferIndex++] = e->HitboxExtraSet[i];
                    }
                    else
                    {
                        temp[bufferIndex++] = e->HitboxSet[i];
                    }
                }
            }

            // Special case for Slide head's unblockable hitbox. Not sure the exact checks for this,
            // but it appears in the first slot of the extra hitbox set and is active when the act header flags are in a certain state.
            if (e->Id == (int)CharacterID.POTEMKIN && e->ActionId == SLIDE_HEAD_ACT_ID &&
                e->ActionHeaderFlags == SLIDE_HEAD_UNBLOCKABLE_ACT_HEADER_VALUE && e->HitboxExtraSet is not null)
            {
                temp[bufferIndex++] = e->HitboxExtraSet[0];
            }

            return new(temp, 0, bufferIndex);
        }

        public static Rect GetCLRect(Player p)
        {
            if (p.HitParam.CLScale == -1) return default;

            Player opponent = p.PlayerIndex == 0 ? Player2 : Player1;
            if (!opponent.IsValid) return default;

            byte CLCounter = opponent.Extra.CleanHitCounter;

            // If player has just hit a clean hit, draw the CLRect as it was before it shrunk.
            if (IsInCLHitstop(p)) CLCounter--;

            HitParam hp = p.HitParam;

            float halfWidth = hp.CLBaseWidth - (hp.CLScale * CLCounter);
            float halfHeight = hp.CLBaseHeight - (hp.CLScale * CLCounter);
            halfWidth = halfWidth >= 1.0f ? halfWidth : 1.0f;
            halfHeight = halfHeight >= 1.0f ? halfHeight : 1.0f;

            return new Rect(
                hp.CLCenterX - halfWidth,
                hp.CLCenterY - halfHeight + CLEAN_HIT_Y_OFFSET,
                halfWidth * 2,
                halfHeight * 2
            );
        }


        // TODO: remove stateful workaround. Anything stateful will not work in replay mode or online
        private static readonly bool[] _clMemory = [false, false];
        /// <summary>
        /// I can't find a good way to detect clean hit hitstun, so I'm just going to look at player hitstop.
        /// There is a flag set in `Player.Extra.HitstunFlags` for CL but it is cleared by the time the graphics hook runs.
        /// </summary>
        /// <param name="p">Target player</param>
        private static bool IsInCLHitstop(Player p)
        {
            // TODO: find a better method. Worst case, hook CL detection function to save result.
            if (p.HitstopCounter > 30) return _clMemory[p.PlayerIndex] = true;
            if (p.HitstopCounter == 0) return _clMemory[p.PlayerIndex] = false;

            return _clMemory[p.PlayerIndex];
        }

        #region Throw Boxes
        private static readonly short* AirThrowRangesPR    = (short*)(Memory.BaseAddress + Offsets.PLUSR_AIR_THROW_HORIZONTAL_RANGE_ARRAY);
        private static readonly short* AirThrowRangesAC    = (short*)(Memory.BaseAddress + Offsets.AC_AIR_THROW_HORIZONTAL_RANGE_ARRAY);
        private static readonly short* AirThrowRangesUpper = (short*)(Memory.BaseAddress + Offsets.AIR_THROW_UPPER_RANGE_ARRAY);
        private static readonly short* AirThrowRangesLower = (short*)(Memory.BaseAddress + Offsets.AIR_THROW_LOWER_RANGE_ARRAY);
        private static readonly short* GroundThrowRangesPR = (short*)(Memory.BaseAddress + Offsets.PLUSR_GROUND_THROW_RANGE_ARRAY);
        private static readonly short* GroundThrowRangesAC = (short*)(Memory.BaseAddress + Offsets.AC_GROUND_THROW_RANGE_ARRAY);

        public static bool GetCommandGrabBox(Player p, Rect pushbox, out Rect rect)
        {
            rect = default;
            if (!IsCommandThrowActive(p)) return false;

            int cmdThrowID = MoveData.GetCommandGrabId(p.CharId, p.ActionId);
            int cmdThrowRange = CommandThrowRangeArr[cmdThrowID];

            // Hard-coded override for A.B.A. Keygrab. See GGXXACPR_Win.exe+12054A
            if (cmdThrowID == SPECIAL_CASE_COMMAND_THROW_ID)
            {
                cmdThrowRange = SPECIAL_CASE_COMMAND_THROW_RANGE;
            }

            rect = new Rect(
                pushbox.X - cmdThrowRange,
                pushbox.Y,
                pushbox.Width + cmdThrowRange * 2,
                pushbox.Height
            );

            return true;
        }
        public static Rect GetGrabBox(Player p) => GetGrabBox(p, GetPushBox(p));
        public static Rect GetGrabBox(Player p, Rect pushbox)
        {
            float x, y, width, height;
            if (p.Status.HasFlag(ActionState.IsAirborne))
            {
                short* rangeArray = IsPlusR ? AirThrowRangesPR : AirThrowRangesAC;
                short range = rangeArray[(int)p.CharId];

                short upperBound = AirThrowRangesUpper[(int)p.CharId];
                short lowerBound = AirThrowRangesLower[(int)p.CharId];

                x = pushbox.X - range;
                y = pushbox.Y + upperBound + pushbox.Height;
                width = pushbox.Width + range * 2;
                height = lowerBound - upperBound;
            }
            else
            {
                short* rangeArray = IsPlusR ? GroundThrowRangesPR : GroundThrowRangesAC;

                short range = rangeArray[(int)p.CharId];

                x = pushbox.X - range;
                y = pushbox.Y;
                width = pushbox.Width + range * 2;
                height = pushbox.Height;
            }

            return new Rect(x, y, width, height);
        }

        public static bool IsThrowActive(Player p)
        {
            Player opponent = p.PlayerIndex == 0 ? Player2 : Player1;
            if (!opponent.IsValid) return false;

            return !p.CommandFlags.HasFlag(CommandState.DisableThrow) &&
                (ThrowFlags.HasFlag(ThrowDetection.Player1ThrowSuccess) && p.PlayerIndex == 0 ||
                ThrowFlags.HasFlag(ThrowDetection.Player2ThrowSuccess) && p.PlayerIndex == 1) &&
                opponent.Status.HasFlag(ActionState.IsInHitstun);
        }
        public static bool IsCommandThrowActive(Player p)
        {
            return p.Mark == 1 && MoveData.IsActiveByMark(p.CharId, p.ActionId);
        }
        #endregion

        #region Push Box
        private static readonly int* PushboxOffsetP1 = (int*)(Memory.BaseAddress + Offsets.PUSHBOX_P1_JUMP_OFFSET);
        private static readonly int* PushboxOffsetP2 = (int*)(Memory.BaseAddress + Offsets.PUSHBOX_P2_JUMP_OFFSET);
        private static readonly short* PushBoxAirWidthRanges       = (short*)(Memory.BaseAddress + Offsets.PUSHBOX_AIR_WIDTH_ARRAY);
        private static readonly short* PushBoxAirHeightRanges      = (short*)(Memory.BaseAddress + Offsets.PUSHBOX_AIR_HEIGHT_ARRAY);
        private static readonly short* PushBoxCrouchWidthRanges    = (short*)(Memory.BaseAddress + Offsets.PUSHBOX_CROUCHING_WIDTH_ARRAY);
        private static readonly short* PushBoxCrouchHeightRanges   = (short*)(Memory.BaseAddress + Offsets.PUSHBOX_CROUCHING_HEIGHT_ARRAY);
        private static readonly short* PushBoxStandingWidthRanges  = (short*)(Memory.BaseAddress + Offsets.PUSHBOX_STANDING_WIDTH_ARRAY);
        private static readonly short* PushBoxStandingHeightRanges = (short*)(Memory.BaseAddress + Offsets.PUSHBOX_STANDING_HEIGHT_ARRAY);
        /// <summary>
        /// Returns a Rectangle representing the given player's pushbox.
        /// </summary>
        /// <param name="p">A specified player struct</param>
        /// <returns>A Rectangle representing the pushbox</returns>
        public static Rect GetPushBox(Player p)
        {
            short charIndex = (short)p.CharId;
            int x = 0, y = 0, halfWidth = 0, height = 0;
            if (p.Status.HasFlag(ActionState.IsAirborne))
            {
                // Jump offset is given as positive/up (opposite of GGXXACPR world coordinates) and it is given as a final Y position.
                // To help reuse more of the hitbox render pipeline, we're converting the the jump offset to a player relative offset.
                if (p.PlayerIndex == 0)
                {
                    y = *PushboxOffsetP1 * -1 - p.YPos;
                }
                else if (p.PlayerIndex == 1)
                {
                    y = *PushboxOffsetP2 * -1 - p.YPos;
                }

                halfWidth = PushBoxAirWidthRanges[charIndex];
                height = PushBoxAirHeightRanges[charIndex];
            }
            else if (p.Status.HasFlag(ActionState.IsCrouching))
            {
                halfWidth = PushBoxCrouchWidthRanges[charIndex];
                height = PushBoxCrouchHeightRanges[charIndex];
            }
            else if (p.Status.HasFlag(ActionState.Wakeup))
            {
                halfWidth = PushBoxAirWidthRanges[charIndex];
                height = PushBoxStandingHeightRanges[charIndex];
            }
            else // standing
            {
                halfWidth = PushBoxStandingWidthRanges[charIndex];
                height = PushBoxStandingHeightRanges[charIndex];
            }

            if (FindFirstPushBoxAdjustment(p, out Hitbox pushAdjust))
            {
                x = pushAdjust.XOffset * 100;
                halfWidth = pushAdjust.Width * 100 / 2;
            }
            else
            {
                x = -halfWidth;
            }

            // see BRIDGET_SHOOT_ACT_ID comment
            if (p.CharId == CharacterID.BRIDGET && p.ActionId == BRIDGET_SHOOT_ACT_ID)
            {
                // Game makes Y adjustment here too, but that's already handled by checking the PushboxOffsetP1 / P2 address
                height += BRIDGET_SHOOT_PUSHBOX_ADJUST;
            }

            return new Rect(
                x,
                y - height,
                halfWidth * 2,
                height
            );
        }

        /// <summary>
        /// Finds the first pushbox adjustment box (box type 3) in the given player's current box set.
        /// </summary>
        /// <param name="p">The player struct</param>
        /// <param name="pushAdjust">The pushbox adjust struct. Default struct if none found.</param>
        /// <returns>Whether a pushadjust box was found</returns>
        private static bool FindFirstPushBoxAdjustment(Player p, out Hitbox pushAdjust)
        {
            pushAdjust = default;
            Span<Hitbox> hitboxes = p.HitboxSet;
            if (hitboxes.Length == 0) return false;

            for (int i = 0; i < p.BoxCount; i++)
            {
                if (hitboxes[i].BoxTypeId == (ushort)BoxId.PUSH)
                {
                    pushAdjust = hitboxes[i];
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Range Checks
        public static bool GetProximityBox(Player p, out Rect box)
        {
            box = default;
            if (!p.IsValid) return false;

            if (p.CharId == CharacterID.POTEMKIN &&
                p.ActionId == POTEMKIN_SLIDE_HEAD_ACT_ID &&
                p.Mark != 0)
            {
                var push = GetPushBox(p);
                box = new(push.X - POTEMKIN_SLIDE_HEAD_RANGE, push.Y, push.Width + POTEMKIN_SLIDE_HEAD_RANGE * 2, push.Height);
                return true;
            }
            else if (p.CharId == CharacterID.FAUST &&
                (p.ActionId == FAUST_HACK_N_SLASH_FAIL_ACT_ID || p.ActionId == FAUST_HACK_N_SLASH_UNBLOCKABLE_ACT_ID) &&
                p.AnimationCounter == 1)
            {
                var push = GetPushBox(p);
                box = new(push.X - FAUST_HACK_N_SLASH_RANGE, push.Y, push.Width + FAUST_HACK_N_SLASH_RANGE * 2, push.Height);
                return true;
            }

            return false;
        }
        public static bool GetProximityBox(Entity e, out Rect box)
        {
            box = default;
            if (!e.IsValid) return false;

            var halfHeight = Settings.Hitboxes.HitboxBorderThickness * 150.0f;

            if (e.Id == ROBO_KY_MAT_ID)
            {
                box = new(-ROBO_KY_MAT_COLLISION_RANGE, -halfHeight, ROBO_KY_MAT_COLLISION_RANGE * 2, halfHeight * 2);
                return true;
            }
            else if (e.Id == TESTAMENT_ENTITY_ID && e.ActionId == 10)
            {
                var range = IsPlusR ? TESTAMENT_HITOMI_ACTIVATION_RANGE_PR : TESTAMENT_HITOMI_ACTIVATION_RANGE_AC;

                if (Settings.Hitboxes.DrawInfiniteHeight)
                    box = new(-range, -MAX_DISPLAY_HEIGHT, range * 2, MAX_DISPLAY_HEIGHT * 2);
                else
                    box = new(-range, -halfHeight, range * 2, halfHeight * 2);

                return true;
            }
            else if (e.Id == FAUST_ENTITY_ID && 
                (e.ActionId is FAUST_DONUT_PICKUP_ACT_ID or FAUST_CHOCOLATE_PICKUP_ACT_ID or FAUST_CHIKUWA_PICKUP_ACT_ID))
            {
                box = new(-FAUST_FOOD_PICKUP_RANGE, -halfHeight, FAUST_FOOD_PICKUP_RANGE * 2, halfHeight * 2);
                return true;
            }
            else if (e.Id == EDDIE_ENTITY_ID && e.LocalId == 1 && e.Transition == 0)
            {
                box = new(-e.Mark * 100, -EDDIE_PUDDLE_VERTICAL_RANGE, e.Mark * 200, EDDIE_PUDDLE_VERTICAL_RANGE * 2);
                return true;
            }

            return false;
        }
        #endregion

        /// <summary>
        /// Helper logic for FrameMeter. Returns whether the given player has any active hitboxes.
        /// </summary>
        public static bool HasActiveFrame(Player p)
        {
            Span<Hitbox> hitboxes = p.HitboxSet;
            Span<Hitbox> hitboxesExtra = p.HitboxExtraSet;

            if (hitboxes.Length == 0 || p.Status.HasFlag(ActionState.DisableHitboxes))
                return false;

            for (int i = 0; i < hitboxes.Length; i++)
            {
                if (hitboxes[i].BoxTypeId == (ushort)BoxId.HIT ||
                        (hitboxes[i].BoxTypeId == (ushort)BoxId.USE_EXTRA &&
                        hitboxesExtra.Length > i &&
                        hitboxesExtra[i].BoxTypeId == (ushort)BoxId.HIT))
                {
                    return true;
                }
            }

            return MoveData.IsActiveByMark(p.CharId, p.ActionId) && p.Mark == 1;
        }

        /// <summary>
        /// Helper logic for FrameMeter. Returns true if the given player has any hurt boxes.
        /// </summary>
        public static bool HasAnyHurtboxes(Player p)
        {
            Span<Hitbox> hitboxes = p.HitboxSet;
            for (int i = 0; i < hitboxes.Length; i++)
            {
                if (hitboxes[i].BoxTypeId == (ushort)BoxId.HURT) return true;
            }
            return false;
        }

        public static bool HasAnyProjectileHitbox(byte playerIndex)
        {
            return AnyEntities((e) =>
            {
                if (e.PlayerIndex != playerIndex) return false;

                foreach (Hitbox hb in e.HitboxSet)
                {
                    if (hb.BoxTypeId == (short)BoxId.HIT) return true;
                }

                return false;
            });
        }
        public static bool HasAnyProjectileHurtbox(byte playerIndex)
        {
            return AnyEntities((e) =>
            {
                if (e.PlayerIndex != playerIndex) return false;

                foreach (Hitbox hb in e.HitboxSet)
                {
                    if (hb.BoxTypeId == (ushort)BoxId.HURT) return true;
                }

                return false;
            });
        }

        public static bool AnyEntities(Func<Entity, bool> predicate)
        {
            Entity Root = RootEntity;
            if (!Root.IsValid) return false;

            Entity iEntity = Root.Next;

            while (!iEntity.Equals(Root))
            {
                if (!iEntity.IsValid) return false;

                if (predicate.Invoke(iEntity)) return true;

                iEntity = iEntity.Next;
            }

            return false;
        }

        #region Rendering Helpers
        /// <summary>
        /// Returns a matrix transform for aligning hitbox model coordinates to a player in world coordinates.
        /// </summary>

        public static Matrix4x4 GetModelTransform(Entity e) => GetModelTransform(e.NativePointer);
        public static Matrix4x4 GetModelTransform(Player p) => GetModelTransform(p.NativePointer);
        private static Matrix4x4 GetModelTransform(BaseEntityRaw* e)
        {
            Matrix4x4 matrix = Matrix4x4.Identity;

            // Scale hitbox coor to world coor
            matrix *= Matrix4x4.CreateScale(100f, 100f, 1f);

            // Flip if facing right
            if (e->IsFacingRight == 1)
            {
                matrix *= Matrix4x4.CreateScale(-1f, 1f, 1f);
            }

            // Player scale var (given as a short * 1000)
            if (e->ScaleX > 0 || e->ScaleY > 0)
            {
                // If either scale value is -1, it should copy the value of the other
                float scaleY = (e->ScaleY < 0 ? e->ScaleX : e->ScaleY) / 1000f;
                float scaleX = (e->ScaleX < 0 ? e->ScaleY : e->ScaleX) / 1000f;
                matrix *= Matrix4x4.CreateScale(scaleX, scaleY, 1f);
            }

            // Player origin translation
            matrix *= Matrix4x4.CreateTranslation(e->XPos, e->YPos, 0f);

            return matrix;
        }

        /// <summary>
        /// Returns a matrix transform for aligning pushboxes and grabboxes to player positions.
        /// </summary>
        public static Matrix4x4 GetPlayerTransform(Player p)
        {
            Matrix4x4 matrix = Matrix4x4.Identity;

            // Flip if facing right
            if (p.IsFacingRight == 1)
            {
                matrix *= Matrix4x4.CreateScale(-1f, 1f, 1f);
            }

            // Player origin translation
            matrix *= Matrix4x4.CreateTranslation(p.XPos, p.YPos, 0f);

            return matrix;
        }

        /// <summary>
        /// Returns a matrix transform for aligning range boxes to entity coordinates.
        /// </summary>
        /// <param name="e"></param>
        public static Matrix4x4 GetEntityTransform(Entity e)
        {
            Matrix4x4 matrix = Matrix4x4.Identity;

            // WARNING: Flip not currently necessary here, but may be if functionality is extended

            matrix *= Matrix4x4.CreateTranslation(e.XPos, e.YPos, 0f);

            return matrix;
        }

        public static Matrix4x4 GetProjectionTransform() => GetProjectionTransform(Camera);
        public static Matrix4x4 GetProjectionTransform(Camera c)
        {
            Matrix4x4 matrix = Matrix4x4.Identity;

            // Translation correction
            matrix *= Matrix4x4.CreateTranslation(new Vector3(-50.0f, -50.0f, 0.0f));

            // Projection
            float cameraBottom = c.CameraHeight + (c.Height / 12f);
            matrix *= Matrix4x4.CreateOrthographicOffCenterLeftHanded(
                c.LeftEdge,
                c.LeftEdge + c.Width,
                cameraBottom,
                cameraBottom - c.Height,
                0,
                1
            );

            matrix *= GetWidescreenCorrectionTransform(c);

            return matrix;
        }
        public static Matrix4x4 GetViewPortProjectionTransform()
        {
            return Matrix4x4.CreateOrthographicOffCenterLeftHanded(
                0, *_viewWidth,
                *_viewHeight, 0,
                0, 1);
        }
        public static Matrix4x4 GetWidescreenCorrectionTransform() => GetWidescreenCorrectionTransform(Camera);
        public static Matrix4x4 GetWidescreenCorrectionTransform(Camera c)
        {
            return Matrix4x4.CreateScale(4 * (*_viewHeight) * 1.0f / (3 * (*_viewWidth)), 1.0f, 1.0f);
        }

        /// <summary>
        /// Used as the scissor rect for clipping
        /// </summary>
        public static Vortice.Direct3D9.Rect GetGameRegion()
        {
            int gameScreenHeight = *_viewHeight;
            int gameScreenWidth = gameScreenHeight * 4 / 3;
            int sideBarLength = (*_viewWidth - gameScreenWidth) / 2;

            return new (sideBarLength, 0, sideBarLength + gameScreenWidth, gameScreenHeight);
        }

        public static float WorldCoorPerGamePixel() => WorldCoorPerGamePixel(Camera);
        public static float WorldCoorPerGamePixel(Camera c)
        {
            return 100.0f / c.Zoom;
        }

        public static float WorldCoorPerViewPixel() => WorldCoorPerViewPixel(Camera);
        public static float WorldCoorPerViewPixel(Camera c)
        {
            return c.Height * 1.0f / (*_viewHeight);
        }
        #endregion
    }
}
