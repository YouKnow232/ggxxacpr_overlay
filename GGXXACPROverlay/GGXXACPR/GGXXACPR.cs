using System.Numerics;
using System.Text;
using Vortice.Mathematics;

namespace GGXXACPROverlay.GGXXACPR
{
    public static unsafe class GGXXACPR
    {
        public const int SCREEN_HEIGHT_PIXELS = 480;
        public const int SCREEN_WIDTH_PIXELS = 640;
        public const int SCREEN_GROUND_PIXEL_OFFSET = 40;
        public const int NUMBER_OF_CHARACTERS = 25;

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
        // Slide head uses a grounded status check for hit detection instead of a hitbox
        //  The frame meter will check for this act id to see if it should mark the frame as an active frame
        public const int SLIDE_HEAD_ACT_ID = 181;
        // For whatever reason, this throw range is hardcoded and not in the array with everything else
        public const int SPECIAL_CASE_COMMAND_THROW_ID = 0x19;
        public const int SPECIAL_CASE_COMMAND_THROW_RANGE = 11000; // GGXXACPR_Win.exe+12054F
        #endregion

        // DirectX
        // Dereferencing pointer at class init since pointer is not expected to change
        public static readonly nint Direct3D9DevicePointer = *(nint*)(Memory.BaseAddress + Offsets.DIRECT3D9_DEVICE_OFFSET);
        public static readonly nint GraphicsHookBreakPointAddress = Memory.BaseAddress + Offsets.GRAPHICS_HOOK_BREAKPOINT_OFFSET;

        private const int COMMAND_GRAB_RANGE_LOOKUP_TABLE_SIZE = 27;

        #region Struct Field Offsets
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
        #endregion

        // Buffer sizes
        private const int ENTITY_STRUCT_BUFFER = 0x130;
        private const int PLAYER_EXTRA_STRUCT_BUFFER = 0x148;
        private const int HITBOX_ARRAY_STEP = 0x0C;


        /// <summary>
        /// Dereferences and returns a snapshot of the player 1 struct. If null, will return a dummy struct with default values.
        /// </summary>
        public static Player Player1 => *_Player1 is null ? default : **_Player1;
        private static readonly Player** _Player1 = (Player**)(Memory.BaseAddress + Offsets.PLAYER_1_PTR_ADDR);
        /// <summary>
        /// Dereferences and returns a snapshot of the player 2 struct. If null, will return a dummy struct with default values.
        /// </summary>
        public static Player Player2 => *_Player2 is null ? default : **_Player2;
        private static readonly Player** _Player2 = (Player**)(Memory.BaseAddress + Offsets.PLAYER_2_PTR_ADDR);
        public static readonly Entity* EntityList = (Entity*)*_Player1;
        /// <summary>
        /// Dereferences and returns a snapshot of the camera struct.
        /// </summary>
        public static Camera Camera => *_camera;
        private static readonly Camera* _camera = (Camera*)(Memory.BaseAddress + Offsets.CAMERA_ADDR);

        public static readonly ThrowDetection ThrowFlags = *(ThrowDetection*)(Memory.BaseAddress + Offsets.GLOBAL_THROW_FLAGS_ADDR);
        public static readonly int CommandThrowIDP1 = *(int*)(Memory.BaseAddress + Offsets.COMMAND_GRAB_ID_ADDR);
        public static readonly int CommandThrowIDP2 = *(int*)(Memory.BaseAddress + Offsets.COMMAND_GRAB_ID_ADDR + sizeof(int));
        /// <summary>
        /// Index with CommandThrowIDP1 or CommandThrowIDP2
        /// </summary>
        public static readonly int* CommandThrowRangeArr = (int*)(Memory.BaseAddress + Offsets.COMMAND_GRAB_RANGE_LOOKUP_TABLE);
        /// <summary>
        /// 0=AC, 1=+R
        /// </summary>
        public static readonly GameVersion* GameVersion = (GameVersion*)(Memory.BaseAddress + Offsets.GAME_VER_FLAG);
        public static readonly byte* InGameFlag = (byte*)(Memory.BaseAddress + Offsets.IN_GAME_FLAG);
        public static readonly int* PauseState = (int*)(Memory.BaseAddress + Offsets.GLOBAL_PAUSE_VAR_ADDR);
        public static readonly int* ReplaySimState = (int*)(Memory.BaseAddress + Offsets.GLOBAL_REPLAY_SIMULATE_ADDR);


        private static readonly delegate* unmanaged[Cdecl]<int, int, float, byte, float, int> _RenderText =
            (delegate* unmanaged[Cdecl]<int, int, float, byte, float, int>)(Memory.BaseAddress + Offsets.RENDER_TEXT_OFFSET);

