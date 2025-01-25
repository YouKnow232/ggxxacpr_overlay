using System;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using GGXXACPROverlay.GGXXACPR;
using SharpDX.Direct2D1.Effects;

namespace GGXXACPROverlay
{
    internal class FrameMeter
    {
        public enum FrameType
        {
            None,
            Neutral,
            CounterHitState,
            Startup,
            Active,
            Recovery,
            BlockStun,
            HitStun
        }
        public enum FrameProperty
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
        public enum FrameProperty2
        {
            Default,
            FRC
        }
        public readonly struct Frame(
            FrameType type = FrameType.None,
            FrameProperty prop = FrameProperty.Default,
            FrameProperty2 prop2 = FrameProperty2.Default,
            int actTimer = -1)
        {
            public readonly FrameType Type = type;
            public readonly FrameProperty Property = prop;
            public readonly FrameProperty2 Property2 = prop2;
            public readonly int ActTimer = actTimer;
        }

        private static readonly int PAUSE_THRESHOLD = 10;
        private static readonly int METER_LENGTH = 100;
        public struct Meter(string name, int length)
        {
            public readonly string Label = name;
            public int Startup = -1;
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
        // DEBUG
        public Meter TestMeter;

        public FrameMeter()
        {
            _index = 0;
            PlayerMeters[0] = new Meter("Player 1", METER_LENGTH);
            PlayerMeters[1] = new Meter("Player 2", METER_LENGTH);
            EntityMeters[0] = new Meter("P1 Sub", METER_LENGTH);
            EntityMeters[1] = new Meter("P2 Sub", METER_LENGTH);
            // DEBUG
            TestMeter = new Meter("Test", METER_LENGTH);
            ClearMeters();
        }

