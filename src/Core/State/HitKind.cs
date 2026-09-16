namespace Godot3dFighter.Core.State;

public enum HitKind : byte
{
    None,
    Hit,
    CounterHit,
    WallHit,
    CounterWallHit,
    Guarded,
    Thrown,
    ThrowEscaped,
}
