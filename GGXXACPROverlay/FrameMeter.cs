using GGXXACPROverlay.GGXXACPR;

namespace GGXXACPROverlay
{
    internal class FrameMeter
    {
        public enum FrameType
        {
            None,
            Neutral,
            Movement,
            CounterHitState,
            Startup,
            Active,
            ActiveThrow,
            Recovery,
            BlockStun,
            HitStun
        }

        /// <summary>
        /// Drawn on bottom of frame meter pip
        /// </summary>
        public enum PrimaryFrameProperty
        {
            Default,
            InvulnFull,
            InvulnStrike,
            InvulnThrow,
            Parry,
            GuardPointFull,
            GuardPointHigh,
            GuardPointLow,
            Armor,
            FRC,
            SlashBack,
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

        public readonly struct Frame(
            FrameType type = FrameType.None,
            PrimaryFrameProperty pprop1 = PrimaryFrameProperty.Default,
            PrimaryFrameProperty pprop2 = PrimaryFrameProperty.Default,
            SecondaryFrameProperty sprop = SecondaryFrameProperty.Default,
            int actId = -1,
            int actTimer = -1,
            byte hitStop = 0,
            uint status = 0)
        {
            public readonly FrameType Type = type;
            public readonly PrimaryFrameProperty PrimaryProperty1 = pprop1;
            public readonly PrimaryFrameProperty PrimaryProperty2 = pprop2;
            public readonly SecondaryFrameProperty SecondaryProperty = sprop;
            public readonly int ActId = actId;
            public readonly int ActTimer = actTimer;
            public readonly byte HitStop = hitStop;
            public readonly ActionStateFlags Status = status;
        }

        public const int METER_LENGTH = 80;
        private const int PAUSE_THRESHOLD = 10;

        public struct Meter(string name, int length)
        {
            public readonly string Label = name;
            public int Startup = -1;
            public int LastAttackActId = -1;
            public int Total = -1;
            public int Advantage = 0;
            public bool DisplayAdvantage = false;
            public bool Hide = false;
            public Frame[] FrameArr = new Frame[length];
        }

        private int _index;
        private bool _isPaused = true;
        public Meter[] PlayerMeters = new Meter[2];
        public Meter[] EntityMeters = new Meter[2];

        private GameState? prevState = null;

        //public FrameMeter()
        //{
        //    _index = 0;
        //    PlayerMeters[0] = new Meter("Player 1", METER_LENGTH);
        //    PlayerMeters[1] = new Meter("Player 2", METER_LENGTH);
        //    EntityMeters[0] = new Meter("P1 Sub", METER_LENGTH);
        //    EntityMeters[1] = new Meter("P2 Sub", METER_LENGTH);
        //    ClearMeters();
        //}

        //public int Update(GameState state, in OverlaySettings settings)
        //{
        //    Frame p1PrevFrame = FrameAtOffset(PlayerMeters[0], -1);
        //    Frame p2PrevFrame = FrameAtOffset(PlayerMeters[1], -1);

        //    // !! Very hacky discard update checks below. These are temp solutions to
        //    //  mitigate syncing issues while gamestate update hooks are being worked on.

        //    // BoxIter is a game state evaluation iterable in the game code. It will only ever not be 255 when the game is evaluating hitboxes.
        //    //  Although not a perfect safe guard, this should lessen incorrect frames due to mid-update game state reads until hooks are implemented.
        //    if (state.Player1.BoxIter != 255 || state.Player2.BoxIter != 255)
        //    {
        //        Console.WriteLine($"Mid update read detected. P1:{state.Player1.BoxIter}, P2:{state.Player2.BoxIter}");
        //        return 1;
        //    }
        //    // Skip update if both character haven't advanced a frame (TODO: Should update this logic after D3D hook update)
        //    // This should handle double frame reads as well as pausing
        //    if (state.Player1.AnimationCounter == prevState?.Player1.AnimationCounter &&
        //        state.Player2.AnimationCounter == prevState?.Player2.AnimationCounter &&
        //        state.Player1.HitstopCounter == prevState?.Player1.HitstopCounter &&
        //        state.Player2.HitstopCounter == prevState?.Player2.HitstopCounter)
        //    {
        //        return 0;
        //    }
        //    // Skip update if either player is frozen in super flash while the opponent doesn't have an active hitbox.
        //    // The hitbox requirement is for moves that become active while in super flash.
        //    // Special exception for first freeze frame.
        //    if ((state.Player1.Status.Freeze || state.Player2.Status.Freeze) &&
        //        (p1PrevFrame.Status.Freeze || p2PrevFrame.Status.Freeze) &&
        //        !settings.RecordDuringSuperFlash)
        //    {
        //        // TODO: account for projectile supers that hit during flash

