using GGXXACPROverlay.GGXXACPR;

namespace GGXXACPROverlay.FrameMeter
{
    /// <summary>
    /// Represents a SF6-style frame chart
    /// </summary>
    internal class FrameMeter
    {
        private const int REPLAY_STATE_BUFFER_SIZE = 256;
        private const int DEFAULT_STATE_BUFFER_SIZE = 16;
        private const int MINIMUM_RUN_SUM = 8;
        private const int PAUSE_THRESHOLD = 10;

        public const int METER_LENGTH = 80;

        private readonly CircularBuffer<StateSnapShot> _stateBuffer;

        private int _index = 0;
        private bool _isPaused = true;
        private readonly int[] _frameNumberLookup = new int[METER_LENGTH];
        public readonly Meter[] PlayerMeters = new Meter[2];
        public readonly Meter[] EntityMeters = new Meter[2];

        private readonly bool _isReplay;
        private int _currentFrame = 0;  // TODO: refactor this to use local var frameId

        public FrameMeter()
        {
            _isReplay = GGXXACPR.GGXXACPR.GameMode == GameMode.ReplayTheater;
            _stateBuffer = new(_isReplay ? REPLAY_STATE_BUFFER_SIZE : DEFAULT_STATE_BUFFER_SIZE);
            PlayerMeters[0] = new Meter("Player 1", METER_LENGTH);
            PlayerMeters[1] = new Meter("Player 2", METER_LENGTH);
            EntityMeters[0] = new Meter("P1 Sub", METER_LENGTH);
            EntityMeters[1] = new Meter("P2 Sub", METER_LENGTH);
            ClearMeters();
        }

