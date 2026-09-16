namespace Godot3dFighter.Core.Data;

using System.Collections.Generic;
using Godot3dFighter.Core.Math;

/// <summary>ステージ1つ分のデータ（ADR-0002）。</summary>
public sealed class StageData
{
    public required string Name { get; init; }

    public required RingShape Shape { get; init; }

    /// <summary>円の半径、または中心から辺までの距離（m）。</summary>
    public required Fix16 Size { get; init; }

    /// <summary>各辺の縁の種類。円は1、正方形は4、正八角形は8要素。</summary>
    public required IReadOnlyList<EdgeKind> Edges { get; init; }

    /// <summary>開始時の2人の距離（m）。</summary>
    public required Fix16 StartDistance { get; init; }
}
