using System;

namespace Godot3dFighter.Core.State;

[Flags]
public enum PlayerFlags : byte
{
    None = 0,
    HasHitThisMove = 1,
    DownHitTaken = 2,
    ThrowEscapeTried = 4,
    WallHitTaken = 8,
    TechQueued = 16,
}
