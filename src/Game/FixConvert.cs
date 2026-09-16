using Godot;
using Godot3dFighter.Core.Math;

namespace Godot3dFighter.Game;

/// <summary>CoreのFix16とAngle16をGodotのfloatとVector3に変換する（03-screens-and-e2e.md）。</summary>
public static class FixConvert
{
    public static float ToFloat(Fix16 value) => (float)value.Raw / (1 << Fix16.Shift);

    public static Vector3 ToVector3(Vec3Fix value) => new(ToFloat(value.X), ToFloat(value.Y), ToFloat(value.Z));

    public static float ToRadians(Angle16 value) => value.Value / 65536f * Mathf.Tau;
}
