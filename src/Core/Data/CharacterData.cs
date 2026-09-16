using System.Collections.Generic;
using Godot3dFighter.Core.Math;

namespace Godot3dFighter.Core.Data;

/// <summary>1キャラ分のデータ（ADR-0004）。</summary>
public sealed class CharacterData
{
    public required string Name { get; init; }

    public required Fix16 WalkSpeed { get; init; }

    public required Fix16 BackWalkSpeed { get; init; }

    public required Fix16 DashSpeed { get; init; }

    public required ushort DashFrames { get; init; }

    public required Fix16 BackdashSpeed { get; init; }

    public required ushort BackdashFrames { get; init; }

    public required ushort DashCancelFrames { get; init; }

    public required Fix16 SidestepSpeed { get; init; }

    public required ushort SidestepFrames { get; init; }

    public required ushort SidestepCancelFrame { get; init; }

    public required Fix16 RollSpeed { get; init; }

    public required ushort RollFrames { get; init; }

    public required Fix16 BodyRadius { get; init; }

    public required IReadOnlyList<HitCapsule> StandHurtCapsules { get; init; }

    public required IReadOnlyList<HitCapsule> CrouchHurtCapsules { get; init; }

    public required IReadOnlyList<HitCapsule> DownHurtCapsules { get; init; }

    public required IReadOnlyList<MoveData> Moves { get; init; }
}
