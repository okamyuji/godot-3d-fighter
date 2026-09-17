namespace Godot3dFighter.Core.Sim;

/// <summary>CPUの手強さ（ADR-0026）。CpuPolicyが技を選ぶ周期、ガード、ガード崩し、投げ抜けの有無を決める。</summary>
public enum CpuLevel : byte
{
    Easy,
    Normal,
    Hard,
}