        //        // Special case for super's that connect during super flash (e.g. Jam 632146S)
        //        // Rewrite the previous frame to an active frame and recalculate startup
        //        if (state.Player2.Status.Freeze && state.Player1.HasActiveFrame())
        //        {
        //            PlayerMeters[0].FrameArr[AddToLoopingIndex(-1)] = new Frame(
        //                FrameType.Active, p1PrevFrame.PrimaryProperty1, p1PrevFrame.PrimaryProperty2, p1PrevFrame.SecondaryProperty, p1PrevFrame.ActId,
        //                p1PrevFrame.ActTimer, p1PrevFrame.HitStop, (uint)p1PrevFrame.Status);

        //            _index = AddToLoopingIndex(-1);
        //            UpdateStartupByCountBackWithMoveData(state.Player1, ref PlayerMeters[0], EntityMeters[0]);
        //            _index = AddToLoopingIndex(1);
        //        }
        //        else if (state.Player1.Status.Freeze && state.Player2.HasActiveFrame())
        //        {
        //            PlayerMeters[1].FrameArr[AddToLoopingIndex(-1)] = new Frame(
        //                FrameType.Active, p2PrevFrame.PrimaryProperty1, p1PrevFrame.PrimaryProperty2, p2PrevFrame.SecondaryProperty, p2PrevFrame.ActId,
        //                p2PrevFrame.ActTimer, p2PrevFrame.HitStop, (uint)p2PrevFrame.Status);

        //            _index = AddToLoopingIndex(-1);
        //            UpdateStartupByCountBackWithMoveData(state.Player2, ref PlayerMeters[1], EntityMeters[1]);
        //            _index = AddToLoopingIndex(1);
        //        }

        //        prevState = state;
        //        return 0;
        //    }
        //    // Skip update when both players are in hitstop (currently somewhat redundant when checking for unchanged animation timers above)
        //    // Special exception to also skip the first frame after hitstop counters have ended
        //    // Super freeze often uses the histop counter as well so need to except that situation
        //    if ((state.Player1.HitstopCounter > 0 && state.Player2.HitstopCounter > 0 ||
        //            prevState?.Player1.HitstopCounter > 0 && prevState?.Player2.HitstopCounter > 0) &&
        //            !(state.Player1.Status.Freeze || state.Player2.Status.Freeze) &&
        //            !settings.RecordDuringHitstop)
        //    {
        //        prevState = state;
        //        return 0;
        //    }


        //    // Pause logic
        //    if (_isPaused)
        //    {
        //        if ((DetermineFrameType(state, 0) == FrameType.Neutral) &&
        //            (DetermineFrameType(state, 1) == FrameType.Neutral) &&
        //            (DetermineEntityFrameType(state, 0) == FrameType.None) &&
        //            (DetermineEntityFrameType(state, 1) == FrameType.None))
        //        {
        //            prevState = state;
        //            return 0;
        //        }
        //        else
        //        {
        //            _isPaused = false;
        //            _index = 0;
        //            ClearMeters();
        //        }
        //    }

        //    // Update each meter
        //    UpdateIndividualMeter(state, 0);
        //    UpdateIndividualMeter(state, 1);
        //    UpdateIndividualEntityMeter(state, 0);
        //    UpdateIndividualEntityMeter(state, 1);