        /// <summary>
        /// Looks at the current game state, and updates the frame meter accordingly.
        /// </summary>
        /// <returns>Returns 0 if an update went through, otherwise returns 1</returns>
        public int Update()
        {
            int frameId = GGXXACPR.GGXXACPR.ReplayFrameCount;
            var P1 = GGXXACPR.GGXXACPR.Player1;
            var P2 = GGXXACPR.GGXXACPR.Player2;

            if (!P1.IsValid || !P2.IsValid || !GGXXACPR.GGXXACPR.ShouldUpdate) return 1;

            if (_isReplay)  // Rewinding only handled in replay mode. (May need to extend this to online mode to handle rollbacks if supporting that mode)
            {
                int frameDiff = frameId - _currentFrame;
                if (frameDiff == 0 || frameId == 0) return 1;

                if (frameDiff < 0)
                {
                    Rewind(frameId);
                    _currentFrame = frameId;
                    return 0;
                }

                if (frameDiff > 1)
                {
                    Debug.Log($"Frame difference of {frameDiff} is unexpected");
                }

                _currentFrame = frameId;
            }

            StateSnapShot prevState = GetPreviousState();
            
            // Skip update if either player is frozen in super flash while the opponent doesn't have an active hitbox.
            // The hitbox requirement is for moves that become active while in super flash.
            // Special exception for first freeze frame.
            if ((P1.Status.HasFlag(ActionState.Freeze) || P2.Status.HasFlag(ActionState.Freeze)) &&
                (prevState.p1.Status.HasFlag(ActionState.Freeze) || prevState.p2.Status.HasFlag(ActionState.Freeze)) &&
                Settings.FrameMeter.PauseDuringSuperFlash)
            {
                // TODO: account for projectile supers that hit during flash

                // Special case for super's that connect during super flash (e.g. Jam 632146S)
                // Rewrite the previous frame to an active frame and recalculate startup
                if (P2.Status.HasFlag(ActionState.Freeze) && GGXXACPR.GGXXACPR.HasActiveFrame(P1))
                {
                    PlayerMeters[0].FrameArr[AddToLoopingIndex(-1)].Type = FrameType.Active;

                    _index = AddToLoopingIndex(-1);
                    UpdateStartupByCountBackWithMoveData(P1, ref PlayerMeters[0]);
                    _index = AddToLoopingIndex(1);
                }
                else if (P1.Status.HasFlag(ActionState.Freeze) && GGXXACPR.GGXXACPR.HasActiveFrame(P2))
                {
                    PlayerMeters[1].FrameArr[AddToLoopingIndex(-1)].Type = FrameType.Active;

                    _index = AddToLoopingIndex(-1);
                    UpdateStartupByCountBackWithMoveData(P2, ref PlayerMeters[1]);
                    _index = AddToLoopingIndex(1);
                }

                _stateBuffer.Add(new(P1, P2, frameId));
                return 0;
            }
            // Skip update when both players are in hitstop (currently somewhat redundant when checking for unchanged animation timers above)
            // Special exception to also skip the first frame after hitstop counters have ended
            // Super freeze often uses the histop counter as well so need to except that situation
            if ((P1.HitstopCounter > 0 && P2.HitstopCounter > 0 ||
                    prevState.p1.HitstopCounter > 0 && prevState.p2.HitstopCounter > 0) &&
                    !P1.Status.HasFlag(ActionState.Freeze) &&
                    !P2.Status.HasFlag(ActionState.Freeze) &&
                    Settings.FrameMeter.PauseDuringHitstop)
            {
                _stateBuffer.Add(new(P1, P2, frameId));
                return 0;
            }


            // Pause logic
            if (_isPaused)
            {
                if (ShouldUnpause(P1, P2))
                {
                    _isPaused = false;
                    _index = 0;
                    ClearMeters();
                }
                else
                {
                    return 0;
                }
            }

            // Update each meter
            UpdateIndividualMeter(P1);
            UpdateIndividualMeter(P2);
            UpdateIndividualEntityMeter(P1);
            UpdateIndividualEntityMeter(P2);
            _frameNumberLookup[_index] = frameId;
            // Debug.Log($"[FRAME METER DEBUG] Update() ReplayFameCount: {frameId} | _frameNumberLookup[]: {_frameNumberLookup[_index]}  !!!!!!!!!!!!!!!!");

            // Check if frame meter should pause
            // TODO: account for frame properties?
            _isPaused = true;
            FrameType p1FrameType, p2FrameType;
            for (int i = 0; i < PAUSE_THRESHOLD; i++)
            {
                p1FrameType = FrameAtOffset(PlayerMeters[0], -i).Type;
                p2FrameType = FrameAtOffset(PlayerMeters[1], -i).Type;

                if (!IsIgnorableFrameType(p1FrameType) || !IsIgnorableFrameType(p2FrameType) ||
                    !IsIgnorableFrameType(FrameAtOffset(EntityMeters[0], -i).Type) ||
                    !IsIgnorableFrameType(FrameAtOffset(EntityMeters[1], -i).Type))
                {
                    _isPaused = false;
                    break;
                }
            }

            // Labels
            UpdateStartupByCountBackWithMoveData(P1, ref PlayerMeters[0]);
            UpdateStartupByCountBackWithMoveData(P2, ref PlayerMeters[1]);
            UpdateAdvantageByCountBack();
            UpdateRunSums(PlayerMeters[1]);
            UpdateRunSums(PlayerMeters[0]);

            _stateBuffer.Add(new(P1, P2, frameId));
            _index = (_index + 1) % METER_LENGTH;
            return 0;
        }


        /// <summary>
        /// Rewinds the frame meter's individual arrays and the state buffer backwards to the target frame
        /// </summary>
        /// <param name="targetFrame">The target frame to rewind towards. Identified by the value given by GGXXACPR.GGXXACPR.ReplayFrameCount</param>
        private void Rewind(int targetFrame)
        {
            // Rewind frame meter itself
            int indexFrameNum = _frameNumberLookup[AddToLoopingIndex(-1)];
            int frameMeterRollbackNum = 1;
            while (indexFrameNum > targetFrame && indexFrameNum > 0)
            {
                indexFrameNum = _frameNumberLookup[AddToLoopingIndex(-1 * (++frameMeterRollbackNum))];
            }
            frameMeterRollbackNum--;

            if (frameMeterRollbackNum > 0)
            {
                _index = AddToLoopingIndex(-frameMeterRollbackNum);

                for (int i = 0; i < frameMeterRollbackNum; i++)
                {
                    int index = AddToLoopingIndex(i);

                    // TODO: instead of erasing the frame pips here, load them from the stateBuffer
                    PlayerMeters[0].FrameArr[index] = new FrameMeterPip();
                    PlayerMeters[1].FrameArr[index] = new FrameMeterPip();
                    EntityMeters[0].FrameArr[index] = new FrameMeterPip();
                    EntityMeters[1].FrameArr[index] = new FrameMeterPip();
                    _frameNumberLookup[index] = -1;
                }
            }

            // Rewind the state buffer as well
            var indexState = _stateBuffer.Get(_stateBuffer.Index - 1);
            int stateFrameRollbackNum = 1;
            while (indexState.frameNumber > targetFrame && (_stateBuffer.Index - stateFrameRollbackNum) > 0)
            {
                indexState = _stateBuffer.Get(_stateBuffer.Index - (++stateFrameRollbackNum));
            }
            stateFrameRollbackNum--;
            if (stateFrameRollbackNum > 0)
            {
                _stateBuffer.RollBack(stateFrameRollbackNum);
            }
        }

