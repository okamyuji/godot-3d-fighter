namespace Godot3dFighter.Core.State;

public enum StateKind : byte
{
    Idle,
    Walk,
    BackWalk,
    Crouch,
    Guard,
    CrouchGuard,
    SidestepIn,
    SidestepOut,
    Dash,
    Backdash,
    Attack,
    Throwing,
    Thrown,
    ThrowEscape,
    Blockstun,
    CrouchBlockstun,
    Hitstun,
    WallStun,
    Down,
    Tech,
    RollIn,
    RollOut,
    Rise,
    Dead,
}