        //    // Check if frame meter should pause
        //    // TODO: account for frame properties?
        //    _isPaused = true;
        //    FrameType p1FrameType, p2FrameType;
        //    for (int i = 0; i < PAUSE_THRESHOLD; i++)
        //    {
        //        p1FrameType = FrameAtOffset(PlayerMeters[0], -i).Type;
        //        p2FrameType = FrameAtOffset(PlayerMeters[1], -i).Type;

        //        if ((p1FrameType != FrameType.Neutral && p1FrameType != FrameType.None) ||
        //            (p2FrameType != FrameType.Neutral && p2FrameType != FrameType.None) ||
        //            FrameAtOffset(EntityMeters[0], -i).Type != FrameType.None ||
        //            FrameAtOffset(EntityMeters[1], -i).Type != FrameType.None)
        //        {
        //            _isPaused = false;
        //            break;
        //        }
        //    }

        //    // Labels
        //    UpdateStartupByCountBackWithMoveData(state.Player1, ref PlayerMeters[0], EntityMeters[0]);
        //    UpdateStartupByCountBackWithMoveData(state.Player2, ref PlayerMeters[1], EntityMeters[1]);
        //    UpdateAdvantageByCountBack();

        //    _index = (_index + 1) % METER_LENGTH;
        //    prevState = state;
        //    return 0;
        //}

        //private void ClearMeters()
        //{
        //    ClearMeter(ref PlayerMeters[0], false);
        //    ClearMeter(ref PlayerMeters[1], false);
        //    ClearMeter(ref EntityMeters[0], true);
        //    ClearMeter(ref EntityMeters[1], true);
        //}
        //private static void ClearMeter(ref Meter m, bool hide)
        //{
        //    for(int i = 0; i < METER_LENGTH; i++)
        //    {
        //        m.FrameArr[i] = new Frame();
        //    }
        //    m.Startup = -1;
        //    m.LastAttackActId = -1;
        //    m.Advantage = -1;
        //    m.Total = -1;
        //    m.DisplayAdvantage = false;
        //    m.Hide = hide;
        //}

        //private static FrameType DetermineFrameType(GameState state, int index)
        //{
        //    var player = index == 0 ? state.Player1 : state.Player2;
        //    var cmdGrabId = index == 0 ? state.GlobalFlags.P1CommandGrabRange : state.GlobalFlags.P2CommandGrabRange;

        //    if (player.Mark == 1 && MoveData.IsActiveByMark(player.CharId, player.ActionId))
        //    {
        //        return FrameType.ActiveThrow;    // Command Grab
        //    }
        //    else if ((state.GlobalFlags.ThrowFlags.Player1ThrowSuccess && index == 0 &&
        //        state.Player2.Status.IsInHitstun ||
        //        state.GlobalFlags.ThrowFlags.Player2ThrowSuccess && index == 1 &&
        //        state.Player1.Status.IsInHitstun) &&
        //        !player.CommandFlags.DisableThrow)
        //    {
        //        return FrameType.ActiveThrow;
        //    }
        //    else if (player.HitboxSet.Any(h => h.BoxTypeId == BoxId.HIT) && !player.Status.DisableHitboxes)
        //    {
        //        return FrameType.Active;
        //    }
        //    // Slide Head runs a grounded status check for all entities when his hitbox flag is -1 to determine if the unblockable connects
        //    else if (player.CharId == CharacterID.POTEMKIN &&
        //        player.ActionId == GGXXACPR.GGXXACPR.SLIDE_HEAD_ACT_ID &&
        //        player.HitboxFlag == 0xFF)
        //    {
        //        return FrameType.Active;
        //    }
        //    else if (player.Status.IsInBlockstun || player.Extra.SBTime > 0)
        //    {
        //        return FrameType.BlockStun;
        //    }
        //    else if (player.Status.IsInHitstun)
        //    {
        //        return FrameType.HitStun;
        //    }
        //    else if (player.AttackFlags.IsInRecovery)
        //    {
        //        return FrameType.Recovery;
        //    }
        //    else if (player.CommandFlags.IsMove && !player.AttackFlags.IsInRecovery)
        //    {
        //        return FrameType.CounterHitState;
        //    }
        //    else if (player.CommandFlags.IsMove)
        //    {
        //        return FrameType.Startup;
        //    }
        //    else if (player.CommandFlags.Prejump ||
        //        player.CommandFlags.FreeCancel || player.CommandFlags.RunDash ||
        //        player.CommandFlags.StepDash || player.CommandFlags.RunDashSkid)
        //    {
        //        return FrameType.Movement;
        //    }
        //    else
        //    {
        //        return FrameType.Neutral;
        //    }
        //}