        /// <summary>
        /// Clears each individual meter
        /// </summary>
        private void ClearMeters()
        {
            ClearMeter(ref PlayerMeters[0]);
            ClearMeter(ref PlayerMeters[1]);
            ClearMeter(ref EntityMeters[0], true);
            ClearMeter(ref EntityMeters[1], true);
            Array.Fill(_frameNumberLookup, 0);
        }
        /// <summary>
        /// Resets the given Meter m to default values. An explicit parameter for the hide value is given for entity meters.
        /// </summary>
        /// <param name="m">The Meter struct to clear</param>
        /// <param name="hide">What the set the Meter.Hide property to</param>
        private static void ClearMeter(ref Meter m, bool hide = false)
        {
            for (int i = 0; i < METER_LENGTH; i++)
            {
                m.FrameArr[i] = new FrameMeterPip();
            }
            m.Startup = -1;
            m.LastAttackActId = -1;
            m.Advantage = -1;
            m.Total = -1;
            m.DisplayAdvantage = false;
            m.Hide = hide;
        }

        /// <summary>
        /// Determines what type of frame should be displayed given the current state of a Player p.
        /// </summary>
        /// <param name="p">The Player to inspect</param>
        /// <returns>The determined frame type to represent the player state</returns>
        private static FrameType DetermineFrameType(Player p)
        {
            if (GGXXACPR.GGXXACPR.IsThrowActive(p) || GGXXACPR.GGXXACPR.IsCommandThrowActive(p))
            {
                return FrameType.ActiveThrow;
            }
            else if (GGXXACPR.GGXXACPR.HasActiveFrame(p))
            {
                return FrameType.Active;
            }
            // Slide Head runs a grounded status check for all entities to determine if the unblockable connects
            else if (p.CharId == CharacterID.POTEMKIN &&
                p.ActionId == GGXXACPR.GGXXACPR.SLIDE_HEAD_ACT_ID &&
                p.ActionHeaderFlags == GGXXACPR.GGXXACPR.SLIDE_HEAD_UNBLOCKABLE_ACT_HEADER_VALUE)
            {
                return FrameType.Active;
            }
            else if (p.Status.HasFlag(ActionState.IsInBlockstun) || p.Extra.SBTime > 0)
            {
                return FrameType.Blockstun;
            }
            else if (p.Status.HasFlag(ActionState.IsInHitstun))
            {
                if (p.Status.HasFlag(ActionState.KnockedDown))
                {
                    return FrameType.KnockDownHitstun;
                }
                else if (p.Status.HasFlag(ActionState.IsAirborne) && p.Extra.UntechTimer < 0)
                {
                    return FrameType.TechableHitstun;
                }
                else
                {
                    return FrameType.Hitstun;
                }
            }
            else if (p.AttackFlags.HasFlag(AttackState.IsInRecovery))
            {
                return FrameType.Recovery;
            }
            else if (p.CommandFlags == CommandState.IsMove && !p.AttackFlags.HasFlag(AttackState.IsInRecovery))
            {
                return FrameType.CounterHitState;
            }
            else if (p.CommandFlags == CommandState.IsMove)
            {
                return FrameType.Startup;
            }
            else if (p.CommandFlags.HasFlag(CommandState.Prejump) ||
                (p.CommandFlags is CommandState.FreeCancel or CommandState.RunDash or
                    CommandState.StepDash or CommandState.RunDashSkid))
            {
                return FrameType.Movement;
            }
            else
            {
                return FrameType.Neutral;
            }
        }

