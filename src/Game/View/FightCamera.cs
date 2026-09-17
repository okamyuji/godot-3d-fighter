using Godot;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Game.View;

/// <summary>ADR-0016のカメラ。2人の足元の中点からCameraYaw - 16384の方向へ離れ、中点の1m上を見る。</summary>
public sealed partial class FightCamera : Camera3D
{
    private const float Distance = 4.5f;
    private const float Height = 1.6f;
    private const float LookHeight = 1.0f;

    public void Apply(in MatchState state)
    {
        var p1 = FixConvert.ToVector3(state.GetPlayer(0).Position);
        var p2 = FixConvert.ToVector3(state.GetPlayer(1).Position);
        var midpoint = (p1 + p2) * 0.5f;
        var yaw = FixConvert.ToRadians(state.CameraYaw) - (Mathf.Pi / 2);
        var back = new Vector3(Mathf.Cos(yaw), 0, -Mathf.Sin(yaw));
        Position = midpoint + (back * Distance) + (Vector3.Up * Height);
        LookAt(midpoint + (Vector3.Up * LookHeight), Vector3.Up);
    }
}