        //private static PrimaryFrameProperty[] DeterminePrimaryFrameProperties(GameState state, int index)
        //{
        //    var output = new Stack<PrimaryFrameProperty>(2);
        //    output.Push(PrimaryFrameProperty.Default);
        //    output.Push(PrimaryFrameProperty.Default);

        //    Player p = state.Player1;
        //    if (index > 0) p = state.Player2;

        //    if (p.Extra.SBTime > 0)
        //    {
        //        output.Push(PrimaryFrameProperty.SlashBack);
        //    }

        //    if ((p.Status.DisableHurtboxes ||
        //            p.Status.StrikeInvuln ||
        //            p.Extra.InvulnCounter > 0 ||
        //            !p.HitboxSet.Any((Hitbox h) => h.BoxTypeId == BoxId.HURT ||
        //            p.Status.ProjDisableHitboxes)
        //        ) && (p.Status.IsThrowInuvln ||
        //            p.Extra.ThrowProtectionTimer > 0))
        //    {
        //        output.Push(PrimaryFrameProperty.InvulnFull);
        //    }
        //    else if (p.Status.IsThrowInuvln ||
        //        (p.Extra.ThrowProtectionTimer > 0 &&
        //            !(p.Status.IsInHitstun || p.Status.IsInBlockstun)))
        //    {
        //        output.Push(PrimaryFrameProperty.InvulnThrow);
        //    }
        //    else if (p.Status.DisableHurtboxes ||
        //            p.Status.StrikeInvuln ||
        //            p.Extra.InvulnCounter > 0 ||
        //            !p.HitboxSet.Any((Hitbox h) => h.BoxTypeId == BoxId.HURT) ||
        //            p.Status.ProjDisableHitboxes)
        //    {
        //        output.Push(PrimaryFrameProperty.InvulnStrike);
        //    }

        //    if (p.GuardFlags.Armor)
        //    {
        //        output.Push(PrimaryFrameProperty.Armor);
        //    }
        //    else if (p.GuardFlags.Parry1 || p.GuardFlags.Parry2)
        //    {
        //        if (p.CharId == CharacterID.JAM)
        //        {
        //            // Jam parry flips her Parry2 flag for the rest of her current animation and uses a character specific counter for the active window
        //            // Jam parry works by swapping out "on guard" interrupt functions (stored at player->0x2C->0xC8). Since this is only called while
        //            //  She has a guard flag set, we'll need to check them here too.
        //            if (p.Extra.JamParryTime == 0xFF && (p.GuardFlags.IsStandBlocking || p.GuardFlags.IsCrouchBlocking))
        //            {
        //                output.Push(PrimaryFrameProperty.Parry);
        //            }
        //        }
        //        // Special case for Axl parry and Dizzy EX parry super
        //        else if ((p.CharId == CharacterID.AXL && p.ActionId == GGXXACPR.GGXXACPR.AXL_TENHOU_SEKI_UPPER_ACT_ID) ||
        //                 (p.CharId == CharacterID.AXL && p.ActionId == GGXXACPR.GGXXACPR.AXL_TENHOU_SEKI_LOWER_ACT_ID) ||
        //                 (p.CharId == CharacterID.DIZZY && p.ActionId == GGXXACPR.GGXXACPR.DIZZY_EX_NECRO_UNLEASHED_ACT_ID))
        //        {
        //            // These moves are marked as in parry state for their full animation and use a special move specific
        //            //  variable (Player.Mark) to actually determine if the move should parry.
        //            if (p.Mark == 1)
        //            {
        //                output.Push(PrimaryFrameProperty.Parry);
        //            }
        //        }
        //        else
        //        {
        //            output.Push(PrimaryFrameProperty.Parry);
        //        }
        //    }
        //    else if (p.GuardFlags.GuardPoint)
        //    {
        //        if (p.GuardFlags.IsStandBlocking && p.GuardFlags.IsCrouchBlocking)
        //        {
        //            output.Push(PrimaryFrameProperty.GuardPointFull);
        //        }
        //        else if (p.GuardFlags.IsStandBlocking)
        //        {
        //            output.Push(PrimaryFrameProperty.GuardPointHigh);
        //        }
        //        else if (p.GuardFlags.IsCrouchBlocking)
        //        {
        //            output.Push(PrimaryFrameProperty.GuardPointLow);
        //        }
        //    }

