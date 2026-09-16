using Godot3dFighter.Core.Math;

namespace Godot3dFighter.Core.Data;

/// <summary>技の間の前後移動の1区間。FromとToは経過フレームで両端を含む。</summary>
public readonly record struct MotionSegment(ushort From, ushort To, Fix16 ForwardPerFrame);
