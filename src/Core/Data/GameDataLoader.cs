using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Godot3dFighter.Core.Math;

namespace Godot3dFighter.Core.Data;

/// <summary>キャラ、ステージ、規則のJSONを読み込む（ADR-0004、ADR-0006、ADR-0017、ADR-0022）。</summary>
public static partial class GameDataLoader
{
    [GeneratedRegex(@"^[1-6]{0,4}[PKG](\+[PKG]){0,2}$")]
    private static partial Regex CommandPattern();

    public static CharacterData LoadCharacter(Stream stream) =>
        MapCharacter(Deserialize(stream, JsonContext.Default.CharacterDataDto));

    public static StageData LoadStage(Stream stream) =>
        MapStage(Deserialize(stream, JsonContext.Default.StageDataDto));

    public static Rules LoadRules(Stream stream) =>
        MapRules(Deserialize(stream, JsonContext.Default.RulesDto));

    private static T Deserialize<T>(Stream stream, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(stream);
        try
        {
            return JsonSerializer.Deserialize(stream, typeInfo)
                ?? throw new GameDataFormatException("JSONの内容が空です。");
        }
        catch (JsonException e)
        {
            throw new GameDataFormatException("JSONの形式が不正です。", e);
        }
    }

    private static CharacterData MapCharacter(CharacterDataDto dto)
    {
        RequireAtLeastOne(dto.DashFrames, nameof(dto.DashFrames));
        RequireAtLeastOne(dto.BackdashFrames, nameof(dto.BackdashFrames));
        RequireAtLeastOne(dto.SidestepFrames, nameof(dto.SidestepFrames));
        RequireAtLeastOne(dto.RollFrames, nameof(dto.RollFrames));

        if (dto.DashCancelFrames > dto.DashFrames)
        {
            throw new GameDataFormatException("dashCancelFramesはdashFrames以下が必要です。");
        }

        if (dto.SidestepCancelFrame > dto.SidestepFrames)
        {
            throw new GameDataFormatException("sidestepCancelFrameはsidestepFrames以下が必要です。");
        }

        RequireAtMost(dto.StandHurtCapsules.Count, Limits.MaxHurtCapsulesPerWindow, "standHurtCapsules");
        RequireAtMost(dto.CrouchHurtCapsules.Count, Limits.MaxHurtCapsulesPerWindow, "crouchHurtCapsules");
        RequireAtMost(dto.DownHurtCapsules.Count, Limits.MaxHurtCapsulesPerWindow, "downHurtCapsules");
        RequireAtMost(dto.Moves.Count, Limits.MaxMovesPerCharacter, "moves");

        var moves = new MoveData[dto.Moves.Count];
        for (var i = 0; i < dto.Moves.Count; i++)
        {
            moves[i] = MapMove(dto.Moves[i]);
        }

        return new CharacterData
        {
            Name = dto.Name,
            WalkSpeed = Fix16.FromDecimal(dto.WalkSpeed),
            BackWalkSpeed = Fix16.FromDecimal(dto.BackWalkSpeed),
            DashSpeed = Fix16.FromDecimal(dto.DashSpeed),
            DashFrames = dto.DashFrames,
            BackdashSpeed = Fix16.FromDecimal(dto.BackdashSpeed),
            BackdashFrames = dto.BackdashFrames,
            DashCancelFrames = dto.DashCancelFrames,
            SidestepSpeed = Fix16.FromDecimal(dto.SidestepSpeed),
            SidestepFrames = dto.SidestepFrames,
            SidestepCancelFrame = dto.SidestepCancelFrame,
            RollSpeed = Fix16.FromDecimal(dto.RollSpeed),
            RollFrames = dto.RollFrames,
            BodyRadius = Fix16.FromDecimal(dto.BodyRadius),
            StandHurtCapsules = MapCapsules(dto.StandHurtCapsules),
            CrouchHurtCapsules = MapCapsules(dto.CrouchHurtCapsules),
            DownHurtCapsules = MapCapsules(dto.DownHurtCapsules),
            Moves = moves,
        };
    }

    private static MoveData MapMove(MoveDataDto dto)
    {
        ValidateMoveBasics(dto);

        RequireAtMost(dto.Motion.Count, Limits.MaxMotionSegmentsPerMove, $"技'{dto.Name}'のmotion");
        RequireAtMost(dto.Windows.Count, Limits.MaxWindowsPerMove, $"技'{dto.Name}'のwindows");

        var (windows, hasThrowHit) = MapWindows(dto);
        var motion = MapMotion(dto);
        var (throwRange, throwSide, throwEndOffset) = MapThrowFields(dto, hasThrowHit);

        return new MoveData
        {
            Name = dto.Name,
            Command = dto.Command,
            Kind = dto.Kind,
            Posture = dto.Posture,
            Startup = dto.Startup,
            Active = dto.Active,
            Recovery = dto.Recovery,
            Tracking = dto.Tracking,
            Height = dto.Height,
            Damage = dto.Damage,
            CounterDamage = dto.CounterDamage,
            Hitstun = dto.Hitstun,
            CounterHitstun = dto.CounterHitstun,
            Blockstun = dto.Blockstun,
            Hitstop = dto.Hitstop,
            Knockdown = dto.Knockdown,
            CounterKnockdown = dto.CounterKnockdown,
            HitsDown = dto.HitsDown,
            Pushback = Fix16.FromDecimal(dto.Pushback),
            Motion = motion,
            Windows = windows,
            ThrowRange = throwRange,
            ThrowSide = throwSide,
            ThrowEndOffset = throwEndOffset,
        };
    }