        //    if (output.Count == 0)
        //    {
        //        output.Push(PrimaryFrameProperty.Default);
        //    }

        //    return output.ToArray();
        //}

        //private static FrameType DetermineEntityFrameType(GameState state, int index)
        //{
        //    // LINQ is so nice
        //    var ownerEntitiesHitboxes =
        //        state.Entities.Where(e => IsEntityInBounds(e) && e.PlayerIndex == index && !e.Status.DisableHitboxes)
        //                      .SelectMany(e => e.HitboxSet)
        //                      .Where(h => h.BoxTypeId == BoxId.HIT);

        //    var ownerEntityHurtboxes =
        //        state.Entities.Where(e => IsEntityInBounds(e) && e.PlayerIndex == index && !e.Status.DisableHurtboxes)
        //                      .SelectMany(e => e.HitboxSet)
        //                      .Where(h => h.BoxTypeId == BoxId.HURT);

        //    if (ownerEntitiesHitboxes.ToArray().Length > 0)
        //    {
        //        return FrameType.Active;
        //    }
        //    else if (ownerEntityHurtboxes.ToArray().Length > 0)
        //    {
        //        return FrameType.Startup;
        //    }
        //    else
        //    {
        //        return FrameType.None;
        //    }
        //}

        //private const int X_IGNORE_BOUNDARY = 86000;
        //private const int Y_IGNORE_TOP_BOUNDARY = -120000;
        //private const int Y_IGNORE_BOTTOM_BOUNDARY = 48000;
        //private static bool IsEntityInBounds(Entity e)
        //{
        //    if (Math.Abs(e.XPos) > X_IGNORE_BOUNDARY ||
        //        e.YPos < Y_IGNORE_TOP_BOUNDARY ||
        //        e.YPos > Y_IGNORE_BOTTOM_BOUNDARY)
        //    {
        //        return false;
        //    }

        //    return true;
        //}

        //private void UpdateIndividualMeter(GameState state, int index)
        //{
        //    Player[] players = [state.Player1, state.Player2];

        //    FrameType type = DetermineFrameType(state, index);
        //    PrimaryFrameProperty[] pprops = DeterminePrimaryFrameProperties(state, index);
        //    SecondaryFrameProperty prop2 = SecondaryFrameProperty.Default;

        //    if (players[index].Extra.RCTime > 0)
        //    {
        //        prop2 = SecondaryFrameProperty.FRC;
        //    }

        //    PlayerMeters[index].FrameArr[_index] = new Frame(type, pprops[0], pprops[1], prop2,
        //        players[index].ActionId, players[index].AnimationCounter, players[index].HitstopCounter, (uint)players[index].Status);
        //    PlayerMeters[index].FrameArr[(_index + 2 + METER_LENGTH) % METER_LENGTH] = new Frame(); // Forward erasure
        //}

        //private void UpdateIndividualEntityMeter(GameState state, int index)
        //{
        //    FrameType type = DetermineEntityFrameType(state, index);

