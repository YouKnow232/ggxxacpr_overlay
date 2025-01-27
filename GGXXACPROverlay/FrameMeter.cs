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
            SlashBack,
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
            int actTimer = -1)
        {
            public readonly FrameType Type = type;
            public readonly FrameProperty1 Property = prop;
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

        public FrameMeter()
        {
            _index = 0;
            PlayerMeters[0] = new Meter("Player 1", METER_LENGTH);
            PlayerMeters[1] = new Meter("Player 2", METER_LENGTH);
            EntityMeters[0] = new Meter("P1 Sub", METER_LENGTH);
            EntityMeters[1] = new Meter("P2 Sub", METER_LENGTH);
            ClearMeters();
        }

        public void Update(GameState state)
        {
            // Check if meter should update
            // Skip update if both character haven't advanced a frame (TODO: Should update this logic after D3D hook update)
            if (state.Player1.AnimationCounter == FrameAtOffset(-1, 0).ActTimer &&
                state.Player2.AnimationCounter == FrameAtOffset(-1, 1).ActTimer)
            {
                if (state.Player1.AnimationCounter != FrameAtOffset(-1, 0).ActTimer ||
                    state.Player2.AnimationCounter != FrameAtOffset(-1, 1).ActTimer)
                {
                    Debug.WriteLine($"{state.Player1.AnimationCounter} != {FrameAtOffset(-1, 0).ActTimer} ||");
                    Debug.WriteLine($"{state.Player2.AnimationCounter} != {FrameAtOffset(-1, 1).ActTimer}");
                }
                return;
            }
            // Skip update when both players are in hitstop (currently redundant when checking for unchanged animation timers)
            if (state.Player1.HitstopCounter > 0 && state.Player2.HitstopCounter > 0) { return; }
            // Skip update if either player is frozen in super flash
            if (state.Player1.Status.Freeze || state.Player2.Status.Freeze) { return; }

            // Pause logic
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
            else if (player.CommandFlags.IsMove || player.Status.DisableHitboxes)
            {
                return FrameType.Startup;
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
                // If Jam
                if (players[index].CharId == 12 && players[index].GuardFlags.Parry2)
                {
                    // Jam parry flips her Parry2 flag for the rest of her current animation and uses a character specific counter for the active window
                    if (players[index].Extra.JamParryTime == 0xFF)
                    {
                        return FrameProperty1.Parry;
                    }
                }
                // If Axl
                else if (players[index].CharId == 5 && players[index].GuardFlags.Parry1)
                {
                    // Testing Mark property here. Seems to be necessary for Axl parry.
                    //  For some reason his parry is marked as a parry state for the full animation (despite being active 5F-17F in practice) and
                    //  uses some extra move properties to actually determine if the move should parry.
                    if (players[index].Mark == 0)
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
            Func<Entity, bool> OwnershipExpression = e => (e.Status.IsPlayer1 && index == 0) || (e.Status.IsPlayer2 && index == 1);

            // LINQ is so nice
            var ownerEntitiesHitboxes =
                //state.Entities.Where(e => e.ParentIndex == index && !e.Status.DisableHitboxes)
                state.Entities.Where(e => OwnershipExpression(e) && !e.Status.DisableHitboxes)
                              .SelectMany(e => e.HitboxSet)
                              .Where(h => h.BoxTypeId == BoxId.HIT);

            var ownerEntityHurtboxes =
                //state.Entities.Where(e => e.ParentIndex == index && !e.Status.DisableHurtboxes)
                state.Entities.Where(e => OwnershipExpression(e) && !e.Status.DisableHitboxes)
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
            FrameProperty1 prop = DetermineFrameProperty1(state, index);
            FrameProperty2 prop2 = FrameProperty2.Default;

            if (players[index].Extra.RCTime > 0)
            {
                prop2 = FrameProperty2.FRC;
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

        // TODO: proj startup, startup logic
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
