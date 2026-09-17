using System;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Core.Sim;

/// <summary>1フレームの試合の進行（02-match-rules.mdの「1フレームの処理の順序」）。</summary>
public static class MatchSimulator
{
    public static MatchState CreateMatch(MatchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var half = context.Stage.StartDistance / Fix16.FromInt(2);
        var p1 = new PlayerState
        {
            Position = new Vec3Fix(Fix16.Zero - half, Fix16.Zero, Fix16.Zero),
            Facing = new Angle16(0),
            Health = context.Rules.InitialHealth,
            State = StateKind.Idle,
            CurrentMove = Limits.NoMove,
            Slot = 0,
        };
        var p2 = new PlayerState
        {
            Position = new Vec3Fix(half, Fix16.Zero, Fix16.Zero),
            Facing = new Angle16(32768),
            Health = context.Rules.InitialHealth,
            State = StateKind.Idle,
            CurrentMove = Limits.NoMove,
            Slot = 1,
        };

        var state = default(MatchState);
        state = state.WithPlayer(0, p1).WithPlayer(1, p2);
        state.RngState = context.Rules.Seed;
        state.RoundNumber = 1;
        state.Phase = RoundPhase.Intro;
        state.RoundTimerFrames = (ushort)(context.Rules.RoundTimeSeconds * Limits.FramesPerSecond);
        state.CameraYaw = new Angle16(0);
        return state;
    }

    public static MatchState Step(in MatchState state, InputFrame p1Input, InputFrame p2Input, MatchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!p1Input.IsNormalized || !p2Input.IsNormalized)
        {
            throw new ArgumentException("入力は正規化されている必要があります。");
        }

        var result = state;

        // 1. 入力の保存
        result = result.WithPlayer(0, result.GetPlayer(0) with { Inputs = result.GetPlayer(0).Inputs.Push(p1Input), LastHitKind = HitKind.None });
        result = result.WithPlayer(1, result.GetPlayer(1) with { Inputs = result.GetPlayer(1).Inputs.Push(p2Input), LastHitKind = HitKind.None });

        // 2. 段階の進行
        result = AdvancePhase(result, context);

        if (result.Phase != RoundPhase.Fight)
        {
            return AdvanceFrameNumber(result, context);
        }

        var onLeftSide0 = CameraSide.IsP1OnLeft(result.GetPlayer(0).Position, result.GetPlayer(1).Position, result.CameraYaw);

        // 3. ヒットストップ
        (result, var stopped) = ApplyHitstop(result, context, onLeftSide0);

        // 4. 状態の経過
        result = ForEachActivePlayer(result, stopped, onLeftSide0, AdvanceState, context);
        result = AdvanceThrow(result, context, onLeftSide0);

        // 5. 行動の判断
        result = ForEachActivePlayer(result, stopped, onLeftSide0, ChooseAction, context);

        // 6. 移動
        var positionsBeforeMovement = (result.GetPlayer(0).Position, result.GetPlayer(1).Position);
        result = ForEachActivePlayer(result, stopped, onLeftSide0, (s, i, c, _) => ApplyMovement(s, i, c), context);

        // 7. 体の押し合いと壁
        result = ApplyBodyPush(result, context, positionsBeforeMovement, onLeftSide0);

        // 8. 向きの更新
        result = ForEachActivePlayer(result, stopped, onLeftSide0, (s, i, c, _) => ApplyFacing(s, i, c), context);

        // 9. 画面の左右
        result.CameraYaw = CameraSide.Update(result.GetPlayer(0).Position, result.GetPlayer(1).Position, result.CameraYaw);
        var onLeftSideAfter = CameraSide.IsP1OnLeft(result.GetPlayer(0).Position, result.GetPlayer(1).Position, result.CameraYaw);

        // 10と11. 投げの判定、打撃の判定と適用、投げの成立
        result = ResolveCombat(result, context, stopped, onLeftSideAfter);

        // 12. 決着
        result = ResolveRoundEnd(result, context);