        public static unsafe int RenderText(string text, int xPos, int yPos, byte alpha)
        {
            var utf8Bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(text.ToUpper()));
            fixed (byte* pText = &utf8Bytes[0])
            {
                Hooks.CustomCallingConventionParameters(0, (uint)pText, 0);
                return _RenderText(xPos, yPos, 365f, alpha, 1f);
            }
        }

        private static readonly Hitbox[] _hitboxBuffer = new Hitbox[100];
        public static Span<Hitbox> GetHitboxes(BoxId[] types, Player p)
        {
            if (p.HitboxSet is null || p.Extra is null) return [];

            var pExtra = *p.Extra;

            int hbBuffIndex = 0;
            for (int i = 0; i < p.BoxCount; i++)
            {
                if (types.Contains(p.HitboxSet[i].BoxTypeId))
                {
                    // hurtbox discard checks
                    if (p.HitboxSet[i].BoxTypeId == BoxId.HURT && (
                        p.Status.HasFlag(ActionState.DisableHurtboxes) ||
                        p.Status.HasFlag(ActionState.StrikeInvuln) ||
                        pExtra.InvulnCounter > 0))
                        continue;

                    // hitbox discard checks
                    if (p.HitboxSet[i].BoxTypeId == BoxId.HIT &&
                        p.Status.HasFlag(ActionState.DisableHitboxes) &&
                        !(p.HitstopCounter > 0))
                        continue;


                    if (p.HitboxSet[i].BoxTypeId == BoxId.USE_EXTRA)
                    {
                        if (p.HitboxExtraSet is not null && types.Contains(p.HitboxExtraSet[i].BoxTypeId))
                            _hitboxBuffer[hbBuffIndex++] = p.HitboxExtraSet[i];
                    }
                    else
                    {
                        _hitboxBuffer[hbBuffIndex++] = p.HitboxSet[i];
                    }
                }
            }

            return _hitboxBuffer.AsSpan(0, hbBuffIndex);
        }

        public static bool ShouldRender()
        {
            return *InGameFlag != 0;
        }

        public static Rect GetThrowBox(Player p) // Take in pushbox as arg?
        {
            throw new NotImplementedException();
        }