    private static void ValidateMoveBasics(MoveDataDto dto)
    {
        if (!CommandPattern().IsMatch(dto.Command))
        {
            throw new GameDataFormatException($"技'{dto.Name}'のコマンド'{dto.Command}'が形に合いません。");
        }

        if (dto.Kind == MoveKind.RisingAttack && dto.Command is not ("P" or "K"))
        {
            throw new GameDataFormatException($"起き上がり攻撃'{dto.Name}'のコマンドはPかKだけです。");
        }

        if (dto.Tracking > dto.Startup)
        {
            throw new GameDataFormatException($"技'{dto.Name}'のtrackingはstartup以下が必要です。");
        }
    }

    private static (HitWindow[] Windows, bool HasThrowHit) MapWindows(MoveDataDto dto)
    {
        var activeEnd = dto.Startup + dto.Active - 1;
        var windows = new HitWindow[dto.Windows.Count];
        var previousTo = -1;
        var hasThrowHit = false;
        for (var i = 0; i < dto.Windows.Count; i++)
        {
            var w = dto.Windows[i];
            if (w.From > w.To)
            {
                throw new GameDataFormatException($"技'{dto.Name}'のwindows[{i}]はfromがtoより大きいです。");
            }

            if (w.From <= previousTo)
            {
                throw new GameDataFormatException($"技'{dto.Name}'のwindows[{i}]が前の区間と重なります。");
            }

            previousTo = w.To;

            RequireAtMost(w.Hit.Count, Limits.MaxHitCapsulesPerWindow, $"技'{dto.Name}'のwindows[{i}].hit");
            RequireAtMost(w.Hurt.Count, Limits.MaxHurtCapsulesPerWindow, $"技'{dto.Name}'のwindows[{i}].hurt");

            if (w.Hit.Count > 0)
            {
                hasThrowHit = true;
                if (w.From < dto.Startup || w.To > activeEnd)
                {
                    throw new GameDataFormatException(
                        $"技'{dto.Name}'のwindows[{i}]の攻撃判定がstartupからstartup+active-1の外にあります。");
                }
            }

            windows[i] = new HitWindow
            {
                From = w.From,
                To = w.To,
                Hit = MapCapsules(w.Hit),
                Hurt = MapCapsules(w.Hurt),
            };
        }

        return (windows, hasThrowHit);
    }

    private static MotionSegment[] MapMotion(MoveDataDto dto)
    {
        var motion = new MotionSegment[dto.Motion.Count];
        for (var i = 0; i < dto.Motion.Count; i++)
        {
            var m = dto.Motion[i];
            if (m.From > m.To)
            {
                throw new GameDataFormatException($"技'{dto.Name}'のmotion[{i}]はfromがtoより大きいです。");
            }

            motion[i] = new MotionSegment(m.From, m.To, Fix16.FromDecimal(m.ForwardPerFrame));
        }

        return motion;
    }

    private static (Fix16 ThrowRange, ThrowSide ThrowSide, Vec3Fix ThrowEndOffset) MapThrowFields(MoveDataDto dto, bool hasThrowHit)
    {
        if (dto.Kind != MoveKind.Throw)
        {
            return (default, ThrowSide.Front, default);
        }

        if (dto.ThrowRange is null || dto.ThrowRange <= 0)
        {
            throw new GameDataFormatException($"投げ技'{dto.Name}'のthrowRangeは0より大きい値が必要です。");
        }

        if (hasThrowHit)
        {
            throw new GameDataFormatException($"投げ技'{dto.Name}'は攻撃判定を持てません。");
        }

        var throwRange = Fix16.FromDecimal(dto.ThrowRange.Value);
        var throwSide = dto.ThrowSide ?? ThrowSide.Front;
        var throwEndOffset = dto.ThrowEndOffset is { } offset ? MapVec3(offset) : default;
        return (throwRange, throwSide, throwEndOffset);
    }

