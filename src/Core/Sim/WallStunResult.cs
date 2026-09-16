using Godot3dFighter.Core.Math;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Core.Sim;

public readonly record struct WallStunResult(bool BecameWallStun, PlayerFlags NewFlags, Vec3Fix FinalPosition);
