using System.Diagnostics;
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
            Recovery,
            BlockStun,
            HitStun
        }

        /// <summary>
        /// Drawn on bottom of frame meter pip
        /// </summary>
        public enum FrameProperty1
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
        public enum FrameProperty2
        {
            Default,
            FRC
        }

        public readonly struct Frame(
            FrameType type = FrameType.None,
            FrameProperty1 prop = FrameProperty1.Default,
            FrameProperty2 prop2 = FrameProperty2.Default,
            int actId = -1,
            int actTimer = -1,
            byte hitStop = 0,
            uint status = 0)
        {
            public readonly FrameType Type = type;
            public readonly FrameProperty1 Property = prop;
            public readonly FrameProperty2 Property2 = prop2;
            public readonly int ActId = actId;
            public readonly int ActTimer = actTimer;
            public readonly byte HitStop = hitStop;
            public readonly ActionStateFlags Status = status;
        }

        private const int PAUSE_THRESHOLD = 10;
        private const int METER_LENGTH = 100;

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

        public FrameMeter()
        {
            _index = 0;
            PlayerMeters[0] = new Meter("Player 1", METER_LENGTH);
            PlayerMeters[1] = new Meter("Player 2", METER_LENGTH);
            EntityMeters[0] = new Meter("P1 Sub", METER_LENGTH);
            EntityMeters[1] = new Meter("P2 Sub", METER_LENGTH);
            ClearMeters();
        }

        public int Update(GameState state)
        {
            Frame p1PrevFrame = FrameAtOffset(PlayerMeters[0], -1);
            Frame p2PrevFrame = FrameAtOffset(PlayerMeters[1], -1);

            // !! Very hacky discard update checks below. These are temp solutions to
            //  mitigate syncing issues while gamestate update hooks are being worked on.

            // BoxIter is a game state evaluation iterable in the game code. It will only ever not be 255 when the game is evaluating hitboxes.
            //  Although not a perfect safe guard, this should lessen incorrect frames due to mid-update game state reads until hooks are implemented.
            if (state.Player1.BoxIter != 255 || state.Player2.BoxIter != 255)
            {
                Debug.WriteLine($"Mid update read detected. P1:{state.Player1.BoxIter}, P2:{state.Player2.BoxIter}");
                return 1;
            }
            // Skip update if both character haven't advanced a frame (TODO: Should update this logic after D3D hook update)
            // This should handle double frame reads as well as pausing
            if (state.Player1.AnimationCounter == prevState?.Player1.AnimationCounter &&
                state.Player2.AnimationCounter == prevState?.Player2.AnimationCounter &&
                state.Player1.HitstopCounter == prevState?.Player1.HitstopCounter &&
                state.Player2.HitstopCounter == prevState?.Player2.HitstopCounter)
            {
                return 0;
            }
            // Skip update if either player is frozen in super flash while the opponent doesn't have an active hitbox.
            // The hitbox requirement is for moves that become active while in super flash.
            // Special exception for first freeze frame.
            if ((state.Player1.Status.Freeze || state.Player2.Status.Freeze) &&
                (p1PrevFrame.Status.Freeze || p2PrevFrame.Status.Freeze))
            {
                // Special case for super's that connect during super flash (e.g. Jam 632146S)
                // Rewrite the previous frame to an active frame and recalculate startup
                if (state.Player2.Status.Freeze && state.Player1.HasActiveFrame())
                {
                    PlayerMeters[0].FrameArr[AddToLoopingIndex(-1)] = new Frame(
                        FrameType.Active, p1PrevFrame.Property, p1PrevFrame.Property2, p1PrevFrame.ActId,
                        p1PrevFrame.ActTimer, p1PrevFrame.HitStop, (uint)p1PrevFrame.Status);

                    _index = AddToLoopingIndex(-1);
                    UpdateStartupByCountBackWithMoveData(state.Player1, ref PlayerMeters[0], EntityMeters[0]);
                    _index = AddToLoopingIndex(1);
                }
                else if (state.Player1.Status.Freeze && state.Player2.HasActiveFrame())
                {
                    PlayerMeters[1].FrameArr[AddToLoopingIndex(-1)] = new Frame(
                        FrameType.Active, p2PrevFrame.Property, p2PrevFrame.Property2, p2PrevFrame.ActId,
                        p2PrevFrame.ActTimer, p2PrevFrame.HitStop, (uint)p2PrevFrame.Status);

                    _index = AddToLoopingIndex(-1);
                    UpdateStartupByCountBackWithMoveData(state.Player2, ref PlayerMeters[1], EntityMeters[1]);
                    _index = AddToLoopingIndex(1);
                }

                prevState = state;
                return 0;
            }
            // Skip update when both players are in hitstop (currently somewhat redundant when checking for unchanged animation timers above)
            // Special exception to also skip the first frame after hitstop counters have ended
            if (state.Player1.HitstopCounter > 0 && state.Player2.HitstopCounter > 0 ||
                    prevState?.Player1.HitstopCounter > 0 && prevState?.Player2.HitstopCounter > 0)
            {
                prevState = state;
                return 0;
            }


            // Pause logic
            if (_isPaused)
            {
                if ((DetermineFrameType(state, 0) == FrameType.Neutral) &&
                    (DetermineFrameType(state, 1) == FrameType.Neutral) &&
                    (DetermineEntityFrameType(state, 0) == FrameType.None) &&
                    (DetermineEntityFrameType(state, 1) == FrameType.None))
                {
                    prevState = state;
                    return 0;
                }
                else
                {
                    _isPaused = false;
                    _index = 0;
                    ClearMeters();
                }
            }

            // Update each meter
            UpdateIndividualMeter(state, 0);
            UpdateIndividualMeter(state, 1);
            UpdateIndividualEntityMeter(state, 0);
            UpdateIndividualEntityMeter(state, 1);

            // Check if frame meter should pause
            // TODO: account for frame properties?
            _isPaused = true;
            FrameType p1FrameType, p2FrameType;
            for (int i = 0; i < PAUSE_THRESHOLD; i++)
            {
                p1FrameType = FrameAtOffset(PlayerMeters[0], -i).Type;
                p2FrameType = FrameAtOffset(PlayerMeters[1], -i).Type;

                if ((p1FrameType != FrameType.Neutral && p1FrameType != FrameType.None) ||
                    (p2FrameType != FrameType.Neutral && p2FrameType != FrameType.None) ||
                    FrameAtOffset(EntityMeters[0], -i).Type != FrameType.None ||
                    FrameAtOffset(EntityMeters[1], -i).Type != FrameType.None)
                {
                    _isPaused = false;
                    break;
                }
            }

            // Labels
            //UpdateStartupByAnimCounter(state.Player1, ref PlayerMeters[0], EntityMeters[0]);
            //UpdateStartupByAnimCounter(state.Player2, ref PlayerMeters[1], EntityMeters[1]);
            UpdateStartupByCountBackWithMoveData(state.Player1, ref PlayerMeters[0], EntityMeters[0]);
            UpdateStartupByCountBackWithMoveData(state.Player2, ref PlayerMeters[1], EntityMeters[1]);
            UpdateAdvantageByCountBack();

            _index = (_index + 1) % METER_LENGTH;
            prevState = state;
            return 0;
        }

        private void ClearMeters()
        {
            ClearMeter(ref PlayerMeters[0], false);
            ClearMeter(ref PlayerMeters[1], false);
            ClearMeter(ref EntityMeters[0], true);
            ClearMeter(ref EntityMeters[1], true);
        }
        private static void ClearMeter(ref Meter m, bool hide)
        {
            for(int i = 0; i < METER_LENGTH; i++)
            {
                m.FrameArr[i] = new Frame();
            }
            m.Startup = -1;
            m.LastAttackActId = -1;
            m.Advantage = -1;
            m.Total = -1;
            m.DisplayAdvantage = false;
            m.Hide = hide;
        }

        private static FrameType DetermineFrameType(GameState state, int index)
        {
            var player = index == 0 ? state.Player1 : state.Player2;

            if (player.HitboxSet.Any(h => h.BoxTypeId == BoxId.HIT) && !player.Status.DisableHitboxes)
            {
                return FrameType.Active;
            }
            else if (player.Status.IsInBlockstun || player.Extra.SBTime > 0)
            {
                return FrameType.BlockStun;
            }
            else if (player.Status.IsInHitstun)
            {
                return FrameType.HitStun;
            }
            else if (player.AttackFlags.IsInRecovery)
            {
                return FrameType.Recovery;
            }
            else if (player.CommandFlags.IsMove && !player.AttackFlags.IsInRecovery)
            {
                return FrameType.CounterHitState;
            }
            else if (player.CommandFlags.IsMove)
            {
                return FrameType.Startup;
            }
            else if (player.CommandFlags.Prejump ||
                player.CommandFlags.FreeCancel || player.CommandFlags.RunDash ||
                player.CommandFlags.StepDash || player.CommandFlags.RunDashSkid)
            {
                return FrameType.Movement;
            }
            else
            {
                return FrameType.Neutral;
            }
        }

        private static FrameProperty1 DetermineFrameProperty1(GameState state, int index)
        {
            Player[] players = [state.Player1, state.Player2];

            if (players[index].Extra.SBTime > 0)
            {
                return FrameProperty1.SlashBack;
            }
            else if ((players[index].Status.DisableHurtboxes ||
                    players[index].Status.StrikeInvuln ||
                    players[index].Extra.InvulnCounter > 0 ||
                    !players[index].HitboxSet.Any((Hitbox h) => h.BoxTypeId == BoxId.HURT)
                ) && (players[index].Status.IsThrowInuvln ||
                    players[index].Extra.ThrowProtectionTimer > 0))
            {
                return FrameProperty1.InvulnFull;
            }
            else if (players[index].Status.IsThrowInuvln ||
                (players[index].Extra.ThrowProtectionTimer > 0 &&
                    !(players[index].Status.IsInHitstun || players[index].Status.IsInBlockstun)))
            {
                return FrameProperty1.InvulnThrow;
            }
            else if (players[index].Status.DisableHurtboxes ||
                    players[index].Status.StrikeInvuln ||
                    players[index].Extra.InvulnCounter > 0 ||
                    !players[index].HitboxSet.Any((Hitbox h) => h.BoxTypeId == BoxId.HURT))
            {
                return FrameProperty1.InvulnStrike;
            }
            else if (players[index].GuardFlags.Armor)
            {
                return FrameProperty1.Armor;
            }
            else if (players[index].GuardFlags.Parry1 || players[index].GuardFlags.Parry2)
            {
                if (players[index].CharId == (int)CharacterID.JAM && players[index].GuardFlags.Parry2)
                {
                    // Jam parry flips her Parry2 flag for the rest of her current animation and uses a character specific counter for the active window
                    if (players[index].Extra.JamParryTime == 0xFF)
                    {
                        return FrameProperty1.Parry;
                    }
                }
                else if (players[index].CharId == (int)CharacterID.AXL && players[index].GuardFlags.Parry1)
                {
                    // Testing Mark property here. Seems to be necessary for Axl parry.
                    //  For some reason his parry is marked as a parry state for the full animation (despite being active 5F-17F in practice) and
                    //  uses some extra move properties (Player.Mark) to actually determine if the move should parry.
                    if (players[index].Mark == 1)
                    {
                        return FrameProperty1.Parry;
                    }
                }
                else
                {
                    return FrameProperty1.Parry;
                }
            }
            else if (players[index].GuardFlags.GuardPoint)
            {
                if (players[index].GuardFlags.IsStandBlocking && players[index].GuardFlags.IsCrouchBlocking)
                {
                    return FrameProperty1.GuardPointFull;
                }
                else if (players[index].GuardFlags.IsStandBlocking)
                {
                    return FrameProperty1.GuardPointHigh;
                }
                else if (players[index].GuardFlags.IsCrouchBlocking)
                {
                    return FrameProperty1.GuardPointLow;
                }
            }

            return FrameProperty1.Default;
        }

        private static FrameType DetermineEntityFrameType(GameState state, int index)
        {
            // LINQ is so nice
            var ownerEntitiesHitboxes =
                state.Entities.Where(e => IsOwnedBy(e, index) && !e.Status.DisableHitboxes)
                              .SelectMany(e => e.HitboxSet)
                              .Where(h => h.BoxTypeId == BoxId.HIT);

            var ownerEntityHurtboxes =
                state.Entities.Where(e => IsOwnedBy(e, index) && !e.Status.DisableHurtboxes)
                              .SelectMany(e => e.HitboxSet)
                              .Where(h => h.BoxTypeId == BoxId.HURT);

            if (ownerEntitiesHitboxes.ToArray().Length > 0)
            {
                return FrameType.Active;
            }
            else if (ownerEntityHurtboxes.ToArray().Length > 0)
            {
                return FrameType.Startup;
            }
            else
            {
                return FrameType.None;
            }
        }
        private static bool IsOwnedBy(Entity e, int playerIndex)
        {
            // Kinda hacky check for the Dizzy bubble exception (see comments on DIZZY_BUBBLE_ENTITY_ID),
            //  but the alternative is recursively pointer tracing e.ParentPtrRaw to a player pointer just because of this one exception.
            if (e.Id == GGXXACPR.GGXXACPR.DIZZY_ENTITY_ID && e.Status.IgnoreHitEffectsRecieved)
            {
                return e.Status.IsPlayer1 && playerIndex == 1 || e.Status.IsPlayer2 && playerIndex == 0;
            }
            else
            {
                return e.Status.IsPlayer1 && playerIndex == 0 || e.Status.IsPlayer2 && playerIndex == 1;
            }
        }

        private void UpdateIndividualMeter(GameState state, int index)
        {
            Player[] players = [state.Player1, state.Player2];

            FrameType type = DetermineFrameType(state, index);
            FrameProperty1 prop = DetermineFrameProperty1(state, index);
            FrameProperty2 prop2 = FrameProperty2.Default;

            if (players[index].Extra.RCTime > 0)
            {
                prop2 = FrameProperty2.FRC;
            }

            PlayerMeters[index].FrameArr[_index] = new Frame(type, prop, prop2,
                players[index].ActionId, players[index].AnimationCounter, players[index].HitstopCounter, (uint)players[index].Status);
            PlayerMeters[index].FrameArr[(_index + 2 + METER_LENGTH) % METER_LENGTH] = new Frame(); // Forward erasure
        }

        private void UpdateIndividualEntityMeter(GameState state, int index)
        {
            FrameType type = DetermineEntityFrameType(state, index);

            EntityMeters[index].FrameArr[_index] = new Frame(type);
            EntityMeters[index].FrameArr[(_index + 2 + METER_LENGTH) % METER_LENGTH] = new Frame(); // Forward erasure

            // Update hide flag
            EntityMeters[index].Hide = !EntityMeters[index].FrameArr.Any((Frame f) => f.Type != FrameType.None);
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
        private void UpdateStartupByAnimCounter(Player p, ref Meter pMeter, Meter eMeter)
        {
            FrameType prevFrameType = FrameAtOffset(pMeter, -1).Type;
            if (FrameAtOffset(pMeter, 0).Type == FrameType.Active &&
                (prevFrameType == FrameType.CounterHitState || prevFrameType == FrameType.Startup))
            {
                pMeter.Startup = p.AnimationCounter;
            }
        }
        private void UpdateStartupByCountBackWithMoveData(Player p, ref Meter pMeter, Meter eMeter)
        {
            Frame currFrame = FrameAtOffset(pMeter, 0);
            FrameType prevFrameType = FrameAtOffset(pMeter, -1).Type;
            if (currFrame.Type == FrameType.Active &&
                (prevFrameType == FrameType.CounterHitState || prevFrameType == FrameType.Startup))
            {
                pMeter.LastAttackActId = currFrame.ActId;
                Frame frame;
                for (int i = 1; i < METER_LENGTH; i++)
                {
                    frame = FrameAtOffset(pMeter, -i);
                    if (frame.ActId != pMeter.LastAttackActId && !MoveData.IsPrevAnimSameMove(p.CharId, frame.ActId, pMeter.LastAttackActId))
                    {
                        pMeter.Startup = i;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the Frame object at the current index plus an offset. Handles array looping.
        /// </summary>
        private Frame FrameAtOffset(Meter meter, int offset)
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
    }
}
