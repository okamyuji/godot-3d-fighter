using System.Collections.Generic;

namespace Godot3dFighter.Core.Data;

/// <summary>技の判定区間。FromとToは経過フレームで両端を含む。</summary>
public sealed class HitWindow
{
    public required ushort From { get; init; }

    public required ushort To { get; init; }

    public required IReadOnlyList<HitCapsule> Hit { get; init; }

    public required IReadOnlyList<HitCapsule> Hurt { get; init; }
}
