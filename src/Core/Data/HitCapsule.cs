using Godot3dFighter.Core.Math;

namespace Godot3dFighter.Core.Data;

/// <summary>攻撃判定とやられ判定の形。両端が同じなら球（ADR-0006）。座標はキャラ座標。</summary>
public readonly record struct HitCapsule(Vec3Fix A, Vec3Fix B, Fix16 Radius);