        //    EntityMeters[index].FrameArr[_index] = new Frame(type);
        //    EntityMeters[index].FrameArr[(_index + 2 + METER_LENGTH) % METER_LENGTH] = new Frame(); // Forward erasure

        //    // Update hide flag
        //    EntityMeters[index].Hide = !EntityMeters[index].FrameArr.Any((Frame f) => f.Type != FrameType.None);
        //}

        //private void UpdateAdvantageByCountBack()
        //{
        //    AdvCountBackFromPlayer(ref PlayerMeters[0], ref PlayerMeters[1]);
        //    AdvCountBackFromPlayer(ref PlayerMeters[1], ref PlayerMeters[0]);
        //}
        //private void AdvCountBackFromPlayer(ref Meter pMeterA, ref Meter pMeterB)
        //{
        //    if (FrameAtOffset(pMeterA, 0).Type == FrameType.Neutral &&
        //        FrameAtOffset(pMeterA, -1).Type != FrameType.Neutral &&
        //        FrameAtOffset(pMeterB, 0).Type == FrameType.Neutral)
        //    {
        //        for (int i = 1; i < METER_LENGTH; i++)
        //        {
        //            if (FrameAtOffset(pMeterB, -i).Type != FrameType.Neutral)
        //            {
        //                pMeterA.Advantage = 1 - i;
        //                pMeterB.Advantage = i - 1;
        //                pMeterA.DisplayAdvantage = true;
        //                pMeterB.DisplayAdvantage = true;
        //                break;
        //            }
        //        }
        //    }
        //}
        //private void UpdateStartupByAnimCounter(Player p, ref Meter pMeter, Meter eMeter)
        //{
        //    FrameType prevFrameType = FrameAtOffset(pMeter, -1).Type;
        //    if (FrameAtOffset(pMeter, 0).Type == FrameType.Active &&
        //        (prevFrameType == FrameType.CounterHitState || prevFrameType == FrameType.Startup))
        //    {
        //        pMeter.Startup = p.AnimationCounter;
        //    }
        //}
        //private void UpdateStartupByCountBackWithMoveData(Player p, ref Meter pMeter, Meter eMeter)
        //{
        //    FrameType[] activeTypes = [FrameType.Active, FrameType.ActiveThrow];
        //    FrameType[] prevFrameTypesAllowed = [FrameType.CounterHitState, FrameType.Startup, FrameType.None];
        //    Frame currFrame = FrameAtOffset(pMeter, 0);
        //    FrameType prevFrameType = FrameAtOffset(pMeter, -1).Type;

        //    //if (activeTypes.Contains(currFrame.Type) && prevFrameTypesAllowed.Contains(prevFrameType))
        //    if (activeTypes.Contains(currFrame.Type) && !activeTypes.Contains(prevFrameType))
        //    {
        //        pMeter.LastAttackActId = currFrame.ActId;
        //        Frame frame;
        //        for (int i = 1; i < METER_LENGTH; i++)
        //        {
        //            frame = FrameAtOffset(pMeter, -i);
        //            if (frame.ActId != pMeter.LastAttackActId && !MoveData.IsPrevAnimSameMove(p.CharId, frame.ActId, pMeter.LastAttackActId))
        //            {
        //                pMeter.Startup = i;
        //                break;
        //            }
        //        }
        //    }
        //}

        ///// <summary>
        ///// Gets the Frame object at the current index plus an offset. Handles array looping.
        ///// </summary>
        //private Frame FrameAtOffset(Meter meter, int offset)
        //{
        //    return meter.FrameArr[AddToLoopingIndex(offset)];
        //}
        ///// <summary>
        ///// Returns the meter index plus an offset. Handles looping. WARNING: Does not handle inputs < METER_LENGTH * -1.
        ///// </summary>
        //private int AddToLoopingIndex(int offset)
        //{ 
        //    return (_index + offset + METER_LENGTH) % METER_LENGTH;
        //}
    }
}
