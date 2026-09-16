using System.Collections.Generic;

namespace Godot3dFighter.Core.Data;

internal sealed class HitCapsuleDto
{
    public required decimal[] A { get; init; }

    public decimal[]? B { get; init; }

    public required decimal R { get; init; }
}

internal sealed class MotionSegmentDto
{
    public required ushort From { get; init; }

    public required ushort To { get; init; }

    public required decimal ForwardPerFrame { get; init; }
}

internal sealed class HitWindowDto
{
    public required ushort From { get; init; }

    public required ushort To { get; init; }

    public required IReadOnlyList<HitCapsuleDto> Hit { get; init; }

    public required IReadOnlyList<HitCapsuleDto> Hurt { get; init; }
}

internal sealed class MoveDataDto
{
    public required string Name { get; init; }

    public required string Command { get; init; }

    public required MoveKind Kind { get; init; }

    public required Posture Posture { get; init; }

    public required ushort Startup { get; init; }

    public required ushort Active { get; init; }

    public required ushort Recovery { get; init; }

    public required ushort Tracking { get; init; }

    public required HitHeight Height { get; init; }

    public required int Damage { get; init; }

    public required int CounterDamage { get; init; }

    public required ushort Hitstun { get; init; }

    public required ushort CounterHitstun { get; init; }

    public required ushort Blockstun { get; init; }

    public required byte Hitstop { get; init; }

    public required bool Knockdown { get; init; }

    public required bool CounterKnockdown { get; init; }

    public required bool HitsDown { get; init; }

    public required decimal Pushback { get; init; }

    public required IReadOnlyList<MotionSegmentDto> Motion { get; init; }

    public required IReadOnlyList<HitWindowDto> Windows { get; init; }

    public decimal? ThrowRange { get; init; }

    public ThrowSide? ThrowSide { get; init; }

    public decimal[]? ThrowEndOffset { get; init; }
}

internal sealed class CharacterDataDto
{
    public required string Name { get; init; }

    public required decimal WalkSpeed { get; init; }

    public required decimal BackWalkSpeed { get; init; }

    public required decimal DashSpeed { get; init; }

    public required ushort DashFrames { get; init; }

    public required decimal BackdashSpeed { get; init; }

    public required ushort BackdashFrames { get; init; }

    public required ushort DashCancelFrames { get; init; }

    public required decimal SidestepSpeed { get; init; }

    public required ushort SidestepFrames { get; init; }

    public required ushort SidestepCancelFrame { get; init; }

    public required decimal RollSpeed { get; init; }

    public required ushort RollFrames { get; init; }

    public required decimal BodyRadius { get; init; }

    public required IReadOnlyList<HitCapsuleDto> StandHurtCapsules { get; init; }

    public required IReadOnlyList<HitCapsuleDto> CrouchHurtCapsules { get; init; }

    public required IReadOnlyList<HitCapsuleDto> DownHurtCapsules { get; init; }

    public required IReadOnlyList<MoveDataDto> Moves { get; init; }
}

internal sealed class StageDataDto
{
    public required string Name { get; init; }

    public required RingShape Shape { get; init; }

    public required decimal Size { get; init; }

    public required IReadOnlyList<EdgeKind> Edges { get; init; }

    public required decimal StartDistance { get; init; }
}

internal sealed class RulesDto
{
    public required ushort RoundTimeSeconds { get; init; }

    public required byte RoundsToWin { get; init; }

    public required byte MaxRounds { get; init; }

    public required int InitialHealth { get; init; }

    public required ushort IntroFrames { get; init; }

    public required ushort RoundEndFrames { get; init; }

    public required ushort TechFrames { get; init; }

    public required ushort TechRecoveryFrames { get; init; }

    public required ushort DownMinFrames { get; init; }

    public required ushort DownMaxFrames { get; init; }

    public required ushort RiseFrames { get; init; }

    public required ushort ThrowEscapeFrames { get; init; }

    public required ushort ThrowEscapeRecoveryFrames { get; init; }

    public required decimal ThrowEscapeDistance { get; init; }

    public required ushort WallStunFrames { get; init; }

    public required uint Seed { get; init; }

    public required bool Training { get; init; }
}