        public static Rect GetCLRect(Player p)
        {
            if (p.HitParam is null || p.HitParam->CLScale == -1) return default;

            byte CLCounter;
            if (p.PlayerIndex == 0 && Player2.Extra is not null)
            {
                CLCounter = Player2.Extra->CleanHitCounter;
            }
            else if (p.PlayerIndex == 1 && Player1.Extra is not null)
            {
                CLCounter = Player1.Extra->CleanHitCounter;
            }
            else { return default; }

            // If player has just hit a clean hit, draw the CLRect as it was before it shrunk.
            if (IsInCLHitstop(p)) CLCounter--;

            HitParam hp = *p.HitParam;

            float halfWidth = hp.CLBaseWidth  - (hp.CLScale * CLCounter);
            float halfHeight = hp.CLBaseHeight - (hp.CLScale * CLCounter);
            halfWidth = halfWidth >= 1.0f ? halfWidth : 1.0f;
            halfHeight = halfHeight >= 1.0f ? halfHeight : 1.0f;

            return new Rect(
                hp.CLCenterX - halfWidth,
                (hp.CLCenterY - halfHeight) + 170, // 170 unit offset hardcoded into CL function (GGXXACPR_Win.exe+132129)
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
        /// <returns></returns>
        private static bool IsInCLHitstop (Player p)
        {
            // TODO: find a better method. Worst case, hook CL detection function to save result.
            if (p.HitstopCounter > 30) _clMemory[p.PlayerIndex] = true;
            if (p.HitstopCounter == 0) _clMemory[p.PlayerIndex] = false;

            return _clMemory[p.PlayerIndex];
        }

        //public static Hitbox GetThrowBox(GameState state, Player p)
        //{
        //    if (p.Status.IsAirborne)
        //    {
        //        short horizontalRange;
        //        if (state.GlobalFlags.GameVersionFlag == GameVersion.PLUS_R)
        //        {
        //            horizontalRange = AirThrowRangesPlusR[(int)p.CharId];
        //        }
        //        else
        //        {
        //            horizontalRange = AirThrowRangesAC[(int)p.CharId];
        //        }
        //        short upperBound = AirThrowUpperBounds[(int)p.CharId];
        //        short lowerBound = AirThrowLowerBounds[(int)p.CharId];

        //        return new Hitbox()
        //        {
        //            XOffset = (short)(p.PushBox.XOffset - horizontalRange / 100),
        //            YOffset = (short)(p.PushBox.YOffset + p.PushBox.Height + upperBound / 100),
        //            Width = (short)(p.PushBox.Width + horizontalRange / 50),
        //            Height = (short)((lowerBound - upperBound) / 100),
        //        };
        //    }
        //    else
        //    {
        //        short range;
        //        if (state.GlobalFlags.GameVersionFlag == GameVersion.PLUS_R)
        //        {
        //            range = GroundThrowRangesPlusR[(int)p.CharId];
        //        }
        //        else
        //        {
        //            range = GroundThrowRangesAC[(int)p.CharId];
        //        }

        //        return new Hitbox()
        //        {
        //            //XOffset = (short)(p.PushBox.XOffset - range / 100),
        //            //YOffset = p.PushBox.YOffset,
        //            //Width   = (short)(p.PushBox.Width + range / 50),
        //            //Height  = p.PushBox.Height,
        //        };
        //    }
        //}

        /// <summary>
        /// Returns a Rectangle representing the given player's pushbox.
        /// </summary>
        /// <param name="p">A specified player struct</param>
        /// <returns>A Rectangle representing the pushbox</returns>
        private static Rect GetPushBox(Player p)
        {
            int x = 0, y = 0, halfWidth = 0, height = 0;
            if (p.Status.HasFlag(ActionState.IsAirborne))
            {
                if (p.PlayerIndex == 0)
                {
                    y = *(int*)(Memory.BaseAddress + Offsets.PUSHBOX_P1_JUMP_OFFSET_ADDRESS) + p.YPos;
                }
                else if (p.PlayerIndex == 1)
                {
                    y = *(int*)(Memory.BaseAddress + Offsets.PUSHBOX_P2_JUMP_OFFSET_ADDRESS) + p.YPos;
                }
            }
            else if (p.Status.HasFlag(ActionState.IsCrouching))
            {
                halfWidth = ((short*)(Memory.BaseAddress + Offsets.PUSHBOX_CROUCHING_WIDTH_ARRAY))[(short)p.CharId];
                height = ((short*)(Memory.BaseAddress + Offsets.PUSHBOX_CROUCHING_HEIGHT_ARRAY))[(short)p.CharId];
            }
            else if (p.Status.HasFlag(ActionState.IsPushboxType1))
            {
                halfWidth = ((short*)(Memory.BaseAddress + Offsets.PUSHBOX_AIR_WIDTH_ARRAY))[(short)p.CharId];
                height = ((short*)(Memory.BaseAddress + Offsets.PUSHBOX_STANDING_HEIGHT_ARRAY))[(short)p.CharId];
            }
            else // standing
            {
                halfWidth = ((short*)(Memory.BaseAddress + Offsets.PUSHBOX_STANDING_WIDTH_ARRAY))[(short)p.CharId];
                height = ((short*)(Memory.BaseAddress + Offsets.PUSHBOX_STANDING_HEIGHT_ARRAY))[(short)p.CharId];
            }
            
            if (FindFirstPushBoxAdjustment(p, out Hitbox pushAdjust))
            {
                x = pushAdjust.XOffset * 100;
                halfWidth = pushAdjust.Width * 100;
            }
            else
            {
                x = halfWidth;
            }

            if (p.CharId == CharacterID.BRIDGET && p.ActionId == BRIDGET_SHOOT_ACT_ID)
            {
                y += BRIDGET_SHOOT_PUSHBOX_ADJUST;
                height += BRIDGET_SHOOT_PUSHBOX_ADJUST;
            }

            return new Rect(
                -x,
                -(y + height),
                halfWidth * 2,
                height
            );
        }

        private static bool FindFirstPushBoxAdjustment(Player p, out Hitbox pushAdjust)
        {
            pushAdjust = default;
            if (p.HitboxSet is null) return false;

            Hitbox* pIndex = p.HitboxSet;
            for (int i = 0; i < p.BoxCount; i++)
            {
                if (pIndex[i].BoxTypeId == BoxId.PUSH)
                {
                    pushAdjust = pIndex[i];
                    return true;
                }
            }

            return false;
        }

        //private static Hitbox GetPushBox(ushort charId, ActionStateFlags status, ushort actId, int yPos, Hitbox[] boxSet)
        //{
        //    int yOffset = 0;
        //    short[] widthArr;
        //    short[] heightArr;
        //    if (status.IsAirborne)
        //    {
        //        widthArr = PushBoxAirWidths;
        //        heightArr = PushBoxAirHeights;
        //        // Special offsets for pushbox collision checks
        //        if (status.IsPlayer1)
        //        {
        //            yOffset = BitConverter.ToInt32(Memory.ReadMemoryPlusBaseOffset(PUSHBOX_P1_JUMP_OFFSET_ADDRESS, sizeof(int))) + yPos;
        //        }
        //        else if (status.IsPlayer2)
        //        {
        //            yOffset = BitConverter.ToInt32(Memory.ReadMemoryPlusBaseOffset(PUSHBOX_P2_JUMP_OFFSET_ADDRESS, sizeof(int))) + yPos;
        //        }
        //    }
        //    else if (status.IsCrouching)
        //    {
        //        widthArr = PushBoxCrouchingWidths;
        //        heightArr = PushBoxCrouchingHeights;
        //    }
        //    else if (status.IsPushboxType1)
        //    {
        //        // Not really sure what state this is. Adapting the draw logic from another project.
        //        widthArr = PushBoxAirWidths;
        //        heightArr = PushBoxStandingHeights;
        //    }
        //    else    // IsStanding
        //    {
        //        widthArr = PushBoxStandingWidths;
        //        heightArr = PushBoxStandingHeights;
        //    }

        //    short w = widthArr[charId];
        //    short h = heightArr[charId];

        //    var pushBoxOverrides = boxSet.Where(b => b.BoxTypeId == BoxId.PUSH);

        //    if (pushBoxOverrides.Any())
        //    {
        //        return new Hitbox()
        //        {
        //            XOffset = pushBoxOverrides.First().XOffset,
        //            YOffset = (short)((h + yOffset) / -100),
        //            Width   = pushBoxOverrides.First().Width,
        //            Height  = (short)(h / 100)
        //        };
        //    }
        //    else if (charId == (int)CharacterID.BRIDGET && actId == BRIDGET_SHOOT_ACT_ID)
        //    {
        //        return new Hitbox()
        //        {
        //            XOffset = (short)(w / -100),
        //            YOffset = (short)((h + yOffset + BRIDGET_SHOOT_PUSHBOX_ADJUST) / -100),
        //            Width   = (short)(w / 100 * 2),
        //            Height  = (short)((h + BRIDGET_SHOOT_PUSHBOX_ADJUST) / 100)
        //        };
        //    }
        //    else
        //    {
        //        return new Hitbox()
        //        {
        //            XOffset = (short)(w / -100),
        //            YOffset = (short)((h + yOffset) / -100),
        //            Width = (short)(w / 100 * 2),
        //            Height = (short)(h / 100)
        //        };
        //    }
        //}

        // TODO:
        //public readonly bool HasActiveFrame(Player p) =>
        //    p.HitboxSet.Where(hb => hb.BoxTypeId == BoxId.HIT).Any() && !p.Status.HasFlag(ActionStateE.DisableHitboxes) ||
        //    Mark == 1 && MoveData.IsActiveByMark(p.CharId, p.ActionId);

        /// <summary>
        /// Returns a matrix transform for aligning hitbox model coordinates to a player in world coordinates.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Matrix4x4 GetModelTransform(Player p)
        {
            Matrix4x4 matrix = Matrix4x4.Identity;

            // Scale hitbox coor to world coor
            matrix *= Matrix4x4.CreateScale(100f, 100f, 1f);

            // Flip if facing right
            if (p.IsFacingRight == 1)
            {
                matrix *= Matrix4x4.CreateScale(-1f, 1f, 1f);
            }

            // Player scale var (given as a short * 1000)
            if (p.ScaleX > 0 || p.ScaleY > 0)
            {
                // If either scale value is -1, it should copy the value of the other
                float scaleY = (p.ScaleY < 0 ? p.ScaleX : p.ScaleY) / 1000f;
                float scaleX = (p.ScaleX < 0 ? p.ScaleY : p.ScaleX) / 1000f;
                matrix *= Matrix4x4.CreateScale(scaleX, scaleY, 1f);
            }

            // Player origin translation
            matrix *= Matrix4x4.CreateTranslation(p.XPos, p.YPos, 0f);

            return matrix;
        }

        public static Matrix4x4 GetModelTransform(Entity e)
        {
            return GetModelTransform(*(Player*)&e);
        }

        public static Matrix4x4 GetProjectionTransform() => GetProjectionTransform(Camera);
        public static Matrix4x4 GetProjectionTransform(Camera c)
        {
            Matrix4x4 matrix = Matrix4x4.Identity;

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

            // Widescreen correction
            // TODO: find viewport info in game memory
            //matrix *= Matrix4x4.CreateScale((c.Width * _device.Viewport.Height) * 1.0f / (c.Height * _device.Viewport.Width), 1.0f, 1.0f);

            return matrix;
        }


        public static float WorldCoorPerGamePixel() => WorldCoorPerGamePixel(Camera);
        public static float WorldCoorPerGamePixel(Camera c)
        {
            return 100.0f / c.Zoom;
        }

        public static float WorldCoorPerViewPixel()
        {
            return 1.0f;
            // TODO: find viewport height in game memory
            //return c.Height / _device.Viewport.Height;
        }

    }
}
