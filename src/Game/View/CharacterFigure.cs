using System;
using Godot;
using Godot3dFighter.Core;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Game.View;

/// <summary>頭、胴、両腕、両脚の図形でキャラを描く（ADR-0025）。姿勢はPlayerStateと技データから毎フレーム決める。</summary>
public sealed partial class CharacterFigure : Node3D
{
    private const float HeadY = 1.6f;
    private const float HeadRadius = 0.12f;
    private const float ShoulderY = 1.4f;
    private const float ShoulderHalf = 0.22f;
    private const float StandHipY = 0.85f;
    private const float CrouchHipY = 0.55f;
    private const float HipHalf = 0.12f;
    private const float UpperArm = 0.3f;
    private const float Forearm = 0.3f;
    private const float Thigh = 0.45f;
    private const float Shin = 0.4f;
    private const float LimbRadius = 0.06f;
    private const float TorsoRadius = 0.18f;
    private const float HitstunTilt = Mathf.Pi / 9;
    private const float CrouchTilt = -0.5f;
    private const float WalkSwing = 0.15f;
    private const int HitFlashFrames = 6;

    private static readonly Vector3 Forward = new(1, 0, 0);
    private static readonly Vector3 HangHandL = new(0.05f, 0.8f, -0.28f);
    private static readonly Vector3 HangHandR = new(0.05f, 0.8f, 0.28f);
    private static readonly Vector3 GuardHandL = new(0.35f, 1.35f, -0.1f);
    private static readonly Vector3 GuardHandR = new(0.35f, 1.35f, 0.1f);
    private static readonly Vector3 RaisedHandL = new(-0.1f, 1.6f, -0.3f);
    private static readonly Vector3 RaisedHandR = new(-0.1f, 1.6f, 0.3f);
    private static readonly Vector3 ReachHandL = new(0.6f, 1.2f, -0.15f);
    private static readonly Vector3 ReachHandR = new(0.6f, 1.2f, 0.15f);
    private static readonly Vector3 CrouchHandL = new(0.25f, 0.9f, -0.15f);
    private static readonly Vector3 CrouchHandR = new(0.25f, 0.9f, 0.15f);
    private static readonly Vector3 StandFootL = new(0f, 0f, -HipHalf);
    private static readonly Vector3 StandFootR = new(0f, 0f, HipHalf);
    private static readonly Vector3 CrouchFootL = new(0.15f, 0f, -0.15f);
    private static readonly Vector3 CrouchFootR = new(0.15f, 0f, 0.15f);
    private static readonly Vector3 ArmBendL = new(-0.3f, -0.5f, -1f);
    private static readonly Vector3 ArmBendR = new(-0.3f, -0.5f, 1f);
    private static readonly Transform3D LyingTransform = new(Basis.FromEuler(new Vector3(0, 0, Mathf.Pi / 2)), new Vector3(0.85f, 0.15f, 0));

    private readonly Node3D _body = new();
    private readonly MeshInstance3D _head;
    private readonly MeshInstance3D _torso;
    private readonly MeshInstance3D _upperArmL;
    private readonly MeshInstance3D _upperArmR;
    private readonly MeshInstance3D _forearmL;
    private readonly MeshInstance3D _forearmR;
    private readonly MeshInstance3D _thighL;
    private readonly MeshInstance3D _thighR;
    private readonly MeshInstance3D _shinL;
    private readonly MeshInstance3D _shinR;
    private int _flashFrames;

    private enum Limb : byte
    {
        None,
        RightArm,
        RightLeg,
        BothArms,
    }

    public CharacterFigure()
    {
        AddChild(_body);
        _head = AddPart(new SphereMesh { Radius = HeadRadius, Height = HeadRadius * 2 });
        _torso = AddPart(new CapsuleMesh { Radius = TorsoRadius, Height = 1f });
        _upperArmL = AddLimb();
        _upperArmR = AddLimb();
        _forearmL = AddLimb();
        _forearmR = AddLimb();
        _thighL = AddLimb();
        _thighR = AddLimb();
        _shinL = AddLimb();
        _shinR = AddLimb();
    }

    public Color BaseColor { get; set; } = Colors.White;

