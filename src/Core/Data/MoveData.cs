using System.Collections.Generic;
using Godot3dFighter.Core.Math;

namespace Godot3dFighter.Core.Data;

/// <summary>1つの技のデータ（ADR-0004、ADR-0006）。</summary>
public sealed class MoveData
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

    public required Fix16 Pushback { get; init; }

    public required IReadOnlyList<MotionSegment> Motion { get; init; }

    public required IReadOnlyList<HitWindow> Windows { get; init; }

    public Fix16 ThrowRange { get; init; }

    public ThrowSide ThrowSide { get; init; }

    public Vec3Fix ThrowEndOffset { get; init; }
}