    private static StageData MapStage(StageDataDto dto)
    {
        if (dto.Size <= 0)
        {
            throw new GameDataFormatException("sizeは0より大きい値が必要です。");
        }

        var expectedEdges = dto.Shape switch
        {
            RingShape.Circle => 1,
            RingShape.Square => 4,
            RingShape.Octagon => 8,
            _ => throw new GameDataFormatException("shapeが不明です。"),
        };

        if (dto.Edges.Count != expectedEdges)
        {
            throw new GameDataFormatException($"{dto.Shape}のedgesは{expectedEdges}要素が必要です。");
        }

        if (dto.StartDistance / 2 >= dto.Size)
        {
            throw new GameDataFormatException("startDistanceの半分はsize未満が必要です。");
        }

        return new StageData
        {
            Name = dto.Name,
            Shape = dto.Shape,
            Size = Fix16.FromDecimal(dto.Size),
            Edges = [.. dto.Edges],
            StartDistance = Fix16.FromDecimal(dto.StartDistance),
        };
    }

    private static Rules MapRules(RulesDto dto)
    {
        if (dto.RoundTimeSeconds < 1 || dto.RoundTimeSeconds > 1092)
        {
            throw new GameDataFormatException("roundTimeSecondsは1以上1092以下が必要です。");
        }

        if (dto.RoundsToWin < 1)
        {
            throw new GameDataFormatException("roundsToWinは1以上が必要です。");
        }

        if (dto.MaxRounds < (dto.RoundsToWin * 2) - 1)
        {
            throw new GameDataFormatException("maxRoundsはroundsToWin*2-1以上が必要です。");
        }

        if (dto.InitialHealth < 1)
        {
            throw new GameDataFormatException("initialHealthは1以上が必要です。");
        }

        if (dto.DownMinFrames > dto.DownMaxFrames)
        {
            throw new GameDataFormatException("downMinFramesはdownMaxFrames以下が必要です。");
        }

        if (dto.Seed == 0)
        {
            throw new GameDataFormatException("seedは0以外が必要です。");
        }

        RequireAtLeastOne(dto.IntroFrames, nameof(dto.IntroFrames));
        RequireAtLeastOne(dto.RoundEndFrames, nameof(dto.RoundEndFrames));
        RequireAtLeastOne(dto.TechFrames, nameof(dto.TechFrames));
        RequireAtLeastOne(dto.TechRecoveryFrames, nameof(dto.TechRecoveryFrames));
        RequireAtLeastOne(dto.DownMinFrames, nameof(dto.DownMinFrames));
        RequireAtLeastOne(dto.RiseFrames, nameof(dto.RiseFrames));
        RequireAtLeastOne(dto.ThrowEscapeFrames, nameof(dto.ThrowEscapeFrames));
        RequireAtLeastOne(dto.ThrowEscapeRecoveryFrames, nameof(dto.ThrowEscapeRecoveryFrames));
        RequireAtLeastOne(dto.WallStunFrames, nameof(dto.WallStunFrames));

        return new Rules
        {
            RoundTimeSeconds = dto.RoundTimeSeconds,
            RoundsToWin = dto.RoundsToWin,
            MaxRounds = dto.MaxRounds,
            InitialHealth = dto.InitialHealth,
            IntroFrames = dto.IntroFrames,
            RoundEndFrames = dto.RoundEndFrames,
            TechFrames = dto.TechFrames,
            TechRecoveryFrames = dto.TechRecoveryFrames,
            DownMinFrames = dto.DownMinFrames,
            DownMaxFrames = dto.DownMaxFrames,
            RiseFrames = dto.RiseFrames,
            ThrowEscapeFrames = dto.ThrowEscapeFrames,
            ThrowEscapeRecoveryFrames = dto.ThrowEscapeRecoveryFrames,
            ThrowEscapeDistance = Fix16.FromDecimal(dto.ThrowEscapeDistance),
            WallStunFrames = dto.WallStunFrames,
            Seed = dto.Seed,
            Training = dto.Training,
        };
    }

    private static HitCapsule[] MapCapsules(IReadOnlyList<HitCapsuleDto> dtos)
    {
        var result = new HitCapsule[dtos.Count];
        for (var i = 0; i < dtos.Count; i++)
        {
            var d = dtos[i];
            var a = MapVec3(d.A);
            var b = d.B is { } bArray ? MapVec3(bArray) : a;
            result[i] = new HitCapsule(a, b, Fix16.FromDecimal(d.R));
        }

        return result;
    }

    private static Vec3Fix MapVec3(decimal[] xyz)
    {
        if (xyz.Length != 3)
        {
            throw new GameDataFormatException("座標は3要素の配列が必要です。");
        }

        return new Vec3Fix(Fix16.FromDecimal(xyz[0]), Fix16.FromDecimal(xyz[1]), Fix16.FromDecimal(xyz[2]));
    }

    private static void RequireAtLeastOne(ushort value, string name)
    {
        if (value < 1)
        {
            throw new GameDataFormatException($"{name}は1以上が必要です。");
        }
    }

    private static void RequireAtMost(int count, int limit, string name)
    {
        if (count > limit)
        {
            throw new GameDataFormatException($"{name}は{limit}個以下が必要です。");
        }
    }
}