        /// <summary>
        /// Determines what frame properties should be displayed given the current state of a Player p.
        /// </summary>
        /// <param name="p">The Player to inspect</param>
        /// <returns>0 to 2 frame primary frame properties to represent the player state</returns>
        private static PrimaryFrameProperty[] DeterminePrimaryFrameProperties(Player p)
        {
            var output = new Stack<PrimaryFrameProperty>(2);
            output.Push(PrimaryFrameProperty.Default);
            output.Push(PrimaryFrameProperty.Default);

            if (p.Extra.SBTime > 0)
            {
                output.Push(PrimaryFrameProperty.Slashback);
            }

            if (HasStrikeInvuln(p) && HasThrowInvuln(p))
            {
                output.Push(PrimaryFrameProperty.InvulnFull);
            }
            else if (HasThrowInvuln(p))
            {
                output.Push(PrimaryFrameProperty.InvulnThrow);
            }
            else if (HasStrikeInvuln(p))
            {
                output.Push(PrimaryFrameProperty.InvulnStrike);
            }

            if (p.GuardFlags.HasFlag(GuardState.Armor))
            {
                output.Push(PrimaryFrameProperty.Armor);
            }
            else if (p.GuardFlags.HasFlag(GuardState.Parry1) || p.GuardFlags.HasFlag(GuardState.Parry2))
            {
                if (p.CharId == CharacterID.JAM)
                {
                    // Jam parry flips her Parry2 flag for the rest of her current animation and uses a character specific counter for the active window
                    // Jam parry works by swapping out "on guard" interrupt functions (stored at player->0x2C->0xC8). Since this is only called while
                    //  She has a guard flag set, we'll need to check them here too.
                    if (p.Extra.JamParryTime == 0xFF &&
                        (p.GuardFlags.HasFlag(GuardState.IsStandBlocking) ||
                        p.GuardFlags.HasFlag(GuardState.IsCrouchBlocking)))
                    {
                        output.Push(PrimaryFrameProperty.Parry);
                    }
                }
                // Special case for Axl parry and Dizzy EX parry super
                else if (p.CharId == CharacterID.AXL && p.ActionId == GGXXACPR.GGXXACPR.AXL_TENHOU_SEKI_UPPER_ACT_ID ||
                         p.CharId == CharacterID.AXL && p.ActionId == GGXXACPR.GGXXACPR.AXL_TENHOU_SEKI_LOWER_ACT_ID ||
                         p.CharId == CharacterID.DIZZY && p.ActionId == GGXXACPR.GGXXACPR.DIZZY_EX_NECRO_UNLEASHED_ACT_ID)
                {
                    // These moves are marked as in parry state for their full animation and use a special move specific
                    //  variable (Player.Mark) to actually determine if the move should parry.
                    if (p.Mark == 1)
                    {
                        output.Push(PrimaryFrameProperty.Parry);
                    }
                }
                else
                {
                    output.Push(PrimaryFrameProperty.Parry);
                }
            }
            else if (p.GuardFlags.HasFlag(GuardState.GuardPoint))
            {
                if (p.GuardFlags.HasFlag(GuardState.IsStandBlocking) && p.GuardFlags.HasFlag(GuardState.IsCrouchBlocking))
                {
                    output.Push(PrimaryFrameProperty.GuardPointFull);
                }
                else if (p.GuardFlags.HasFlag(GuardState.IsStandBlocking))
                {
                    output.Push(PrimaryFrameProperty.GuardPointHigh);
                }
                else if (p.GuardFlags.HasFlag(GuardState.IsCrouchBlocking))
                {
                    output.Push(PrimaryFrameProperty.GuardPointLow);
                }
            }

            if (output.Count == 0)
            {
                output.Push(PrimaryFrameProperty.Default);
            }

            return output.ToArray();
        }

        /// <summary>
        /// Determines the frame type for a given player's entity sub meter.
        /// </summary>
        private static FrameType DetermineEntityFrameType(Player p)
        {
            int pIndex = p.PlayerIndex;

            // TODO: Preformance concern

            if (GGXXACPR.GGXXACPR.AnyEntities(
                e =>
                {
                    if (e.PlayerIndex != pIndex) return false;

                    if (Settings.FrameMeter.IgnoreDistantProjectiles && !IsEntityInBounds(e))
                        return false;

                    foreach (Hitbox hb in e.HitboxSet)
                    {
                        if (hb.BoxTypeId == (short)BoxId.HIT) return true;
                    }

                    return false;
                }))
            {
                return FrameType.Active;
            }
            else if (GGXXACPR.GGXXACPR.AnyEntities(
                e =>
                {
                    if (e.PlayerIndex != pIndex) return false;

                    if (Settings.FrameMeter.IgnoreDistantProjectiles && !IsEntityInBounds(e))
                        return false;

                    foreach (Hitbox hb in e.HitboxSet)
                    {
                        if (hb.BoxTypeId == (short)BoxId.HURT) return true;
                    }

                    return false;
                }))
            {
                return FrameType.Startup;
            }
            else
            {
                return FrameType.None;
            }
        }