    /// <summary>状態から姿勢と色を決めて部品を置く。キャラ座標（前が+X、右が+Z、上が+Y）で計算し、向きは親が回す。</summary>
    public void Apply(in PlayerState player, MatchContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var pose = ComputePose(player, context);
        _body.Transform = IsLying(player.State) ? LyingTransform : Transform3D.Identity;

        var tiltedUp = new Vector3(-Mathf.Sin(pose.TorsoTilt), Mathf.Cos(pose.TorsoTilt), 0);
        var hip = new Vector3(0, pose.HipY, 0);
        var shoulderCenter = hip + (tiltedUp * (ShoulderY - StandHipY));
        _head.Position = hip + (tiltedUp * (HeadY - StandHipY));
        SetSegment(_torso, hip, shoulderCenter);

        PlaceLimb(_upperArmL, _forearmL, shoulderCenter + new Vector3(0, 0, -ShoulderHalf), pose.HandL, UpperArm, Forearm, ArmBendL);
        PlaceLimb(_upperArmR, _forearmR, shoulderCenter + new Vector3(0, 0, ShoulderHalf), pose.HandR, UpperArm, Forearm, ArmBendR);
        PlaceLimb(_thighL, _shinL, hip + new Vector3(0, 0, -HipHalf), pose.FootL, Thigh, Shin, Forward);
        PlaceLimb(_thighR, _shinR, hip + new Vector3(0, 0, HipHalf), pose.FootR, Thigh, Shin, Forward);

        if (player.LastHitKind is HitKind.Hit or HitKind.CounterHit or HitKind.WallHit or HitKind.CounterWallHit or HitKind.Thrown)
        {
            _flashFrames = HitFlashFrames;
        }
        else if (_flashFrames > 0)
        {
            _flashFrames--;
        }

        Paint(pose.StrikingLimb);
    }

    private readonly record struct Pose(float HipY, float TorsoTilt, Vector3 HandL, Vector3 HandR, Vector3 FootL, Vector3 FootR, Limb StrikingLimb);

    private static Pose Standing() => new(StandHipY, 0f, HangHandL, HangHandR, StandFootL, StandFootR, Limb.None);

    private static Pose ComputePose(in PlayerState player, MatchContext context)
    {
        var standing = Standing();
        switch (player.State)
        {
            case StateKind.Walk or StateKind.BackWalk:
                return WithWalk(standing, player.StateFrame);
            case StateKind.Crouch or StateKind.CrouchGuard or StateKind.CrouchBlockstun:
                return new Pose(CrouchHipY, CrouchTilt, CrouchHandL, CrouchHandR, CrouchFootL, CrouchFootR, Limb.None);
            case StateKind.Guard or StateKind.Blockstun:
                return standing with { HandL = GuardHandL, HandR = GuardHandR };
            case StateKind.Hitstun or StateKind.WallStun:
                return standing with { TorsoTilt = HitstunTilt, HandL = RaisedHandL, HandR = RaisedHandR };
            case StateKind.Rise:
                return standing with { HipY = Mathf.Lerp(CrouchHipY, StandHipY, Mathf.Min(1f, (float)player.StateFrame / context.Rules.RiseFrames)) };
            case StateKind.Throwing or StateKind.Thrown:
                return standing with { HandL = ReachHandL, HandR = ReachHandR };
            case StateKind.Attack:
                return Attacking(standing, player, context);
            default:
                return standing;
        }
    }

    private static Pose WithWalk(Pose standing, ushort frame)
    {
        var phase = Mathf.Sin(frame * 0.3f);
        return standing with
        {
            FootL = StandFootL + new Vector3(-WalkSwing * phase, 0.05f * Mathf.Max(0f, -phase), 0),
            FootR = StandFootR + new Vector3(WalkSwing * phase, 0.05f * Mathf.Max(0f, phase), 0),
        };
    }

    private static Pose Attacking(Pose standing, in PlayerState player, MatchContext context)
    {
        if (player.CurrentMove == Limits.NoMove)
        {
            return standing;
        }

        var move = context.Characters[player.Slot].Moves[player.CurrentMove];
        var limb = LimbOf(move);
        if (limb == Limb.BothArms)
        {
            return standing with { HandL = ReachHandL, HandR = ReachHandR, StrikingLimb = Limb.BothArms };
        }

        if (move.Windows.Count == 0)
        {
            return standing;
        }

        var rest = limb == Limb.RightLeg ? StandFootR : HangHandR;
        var (target, active) = StrikeTarget(move, player.StateFrame, rest);
        var striking = active ? limb : Limb.None;
        return limb == Limb.RightLeg
            ? standing with { FootR = target, StrikingLimb = striking }
            : standing with { HandR = target, HandL = GuardHandL, StrikingLimb = striking };
    }

    private static Limb LimbOf(MoveData move)
    {
        if (move.Kind == MoveKind.Throw)
        {
            return Limb.BothArms;
        }

        return move.Command.Contains('K', StringComparison.Ordinal) ? Limb.RightLeg : Limb.RightArm;
    }