        public void Update(GameState state)
        {
            // Check if meter should update
            if (state.Player1.AnimationCounter == FrameAtOffset(-1, 0).ActTimer &&
                state.Player2.AnimationCounter == FrameAtOffset(-1, 1).ActTimer)
            {
                if (state.Player1.AnimationCounter != FrameAtOffset(-1, 0).ActTimer ||
                    state.Player2.AnimationCounter != FrameAtOffset(-1, 1).ActTimer)
                {
                    Debug.WriteLine($"{state.Player1.AnimationCounter} != {FrameAtOffset(-1, 0).ActTimer} ||");
                    Debug.WriteLine($"{state.Player2.AnimationCounter} != {FrameAtOffset(-1, 1).ActTimer}");
                    // Mid update reads usually happen between player animation counter update and entity array fully updating
                    //Debug.WriteLine($"Mid Update Read!!!!! {state.Player1.BoxIter}/{state.Player2.BoxIter}");
                }
                //Debug.WriteLine($"Skipping frame double count. {tempSkipCounter}");
                return;
            }
            // Pause when both players are in hitstop (redunt when checking for unchanged animation timers)
            if (state.Player1.HitstopCounter > 0 && state.Player2.HitstopCounter > 0) { return; }
            if (state.Player1.Status.Freeze || state.Player2.Status.Freeze) { return; }
            if (_isPaused)
            {
                if ((DetermineFrameType(state, 0) == FrameType.Neutral) &&
                    (DetermineFrameType(state, 1) == FrameType.Neutral) &&
                    (DetermineEntityFrameType(state, 0) == FrameType.None) &&
                    (DetermineEntityFrameType(state, 1) == FrameType.None))
                {
                    return;
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
            // DEBUG
            UpdateTest(state);

            // Check if frame meter should pause
            // TODO: account for frame properties
            _isPaused = true;
            bool a, b, c, d, e, f;
            for (int i = 0; i < PAUSE_THRESHOLD; i++)
            {
                // TODO: clean this
                a = (PlayerMeters[0].FrameArr[(_index - i + METER_LENGTH) % METER_LENGTH].Type != FrameType.Neutral);
                b = (PlayerMeters[0].FrameArr[(_index - i + METER_LENGTH) % METER_LENGTH].Type != FrameType.None);
                c = (PlayerMeters[1].FrameArr[(_index - i + METER_LENGTH) % METER_LENGTH].Type != FrameType.Neutral);
                d = (PlayerMeters[1].FrameArr[(_index - i + METER_LENGTH) % METER_LENGTH].Type != FrameType.None);
                e = (EntityMeters[0].FrameArr[(_index - i + METER_LENGTH) % METER_LENGTH].Type != FrameType.None);
                f = (EntityMeters[1].FrameArr[(_index - i + METER_LENGTH) % METER_LENGTH].Type != FrameType.None);

                if (((a && b) || (c && d)) || e || f)
                {
                    _isPaused = false;
                    break;
                }
            }

            CalculateLabels(state);

            _index = (_index + 1) % METER_LENGTH;
        }

        private void ClearMeters()
        {
            for (int i = 0; i < METER_LENGTH; i++)
            {
                PlayerMeters[0].FrameArr[i] = new Frame();
                PlayerMeters[1].FrameArr[i] = new Frame();
                EntityMeters[0].FrameArr[i] = new Frame();
                EntityMeters[1].FrameArr[i] = new Frame();
                TestMeter.FrameArr[i] = new Frame();
            }
            PlayerMeters[0].Startup = -1;
            PlayerMeters[0].Advantage = -1;
            PlayerMeters[0].DisplayAdvantage = false;
            PlayerMeters[1].Startup = -1;
            PlayerMeters[1].Advantage = -1;
            PlayerMeters[1].DisplayAdvantage = false;
            EntityMeters[0].Hide = true;
            EntityMeters[1].Hide = true;
        }

        // DEBUG
        private void UpdateTest(GameState state)
        {
            FrameType type = FrameType.None;

            if (state.GlobalFlags.ThrowFlags.Player1ThrowActive && state.GlobalFlags.ThrowFlags.Player2ThrowActive)
            {
                type = FrameType.CounterHitState;
            }
            else if (state.GlobalFlags.ThrowFlags.Player2ThrowActive)
            {
                type = FrameType.Active;
            }
            else if (state.GlobalFlags.ThrowFlags.Player1ThrowActive)
            {
                type = FrameType.Recovery;
            }
            else if (state.Player1.BufferFlags.Unknown8)
            {
                type = FrameType.BlockStun;
            }

            TestMeter.FrameArr[_index] = new Frame(type, FrameProperty.Default, FrameProperty2.Default, state.Player1.AnimationCounter);
            TestMeter.FrameArr[(_index + 2 + METER_LENGTH) % METER_LENGTH] = new Frame(); // Forward erasure
        }

        private static FrameType DetermineFrameType(GameState state, int index)
        {
            var player = index == 0 ? state.Player1 : state.Player2;

            // Is active if there is a hitbox and either the player recovery flag is not set or the move is not in recovery and has connect
            //else if (player.HitboxSet.Any((Hitbox h) => h.BoxTypeId == BoxId.HIT) &&
            //    !player.AttackFlags.IsInRecovery &&
            //    (!player.Status.DisableHitboxes || player.AttackFlags.HasConnected) )
            if (player.HitboxSet.Any(h => h.BoxTypeId == BoxId.HIT) && !player.Status.DisableHitboxes)
            {
                return FrameType.Active;
            }
            // TODO: see what game state really ensures a throw. This flag is close, but can be overriden by other actions
            //else if ((state.GlobalFlags.ThrowFlags.Player1ThrowActive && index == 0) ||
            //    (state.GlobalFlags.ThrowFlags.Player2ThrowActive && index == 1))
            //{
            //    return FrameType.Active
            //}
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
            else if (player.CommandFlags.IsMove || player.Status.DisableHitboxes)
            {
                return FrameType.Startup;
            }
            else
            {
                return FrameType.Neutral;
            }
        }

        private static FrameType DetermineEntityFrameType(GameState state, int index)
        {
            // LINQ is so nice
            var ownerEntitiesHitboxes =
                state.Entities.Where(e => e.ParentIndex == index && !e.Status.DisableHitboxes)
                              .SelectMany(e => e.HitboxSet)
                              .Where(h => h.BoxTypeId == BoxId.HIT);

            var ownerEntityHurtboxes =
                state.Entities.Where(e => e.ParentIndex == index && !e.Status.DisableHurtboxes)
                              .SelectMany(e => e.HitboxSet)
                              .Where(h => h.BoxTypeId == BoxId.HURT);

            if (ownerEntitiesHitboxes.ToArray().Length > 0)
            {
                return FrameType.Active;
            }
            else if (ownerEntityHurtboxes.ToArray().Length > 0)
            {
                return FrameType.Startup;
            } else
            {
                return FrameType.None;
            }
        }

        private void UpdateIndividualMeter(GameState state, int index)
        {
            Player[] players = [state.Player1, state.Player2];

            FrameType type = DetermineFrameType(state, index);
            FrameProperty prop = FrameProperty.Default;
            FrameProperty2 prop2 = FrameProperty2.Default;

            if (players[index].Extra.RCTime > 0)
            {
                prop2 = FrameProperty2.FRC;
            }

            if (players[index].Extra.SBTime > 0)
            {
                prop = FrameProperty.SlashBack;
            }
            else if ((players[index].Status.DisableHurtboxes ||
                    players[index].Status.StrikeInvuln ||
                    players[index].Extra.InvulnCounter > 0 ||
                    !players[index].HitboxSet.Any((Hitbox h) => h.BoxTypeId == BoxId.HURT)
                ) && (players[index].Status.IsThrowInuvln ||
                    players[index].Extra.ThrowProtectionTimer > 0))
            {
                prop = FrameProperty.InvulnFull;
            }
            else if (players[index].Status.IsThrowInuvln ||
                (players[index].Extra.ThrowProtectionTimer > 0 &&
                    !(players[index].Status.IsInHitstun || players[index].Status.IsInBlockstun)))
            {
                prop = FrameProperty.InvulnThrow;
            }
            else if (players[index].Status.DisableHurtboxes ||
                    players[index].Status.StrikeInvuln ||
                    players[index].Extra.InvulnCounter > 0 ||
                    !players[index].HitboxSet.Any((Hitbox h) => h.BoxTypeId == BoxId.HURT))
            {
                prop = FrameProperty.InvulnStrike;
            }
            else if (players[index].GuardFlags.Armor)
            {
                prop = FrameProperty.Armor;
            }
            else if (players[index].GuardFlags.Parry)
            {
                prop = FrameProperty.Parry;
            }
            else if (players[index].GuardFlags.GuardPoint)
            {
                if (players[index].GuardFlags.IsStandBlocking && players[index].GuardFlags.IsCrouchBlocking)
                {
                    prop = FrameProperty.GuardPointFull;
                }
                else if (players[index].GuardFlags.IsStandBlocking)
                {
                    prop = FrameProperty.GuardPointHigh;
                }
                else if (players[index].GuardFlags.IsCrouchBlocking)
                {
                    prop = FrameProperty.GuardPointLow;
                }
            }

            PlayerMeters[index].FrameArr[_index] = new Frame(type, prop, prop2, players[index].AnimationCounter);
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

        // TODO: proj startup
        private void CalculateLabels(GameState state)
        {
            // Startup
            if (FrameAtOffset(0, 0).Type == FrameType.Active && FrameAtOffset(-1, 0).Type == FrameType.CounterHitState)
            {
                PlayerMeters[0].Startup = state.Player1.AnimationCounter;
            }
            if (FrameAtOffset(0, 1).Type == FrameType.Active && FrameAtOffset(-1, 1).Type == FrameType.CounterHitState)
            {
                PlayerMeters[1].Startup = state.Player2.AnimationCounter;
            }

            // Advantage TODO: clean redundancy
            if (FrameAtOffset(0,0).Type == FrameType.Neutral &&
                FrameAtOffset(-1, 0).Type != FrameType.Neutral &&
                FrameAtOffset(0, 1).Type == FrameType.Neutral)
            {
                for (int i = 1; i < METER_LENGTH; i++)
                {
                    if (FrameAtOffset(-i, 1).Type != FrameType.Neutral)
                    {
                        PlayerMeters[0].Advantage = 1 - i;
                        PlayerMeters[1].Advantage = i - 1;
                        PlayerMeters[0].DisplayAdvantage = true;
                        break;
                    }
                }
            }
            if (FrameAtOffset(0, 1).Type == FrameType.Neutral &&
                FrameAtOffset(-1, 1).Type != FrameType.Neutral &&
                FrameAtOffset(0, 0).Type == FrameType.Neutral)
            {
                for (int i = 1; i < METER_LENGTH; i++)
                {
                    if (FrameAtOffset(-i, 0).Type != FrameType.Neutral)
                    {
                        PlayerMeters[0].Advantage = i - 1;
                        PlayerMeters[1].Advantage = 1 - i;
                        PlayerMeters[0].DisplayAdvantage = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the Frame object at the current index plus an offset. Handles array looping.
        /// </summary>
        private Frame FrameAtOffset(int offset, int player)
        {
            if (player == 0)
            {
                return PlayerMeters[0].FrameArr[(_index + offset + METER_LENGTH) % METER_LENGTH];
            } else
            {
                return PlayerMeters[1].FrameArr[(_index + offset + METER_LENGTH) % METER_LENGTH];
            }
        }
    }
}