        private const int X_IGNORE_BOUNDARY = 86000;
        private const int Y_IGNORE_TOP_BOUNDARY = -120000;
        private const int Y_IGNORE_BOTTOM_BOUNDARY = 48000;
        /// <summary>
        /// Determines if a given entity is within the ignore boundary. This is used to discard active entities that are too far away to be relevant.
        /// </summary>
        /// <param name="e">A specific entity</param>
        /// <returns>True if in bounds, false if it should be ignored</returns>
        private static bool IsEntityInBounds(Entity e)
        {
            return Math.Abs(e.XPos) < X_IGNORE_BOUNDARY &&
                e.YPos > Y_IGNORE_TOP_BOUNDARY &&
                e.YPos < Y_IGNORE_BOTTOM_BOUNDARY;
        }

        private void UpdateIndividualMeter(Player p)
        {
            int index = p.PlayerIndex;
            FrameType type = DetermineFrameType(p);
            PrimaryFrameProperty[] pProps = DeterminePrimaryFrameProperties(p);
            SecondaryFrameProperty sProp = SecondaryFrameProperty.Default;

            if (p.Extra.RCTime > 0)
            {
                sProp = SecondaryFrameProperty.FRC;
            }

            PlayerMeters[index].FrameArr[_index] = new FrameMeterPip()
            {
                Type = type,
                PrimaryProperty1 = pProps[0],
                PrimaryProperty2 = pProps[1],
                SecondaryProperty = sProp,
                PlayerState = new PlayerSnapShot(p),
            };
            PlayerMeters[index].FrameArr[(_index + 2 + METER_LENGTH) % METER_LENGTH] = new FrameMeterPip(); // Forward erasure
        }

        private void UpdateIndividualEntityMeter(Player p)
        {
            int index = p.PlayerIndex;
            FrameType type = DetermineEntityFrameType(p);

            EntityMeters[index].FrameArr[_index] = new FrameMeterPip() { Type = type };
            EntityMeters[index].FrameArr[(_index + 2 + METER_LENGTH) % METER_LENGTH] = new FrameMeterPip(); // Forward erasure

            // Update hide flag
            EntityMeters[index].Hide = !EntityMeters[index].FrameArr.Any(frame => frame.Type != FrameType.None);
        }