    /// <summary>発生中は構えから最初の区間のBへ、区間中はそのB、硬直中は最後の区間のBから構えへ戻す。</summary>
    private static (Vector3 Target, bool Active) StrikeTarget(MoveData move, ushort frame, Vector3 rest)
    {
        HitWindow? current = null;
        var first = move.Windows[0];
        var last = move.Windows[0];
        foreach (var window in move.Windows)
        {
            if (window.From < first.From)
            {
                first = window;
            }

            if (window.To > last.To)
            {
                last = window;
            }

            if (frame >= window.From && frame <= window.To)
            {
                current = window;
            }
        }

        if (current is not null)
        {
            return (TipOf(current, rest), current.Hit.Count > 0);
        }

        if (frame < first.From)
        {
            return (rest.Lerp(TipOf(first, rest), (float)frame / first.From), false);
        }

        var t = move.Recovery == 0 ? 1f : Mathf.Min(1f, (float)(frame - last.To) / move.Recovery);
        return (TipOf(last, rest).Lerp(rest, t), false);
    }

    private static Vector3 TipOf(HitWindow window, Vector3 rest) =>
        window.Hit.Count > 0 ? FixConvert.ToVector3(window.Hit[0].B) : rest;

    private static bool IsLying(StateKind state) =>
        state is StateKind.Down or StateKind.RollIn or StateKind.RollOut or StateKind.Dead;

    /// <summary>2関節の解析的IK。根元から目標までを2本の長さの和で抑え、余弦定理で関節を置く。</summary>
    private static void PlaceLimb(MeshInstance3D upper, MeshInstance3D lower, Vector3 root, Vector3 target, float upperLength, float lowerLength, Vector3 bendDirection)
    {
        var toTarget = target - root;
        var reach = upperLength + lowerLength - 0.001f;
        var distance = toTarget.Length();
        if (distance > reach)
        {
            toTarget *= reach / distance;
            distance = reach;
        }

        Vector3 joint;
        if (distance < 0.001f)
        {
            joint = root + (bendDirection.Normalized() * upperLength);
        }
        else
        {
            var direction = toTarget / distance;
            var cosRoot = Mathf.Clamp(((upperLength * upperLength) + (distance * distance) - (lowerLength * lowerLength)) / (2f * upperLength * distance), -1f, 1f);
            var sinRoot = Mathf.Sqrt(1f - (cosRoot * cosRoot));
            var side = bendDirection - (direction * bendDirection.Dot(direction));
            if (side.LengthSquared() < 1e-6f)
            {
                side = new Vector3(0, 0, 1);
            }

            joint = root + (direction * (upperLength * cosRoot)) + (side.Normalized() * (upperLength * sinRoot));
        }

        SetSegment(upper, root, joint);
        SetSegment(lower, joint, root + toTarget);
    }

    /// <summary>カプセルのY軸を線分に合わせ、長さを線分の長さ+直径にする。</summary>
    private static void SetSegment(MeshInstance3D part, Vector3 a, Vector3 b)
    {
        var d = b - a;
        var length = d.Length();
        if (part.Mesh is CapsuleMesh capsule)
        {
            var height = length + (capsule.Radius * 2);
            if (Mathf.Abs(capsule.Height - height) > 0.001f)
            {
                capsule.Height = height;
            }
        }

        var y = length > 0.0001f ? d / length : Vector3.Up;
        var helper = Mathf.Abs(y.Dot(Vector3.Up)) > 0.99f ? new Vector3(1, 0, 0) : Vector3.Up;
        var x = helper.Cross(y).Normalized();
        var z = x.Cross(y);
        part.Transform = new Transform3D(new Basis(x, y, z), (a + b) * 0.5f);
    }

    private void Paint(Limb striking)
    {
        var body = _flashFrames > 0 ? Colors.White : BaseColor;
        var strike = _flashFrames > 0 ? Colors.White : BaseColor.Lightened(0.5f);
        var leftArm = striking == Limb.BothArms ? strike : body;
        var rightArm = striking is Limb.RightArm or Limb.BothArms ? strike : body;
        var rightLeg = striking == Limb.RightLeg ? strike : body;
        SetColor(_head, body);
        SetColor(_torso, body);
        SetColor(_upperArmL, leftArm);
        SetColor(_forearmL, leftArm);
        SetColor(_upperArmR, rightArm);
        SetColor(_forearmR, rightArm);
        SetColor(_thighL, body);
        SetColor(_shinL, body);
        SetColor(_thighR, rightLeg);
        SetColor(_shinR, rightLeg);
    }

    private static void SetColor(MeshInstance3D part, Color color)
    {
        if (part.MaterialOverride is StandardMaterial3D material && material.AlbedoColor != color)
        {
            material.AlbedoColor = color;
        }
    }

    private MeshInstance3D AddLimb() => AddPart(new CapsuleMesh { Radius = LimbRadius, Height = 0.5f });

    private MeshInstance3D AddPart(Mesh mesh)
    {
        var part = new MeshInstance3D { Mesh = mesh, MaterialOverride = new StandardMaterial3D() };
        _body.AddChild(part);
        return part;
    }
}