        // 13. フレームの更新
        return AdvanceFrameNumber(result, context);
    }

    private static bool OnLeftSide(int slot, bool p1OnLeft) => slot == 0 ? p1OnLeft : !p1OnLeft;

    private static (MatchState State, bool[] Stopped) ApplyHitstop(MatchState state, MatchContext context, bool onLeftSide0)
    {
        var stopped = new bool[Limits.PlayerCount];
        for (var i = 0; i < Limits.PlayerCount; i++)
        {
            var player = state.GetPlayer(i);
            if (player.HitstopFrames > 0)
            {
                state = state.WithPlayer(i, player with { HitstopFrames = (byte)(player.HitstopFrames - 1) });
                stopped[i] = true;
            }
        }

        for (var i = 0; i < Limits.PlayerCount; i++)
        {
            if (stopped[i])
            {
                state = ProcessStoppedDownPlayer(state, i, context, OnLeftSide(i, onLeftSide0));
            }
        }

        return (state, stopped);
    }

    private static MatchState ForEachActivePlayer(
        MatchState state, bool[] stopped, bool onLeftSide0, Func<MatchState, int, MatchContext, bool, MatchState> action, MatchContext context)
    {
        for (var i = 0; i < Limits.PlayerCount; i++)
        {
            if (!stopped[i])
            {
                state = action(state, i, context, OnLeftSide(i, onLeftSide0));
            }
        }

        return state;
    }

    private static MatchState AdvancePhase(in MatchState state, MatchContext context) => state.Phase switch
    {
        RoundPhase.Intro => AdvanceIntroPhase(state, context),
        RoundPhase.RoundEnd => AdvanceRoundEndPhase(state, context),
        RoundPhase.MatchEnd => AdvanceMatchEndPhase(state),
        _ => state,
    };

    private static MatchState AdvanceIntroPhase(MatchState result, MatchContext context)
    {
        result.PhaseFrames++;
        if (result.PhaseFrames >= context.Rules.IntroFrames)
        {
            result.Phase = RoundPhase.Fight;
            result.PhaseFrames = 0;
        }

        return result;
    }

    private static MatchState AdvanceRoundEndPhase(MatchState result, MatchContext context)
    {
        result.PhaseFrames++;
        if (result.PhaseFrames < context.Rules.RoundEndFrames)
        {
            return result;
        }

        var matchOver = RoundResolver.IsMatchOver(
            result.GetRoundWins(0), result.GetRoundWins(1), result.RoundNumber, context.Rules.RoundsToWin, context.Rules.MaxRounds);
        if (matchOver)
        {
            result.Phase = RoundPhase.MatchEnd;
            result.PhaseFrames = 0;
            return result;
        }

        result = ResetPositions(result, context);
        result.RoundNumber++;
        result.Phase = RoundPhase.Intro;
        result.PhaseFrames = 0;
        return result;
    }

    private static MatchState AdvanceMatchEndPhase(MatchState result)
    {
        if (result.PhaseFrames < ushort.MaxValue)
        {
            result.PhaseFrames++;
        }

        return result;
    }

    /// <summary>2人の位置、体力、状態を試合開始時に戻す（トレーニングの位置のリセットで使う）。</summary>
    public static MatchState ResetPositions(in MatchState state, MatchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var half = context.Stage.StartDistance / Fix16.FromInt(2);
        var result = state;
        result = result.WithPlayer(0, result.GetPlayer(0) with
        {
            Position = new Vec3Fix(Fix16.Zero - half, Fix16.Zero, Fix16.Zero),
            Velocity = default,
            Facing = new Angle16(0),
            Health = context.Rules.InitialHealth,
            State = StateKind.Idle,
            CurrentMove = Limits.NoMove,
            StateFrame = 0,
            StunFrames = 0,
            HitstopFrames = 0,
            Flags = PlayerFlags.None,
        });
        result = result.WithPlayer(1, result.GetPlayer(1) with
        {
            Position = new Vec3Fix(half, Fix16.Zero, Fix16.Zero),
            Velocity = default,
            Facing = new Angle16(32768),
            Health = context.Rules.InitialHealth,
            State = StateKind.Idle,
            CurrentMove = Limits.NoMove,
            StateFrame = 0,
            StunFrames = 0,
            HitstopFrames = 0,
            Flags = PlayerFlags.None,
        });
        result.CameraYaw = new Angle16(0);
        result.RoundTimerFrames = (ushort)(context.Rules.RoundTimeSeconds * Limits.FramesPerSecond);
        return result;
    }

    private static MatchState ProcessStoppedDownPlayer(in MatchState state, int index, MatchContext context, bool onLeftSide)
    {
        var player = state.GetPlayer(index);
        if (player.State != StateKind.Down)
        {
            return state;
        }

        if (WakeUp.IsTechInputThisFrame(player.Inputs, onLeftSide) && player.StateFrame <= context.Rules.TechFrames)
        {
            player = player with { Flags = player.Flags | PlayerFlags.TechQueued };
        }

        return state.WithPlayer(index, player);
    }

    private static MatchState AdvanceState(MatchState state, int index, MatchContext context, bool onLeftSide)
    {
        var player = state.GetPlayer(index);
        if (player.StateFrame < ushort.MaxValue)
        {
            player = player with { StateFrame = (ushort)(player.StateFrame + 1) };
        }

        if (player.StunFrames > 0)
        {
            player = player with { StunFrames = (ushort)(player.StunFrames - 1) };
        }

        if (player.State == StateKind.Down)
        {
            if (WakeUp.IsTechInputThisFrame(player.Inputs, onLeftSide) && player.StateFrame <= context.Rules.TechFrames)
            {
                player = player with { Flags = player.Flags | PlayerFlags.TechQueued };
            }
        }

        player = ApplyEndConditions(player, index, context, onLeftSide);

        state = state.WithPlayer(index, player);
        return state;
    }

    private static PlayerState ApplyEndConditions(PlayerState player, int index, MatchContext context, bool onLeftSide)
    {
        var character = context.Characters[index];
        return player.State switch
        {
            StateKind.SidestepIn or StateKind.SidestepOut => EndAfterFrames(player, character.SidestepFrames),
            StateKind.Dash => EndAfterFrames(player, character.DashFrames),
            StateKind.Backdash => EndAfterFrames(player, character.BackdashFrames),
            StateKind.Attack => EndAttack(player, character),
            StateKind.ThrowEscape => EndAfterFrames(player, context.Rules.ThrowEscapeRecoveryFrames),
            StateKind.Blockstun or StateKind.CrouchBlockstun or StateKind.Hitstun or StateKind.WallStun => EndStun(player),
            StateKind.Tech => EndAfterFrames(player, context.Rules.TechRecoveryFrames),
            StateKind.RollIn or StateKind.RollOut => EndRoll(player, character.RollFrames),
            StateKind.Rise => EndAfterFrames(player, context.Rules.RiseFrames),
            StateKind.Down => AdvanceDown(player, context, onLeftSide),
            _ => player,
        };
    }

    private static PlayerState EndAfterFrames(PlayerState player, ushort frames) =>
        player.StateFrame >= frames ? ToActionable(player) : player;

    private static PlayerState EndAttack(PlayerState player, CharacterData character)
    {
        if (player.CurrentMove == Limits.NoMove)
        {
            return player;
        }

        var move = character.Moves[player.CurrentMove];
        return player.StateFrame >= move.Startup + move.Active + move.Recovery
            ? ToActionable(player) with { CurrentMove = Limits.NoMove }
            : player;
    }

    private static PlayerState EndStun(PlayerState player) =>
        player.StunFrames == 0 ? ToActionable(player) with { Flags = player.Flags & ~PlayerFlags.WallHitTaken } : player;

    private static PlayerState EndRoll(PlayerState player, ushort rollFrames) =>
        player.StateFrame >= rollFrames ? player with { State = StateKind.Rise, StateFrame = 0 } : player;

    private static PlayerState ToActionable(PlayerState player) =>
        player with { State = StateKind.Idle, StateFrame = 0, Flags = player.Flags & ~PlayerFlags.DownHitTaken & ~PlayerFlags.ThrowEscapeTried };

    private static PlayerState AdvanceDown(PlayerState player, MatchContext context, bool onLeftSide)
    {
        if (player.Flags.HasFlag(PlayerFlags.TechQueued))
        {
            var digit = CommandParser.Digit(player.Inputs.At(0), onLeftSide);
            var state = digit == 8 ? StateKind.RollIn : digit is 1 or 2 or 3 ? StateKind.RollOut : StateKind.Tech;
            return player with { State = state, StateFrame = 0, Flags = player.Flags & ~PlayerFlags.TechQueued };
        }

        if (player.StateFrame < context.Rules.DownMinFrames)
        {
            return player;
        }

        var decision = WakeUp.Decide(player, player.Inputs, onLeftSide, RisingAttacks(context, player), context.Rules);
        return decision.Action switch
        {
            WakeUpAction.RollIn => player with { State = StateKind.RollIn, StateFrame = 0 },
            WakeUpAction.RollOut => player with { State = StateKind.RollOut, StateFrame = 0 },
            WakeUpAction.RisingAttack => player with
            {
                State = StateKind.Attack,
                StateFrame = 0,
                CurrentMove = (byte)decision.MoveIndex,
                Flags = player.Flags & ~PlayerFlags.HasHitThisMove,
            },
            WakeUpAction.Rise => player with { State = StateKind.Rise, StateFrame = 0 },
            _ => player,
        };
    }

    private static System.Collections.Generic.IReadOnlyList<MoveData> RisingAttacks(MatchContext context, PlayerState player)
    {
        var slot = player.Slot;
        return context.Characters[slot].Moves;
    }

    private static bool CanChooseAction(StateKind state, ushort stateFrame, CharacterData character) => state switch
    {
        StateKind.Idle or StateKind.Walk or StateKind.BackWalk or StateKind.Crouch or StateKind.Guard or StateKind.CrouchGuard => true,
        StateKind.SidestepIn or StateKind.SidestepOut => stateFrame >= character.SidestepCancelFrame,
        StateKind.Dash or StateKind.Backdash => false,
        _ => false,
    };

    private static MatchState ChooseAction(MatchState state, int index, MatchContext context, bool onLeftSide)
    {
        var player = state.GetPlayer(index);
        var character = context.Characters[index];
        var enteredActionableThisFrame = player.StateFrame == 0
            && player.State is StateKind.Idle or StateKind.Walk or StateKind.BackWalk or StateKind.Crouch or StateKind.Guard or StateKind.CrouchGuard;
        var atCancelPoint = player.State is StateKind.SidestepIn or StateKind.SidestepOut && player.StateFrame == character.SidestepCancelFrame;

        if (!CanChooseAction(player.State, player.StateFrame, character))
        {
            return state;
        }

        var buffered = enteredActionableThisFrame || atCancelPoint;

        if (SidestepInput.TryEnterIn(player.Inputs, onLeftSide))
        {
            return state.WithPlayer(index, player with { State = StateKind.SidestepIn, StateFrame = 0 });
        }

        if (SidestepInput.TryEnterOut(player.Inputs, onLeftSide))
        {
            return state.WithPlayer(index, player with { State = StateKind.SidestepOut, StateFrame = 0 });
        }

        var decision = ActionSelector.Select(player.Inputs, onLeftSide, character.Moves, buffered);
        return state.WithPlayer(index, ApplyActionDecision(player, decision));
    }

    private static PlayerState ApplyActionDecision(PlayerState player, ActionDecision decision) => decision.Action switch
    {
        SelectedAction.Move => player with
        {
            State = StateKind.Attack,
            StateFrame = 0,
            CurrentMove = (byte)decision.MoveIndex,
            Flags = player.Flags & ~PlayerFlags.HasHitThisMove,
        },
        SelectedAction.DashForward => player with { State = StateKind.Dash, StateFrame = 0 },
        SelectedAction.DashBack => player with { State = StateKind.Backdash, StateFrame = 0 },
        SelectedAction.Guard => EnterPersistentState(player, StateKind.Guard),
        SelectedAction.CrouchGuard => EnterPersistentState(player, StateKind.CrouchGuard),
        SelectedAction.Crouch => EnterPersistentState(player, StateKind.Crouch),
        SelectedAction.WalkForward => EnterPersistentState(player, StateKind.Walk),
        SelectedAction.WalkBack => EnterPersistentState(player, StateKind.BackWalk),
        SelectedAction.Idle => EnterPersistentState(player, StateKind.Idle),
        _ => player,
    };

    /// <summary>すでに同じ状態ならStateFrameを保ち、違う状態なら0から数え直す。</summary>
    private static PlayerState EnterPersistentState(PlayerState player, StateKind state) =>
        player with { State = state, StateFrame = player.State == state ? player.StateFrame : (ushort)0 };

    private static MatchState ApplyMovement(MatchState state, int index, MatchContext context)
    {
        var player = state.GetPlayer(index);
        var character = context.Characters[index];
        var onLeftSide = CameraSide.IsP1OnLeft(state.GetPlayer(0).Position, state.GetPlayer(1).Position, state.CameraYaw);
        var side = OnLeftSide(index, onLeftSide);
        MoveData? currentMove = player.State == StateKind.Attack && player.CurrentMove != Limits.NoMove
            ? character.Moves[player.CurrentMove]
            : null;

        var velocity = Movement.ComputeVelocity(player, character, side, currentMove);
        var newPosition = PositionFinalizer.Finalize(player.Position + velocity, character.BodyRadius, context.Stage);
        return state.WithPlayer(index, player with { Velocity = velocity, Position = newPosition });
    }

    private static MatchState ApplyBodyPush(
        MatchState state, MatchContext context, (Vec3Fix P1, Vec3Fix P2) beforeMovement, bool onLeftSide0)
    {
        var p1 = state.GetPlayer(0);
        var p2 = state.GetPlayer(1);
        var eitherThrowing = p1.State is StateKind.Throwing or StateKind.Thrown || p2.State is StateKind.Throwing or StateKind.Thrown;

        var (newP1Pos, newP2Pos) = BodyPush.Resolve(
            beforeMovement.P1, beforeMovement.P2, p1.Position, p2.Position,
            context.Characters[0].BodyRadius, context.Characters[1].BodyRadius,
            state.CameraYaw, onLeftSide0, context.Stage, eitherThrowing);

        state = state.WithPlayer(0, p1 with { Position = newP1Pos });
        state = state.WithPlayer(1, p2 with { Position = newP2Pos });
        return state;
    }

    private static MatchState ApplyFacing(MatchState state, int index, MatchContext context)
    {
        var player = state.GetPlayer(index);
        var opponent = state.GetPlayer(index == 0 ? 1 : 0);
        MoveData? currentMove = player.State == StateKind.Attack && player.CurrentMove != Limits.NoMove
            ? context.Characters[index].Moves[player.CurrentMove]
            : null;

        var facing = Facing.Update(player, opponent.Position, player.Position, currentMove);
        return state.WithPlayer(index, player with { Facing = facing });
    }

    private static MatchState ResolveCombat(MatchState state, MatchContext context, bool[] stopped, bool onLeftSide0)
    {
        var p1 = state.GetPlayer(0);
        var p2 = state.GetPlayer(1);

        var p1HitsP2 = stopped[0] ? default : HitResolver.Detect(p1, context.Characters[0], p2, context.Characters[1]);
        var p2HitsP1 = stopped[1] ? default : HitResolver.Detect(p2, context.Characters[1], p1, context.Characters[0]);

        MoveData? throwMoveFor2 = null;
        MoveData? throwMoveFor1 = null;
        var throwsP2 = !stopped[0] && p2HitsP1.Kind == HitOutcomeKind.None
            && ThrowResolver.TryDetect(p1, context.Characters[0], p2, context.Characters[1], out throwMoveFor2);
        var throwsP1 = !stopped[1] && p1HitsP2.Kind == HitOutcomeKind.None
            && ThrowResolver.TryDetect(p2, context.Characters[1], p1, context.Characters[0], out throwMoveFor1);

        byte maxHitstop = 0;

        if (p1HitsP2.Kind != HitOutcomeKind.None)
        {
            (p2, p1) = ApplyHit(p1, p2, p1HitsP2, context);
            maxHitstop = MaxByte(maxHitstop, p1HitsP2.Move!.Hitstop);
        }

        if (p2HitsP1.Kind != HitOutcomeKind.None)
        {
            (p1, p2) = ApplyHit(p2, p1, p2HitsP1, context);
            maxHitstop = MaxByte(maxHitstop, p2HitsP1.Move!.Hitstop);
        }

        if (throwsP2 && throwsP1)
        {
            p1 = p1 with { State = StateKind.ThrowEscape, StateFrame = 0 };
            p2 = p2 with { State = StateKind.ThrowEscape, StateFrame = 0 };
        }
        else if (throwsP2)
        {
            (p1, p2) = ThrowSequence.Start(p1, p2);
            (p1, p2) = ApplyEscapeCheck(p1, p2, throwMoveFor2!, OnLeftSide(1, onLeftSide0), context);
        }
        else if (throwsP1)
        {
            (p2, p1) = ThrowSequence.Start(p2, p1);
            (p2, p1) = ApplyEscapeCheck(p2, p1, throwMoveFor1!, OnLeftSide(0, onLeftSide0), context);
        }

        if (maxHitstop > 0)
        {
            p1 = p1 with { HitstopFrames = MaxByte(p1.HitstopFrames, maxHitstop) };
            p2 = p2 with { HitstopFrames = MaxByte(p2.HitstopFrames, maxHitstop) };
        }

        state = state.WithPlayer(0, p1);
        state = state.WithPlayer(1, p2);
        return state;
    }

    private static byte MaxByte(byte a, byte b) => a > b ? a : b;

    /// <summary>投げの最中の進み。投げ抜けの確認を投げの終わりより先に行う。どちらの席が投げた側でも順序は同じにする。</summary>
    private static MatchState AdvanceThrow(MatchState state, MatchContext context, bool onLeftSide0)
    {
        var throwerIndex = state.GetPlayer(0).State == StateKind.Throwing ? 0 : state.GetPlayer(1).State == StateKind.Throwing ? 1 : -1;
        if (throwerIndex < 0)
        {
            return state;
        }

        var thrownIndex = 1 - throwerIndex;
        var thrower = state.GetPlayer(throwerIndex);
        var thrown = state.GetPlayer(thrownIndex);
        var move = context.Characters[throwerIndex].Moves[thrower.CurrentMove];

        (thrower, thrown) = ApplyEscapeCheck(thrower, thrown, move, OnLeftSide(thrownIndex, onLeftSide0), context);
        if (thrower.State == StateKind.Throwing && thrower.StateFrame >= context.Rules.ThrowEscapeFrames)
        {
            (thrower, thrown) = ThrowSequence.Finish(thrower, thrown, move, context);
        }

        return state.WithPlayer(throwerIndex, thrower).WithPlayer(thrownIndex, thrown);
    }

    private static (PlayerState Thrower, PlayerState Thrown) ApplyEscapeCheck(
        PlayerState thrower, PlayerState thrown, MoveData move, bool thrownOnLeftSide, MatchContext context)
    {
        var check = ThrowSequence.CheckEscape(thrown, thrownOnLeftSide, move);
        return check switch
        {
            ThrowEscapeCheck.Escaped => ThrowSequence.Escape(thrower, thrown, context),
            ThrowEscapeCheck.Tried => (thrower, thrown with { Flags = thrown.Flags | PlayerFlags.ThrowEscapeTried }),
            _ => (thrower, thrown),
        };
    }

    private static (PlayerState Defender, PlayerState Attacker) ApplyHit(PlayerState attacker, PlayerState defender, HitOutcome outcome, MatchContext context)
    {
        var move = outcome.Move!;
        attacker = attacker with { Flags = attacker.Flags | PlayerFlags.HasHitThisMove };

        defender = outcome.Kind switch
        {
            HitOutcomeKind.DownHit => ApplyDownHit(defender, move),
            HitOutcomeKind.Guarded => ApplyGuardedHit(attacker, defender, move, context),
            _ => ApplyDamagingHit(attacker, defender, move, outcome.Kind == HitOutcomeKind.CounterHit, context),
        };

        return (defender, attacker);
    }

    private static PlayerState ApplyDownHit(PlayerState defender, MoveData move) =>
        defender with { Health = defender.Health - move.Damage, Flags = defender.Flags | PlayerFlags.DownHitTaken, LastHitKind = HitKind.Hit };

    private static PlayerState ApplyGuardedHit(PlayerState attacker, PlayerState defender, MoveData move, MatchContext context)
    {
        var pushDirection = HorizontalUnit(defender.Position - attacker.Position, attacker.Facing);
        var pushed = defender.Position + (pushDirection * move.Pushback);
        var pushedFinal = PositionFinalizer.Finalize(pushed, context.Characters[defender.Slot].BodyRadius, context.Stage);
        var crouching = defender.State is StateKind.CrouchGuard or StateKind.CrouchBlockstun;
        return defender with
        {
            State = crouching ? StateKind.CrouchBlockstun : StateKind.Blockstun,
            StateFrame = 0,
            StunFrames = move.Blockstun,
            Position = pushedFinal,
            LastHitKind = HitKind.Guarded,
        };
    }

    private static PlayerState ApplyDamagingHit(PlayerState attacker, PlayerState defender, MoveData move, bool isCounter, MatchContext context)
    {
        var damage = isCounter ? move.CounterDamage : move.Damage;
        var knockdown = isCounter ? move.CounterKnockdown : move.Knockdown;
        var stun = isCounter ? move.CounterHitstun : move.Hitstun;

        var pushDirection = HorizontalUnit(defender.Position - attacker.Position, attacker.Facing);
        var pushedPos = defender.Position + (pushDirection * move.Pushback);
        var wallResult = WallStunResolver.Resolve(pushedPos, context.Characters[defender.Slot].BodyRadius, context.Stage, defender.Flags);
        var newHealth = defender.Health - damage;

        if (wallResult.BecameWallStun)
        {
            return defender with
            {
                Health = newHealth,
                State = StateKind.WallStun,
                StateFrame = 0,
                StunFrames = context.Rules.WallStunFrames,
                Position = wallResult.FinalPosition,
                Flags = wallResult.NewFlags,
                LastHitKind = isCounter ? HitKind.CounterWallHit : HitKind.WallHit,
            };
        }

        if (knockdown)
        {
            return defender with
            {
                Health = newHealth,
                State = StateKind.Down,
                StateFrame = 0,
                Position = wallResult.FinalPosition,
                Flags = wallResult.NewFlags & ~PlayerFlags.DownHitTaken & ~PlayerFlags.TechQueued,
                LastHitKind = isCounter ? HitKind.CounterHit : HitKind.Hit,
            };
        }

        return defender with
        {
            Health = newHealth,
            State = StateKind.Hitstun,
            StateFrame = 0,
            StunFrames = stun,
            Position = wallResult.FinalPosition,
            Flags = wallResult.NewFlags,
            LastHitKind = isCounter ? HitKind.CounterHit : HitKind.Hit,
        };
    }

    internal static Vec3Fix HorizontalUnit(Vec3Fix diff, Angle16 fallbackFacing)
    {
        var distSq = diff.HorizontalDistanceSquared(default);
        if (distSq == 0)
        {
            return Trig.Direction(fallbackFacing);
        }

        var dist = new Fix16((int)IntMath.Sqrt(distSq));
        return new Vec3Fix(diff.X / dist, Fix16.Zero, diff.Z / dist);
    }

    private static MatchState ResolveRoundEnd(MatchState state, MatchContext context) =>
        context.Rules.Training ? ResolveTrainingRound(state, context) : ResolveMatchRound(state, context);

    private static bool IsActionableForHealthReset(PlayerState player) =>
        player.StateFrame == 0
        && player.State is StateKind.Idle or StateKind.Walk or StateKind.BackWalk or StateKind.Crouch or StateKind.Guard or StateKind.CrouchGuard;

    private static MatchState ResolveTrainingRound(MatchState state, MatchContext context)
    {
        var p1 = state.GetPlayer(0);
        var p2 = state.GetPlayer(1);

        if (RingBounds.IsOut(p1.Position, context.Stage) || RingBounds.IsOut(p2.Position, context.Stage))
        {
            return ResetPositions(state, context);
        }

        if (IsActionableForHealthReset(p1) && p1.Health < context.Rules.InitialHealth)
        {
            state = state.WithPlayer(0, p1 with { Health = context.Rules.InitialHealth });
        }

        if (IsActionableForHealthReset(p2) && p2.Health < context.Rules.InitialHealth)
        {
            state = state.WithPlayer(1, p2 with { Health = context.Rules.InitialHealth });
        }

        return state;
    }

    private static MatchState ResolveMatchRound(MatchState state, MatchContext context)
    {
        var p1 = state.GetPlayer(0);
        var p2 = state.GetPlayer(1);
        var outcome = RoundResolver.CheckRoundEnd(p1, p2, context.Stage, state.RoundTimerFrames);
        if (!outcome.Decided)
        {
            return state;
        }

        if (outcome.P1Won)
        {
            state = state.WithRoundWins(0, (byte)(state.GetRoundWins(0) + 1));
        }

        if (outcome.P2Won)
        {
            state = state.WithRoundWins(1, (byte)(state.GetRoundWins(1) + 1));
        }

        state.LastRoundReason = outcome.Reason;
        state.LastRoundWinners = (byte)((outcome.P1Won ? 1 : 0) | (outcome.P2Won ? 2 : 0));
        state.Phase = RoundPhase.RoundEnd;
        state.PhaseFrames = 0;
        return state;
    }

    private static MatchState AdvanceFrameNumber(MatchState state, MatchContext context)
    {
        if (state.FrameNumber < uint.MaxValue)
        {
            state.FrameNumber++;
        }

        if (state.Phase == RoundPhase.Fight && !context.Rules.Training && state.RoundTimerFrames > 0)
        {
            state.RoundTimerFrames--;
        }

        return state;
    }
}