        private void UpdateAdvantageByCountBack()
        {
            AdvCountBackFromPlayer(ref PlayerMeters[0], ref PlayerMeters[1]);
            AdvCountBackFromPlayer(ref PlayerMeters[1], ref PlayerMeters[0]);
        }
        private void AdvCountBackFromPlayer(ref Meter pMeterA, ref Meter pMeterB)
        {
            if (FrameAtOffset(pMeterA, 0).Type == FrameType.Neutral &&
                FrameAtOffset(pMeterA, -1).Type != FrameType.Neutral &&
                FrameAtOffset(pMeterB, 0).Type == FrameType.Neutral)
            {
                for (int i = 1; i < METER_LENGTH; i++)
                {
                    if (FrameAtOffset(pMeterB, -i).Type != FrameType.Neutral)
                    {
                        pMeterA.Advantage = 1 - i;
                        pMeterB.Advantage = i - 1;
                        pMeterA.DisplayAdvantage = true;
                        pMeterB.DisplayAdvantage = true;
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// Calculates startup by looking at the player's current action animation counter.
        /// This is unreliable because many attacks are made up of multiple actions, so the
        /// animation counter will not necessarilly account for the entire move.
        /// Another source for inaccuracy is moves that animate during super freeze.
        /// The animation counter will count up, but super freeze frames are generally not counted in startup measurements.
        /// 
        /// Use UpdateStartupByCountBackWithMoveData instead.
        /// </summary>
        private void UpdateStartupByAnimCounter(Player p, ref Meter pMeter)
        {
            FrameType prevFrameType = FrameAtOffset(pMeter, -1).Type;
            if (FrameAtOffset(pMeter, 0).Type == FrameType.Active &&
                (prevFrameType == FrameType.CounterHitState || prevFrameType == FrameType.Startup))
            {
                pMeter.Startup = p.AnimationCounter;
            }
        }

        /// <summary>
        /// Calculates startup using data from the MoveData class.
        /// </summary>
        private void UpdateStartupByCountBackWithMoveData(Player p, ref Meter pMeter)
        {
            FrameType[] activeTypes = [FrameType.Active, FrameType.ActiveThrow];
            FrameMeterPip currFrame = FrameAtOffset(pMeter, 0);
            FrameType prevFrameType = FrameAtOffset(pMeter, -1).Type;

            if (activeTypes.Contains(currFrame.Type) && !activeTypes.Contains(prevFrameType))
            {
                pMeter.LastAttackActId = currFrame.PlayerState.ActionId;
                FrameMeterPip frame;
                for (int i = 1; i < METER_LENGTH; i++)
                {
                    frame = FrameAtOffset(pMeter, -i);
                    if (frame.PlayerState.ActionId != pMeter.LastAttackActId &&
                        !MoveData.IsPrevAnimSameMove(p.CharId, frame.PlayerState.ActionId, pMeter.LastAttackActId))
                    {
                        pMeter.Startup = i;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Determines if a continuous run of FrameTypes just ended in the given Meter and updates that FrameMeterPip's RunSum value.
        /// </summary>
        private void UpdateRunSums(Meter meter)
        {
            if (FrameAtOffset(meter, -1).Type == FrameAtOffset(meter, 0).Type)
                return;

            FrameType runType = FrameAtOffset(meter, -1).Type;

            if (runType is FrameType.None or FrameType.Neutral) return;

            int countingBack = 1;
            while (countingBack < METER_LENGTH && FrameAtOffset(meter, -countingBack).Type == FrameAtOffset(meter, -countingBack - 1).Type)
            {
                countingBack++;
            }

            if (countingBack is >= MINIMUM_RUN_SUM and < (METER_LENGTH - 3))
            {
                meter.FrameArr[AddToLoopingIndex(-1)].RunSum = countingBack;
            }
        }

        /// <summary>
        /// Gets the Frame object at the current index plus an offset. Handles array looping.
        /// </summary>
        private FrameMeterPip FrameAtOffset(Meter meter, int offset)
        {
            return meter.FrameArr[AddToLoopingIndex(offset)];
        }
        /// <summary>
        /// Returns the meter index plus an offset. Handles looping. WARNING: Does not handle inputs < METER_LENGTH * -1.
        /// </summary>
        private int AddToLoopingIndex(int offset)
        {
            return (_index + offset + METER_LENGTH) % METER_LENGTH;
        }

        private StateSnapShot GetPreviousState()
        {
            if (_stateBuffer.Index == _stateBuffer.MinIndex) return default;
            return _stateBuffer.Get(_stateBuffer.Index - 1);
        }

        private static bool ShouldUnpause(Player p1, Player p2)
        {
            return !IsIgnorableFrameType(DetermineFrameType(p1)) || !IsIgnorableFrameType(DetermineFrameType(p2));
        }
        private static bool IsIgnorableFrameType(FrameType type) => type is FrameType.None or FrameType.Neutral;

        private static bool HasThrowInvuln(Player p)
        {
            if (p.Status.HasFlag(ActionState.IsInHitstun) || p.Status.HasFlag(ActionState.IsInBlockstun))
                return false;

            return p.Status.HasFlag(ActionState.IsThrowInvuln) || p.Extra.ThrowProtectionTimer > 0;
        }
        private static bool HasStrikeInvuln(Player p)
        {
            return p.Status.HasFlag(ActionState.DisableHurtboxes) ||
                    p.Status.HasFlag(ActionState.StrikeInvuln) ||
                    p.Extra.InvulnCounter > 0 ||
                    !GGXXACPR.GGXXACPR.HasAnyHurtboxes(p) ||
                    p.Status.HasFlag(ActionState.ProjDisableHitboxes);  // Technically not strike invuln, but will be considered
        }
    }
}
