using Godot;
using Godot3dFighter.Core.Data;
using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;
using Godot3dFighter.Game.View;

namespace Godot3dFighter.Game.Screens;

/// <summary>試合とトレーニングで共通する、1物理フレームごとのMatchSimulator.Step呼び出しと、図形キャラ、カメラ、HUDの更新（03-screens-and-e2e.md）。</summary>
public abstract partial class FightScreenBase : Node3D, IE2eMatchState
{
    private CharacterFigure? _p1Figure;
    private CharacterFigure? _p2Figure;
    private FightCamera? _camera;
    private FightHud? _hud;

    protected MatchContext Context { get; private set; } = null!;

    protected MatchState State { get; set; }

    public RoundPhase Phase => State.Phase;

    public RoundEndReason LastRoundReason => State.LastRoundReason;

    public byte LastRoundWinners => State.LastRoundWinners;

    public string HudText => _hud?.Text ?? "";

    public override void _Ready()
    {
        Context = BuildContext();
        State = MatchSimulator.CreateMatch(Context);

        _p1Figure = new CharacterFigure { BaseColor = new Color(0.2f, 0.4f, 0.9f) };
        _p2Figure = new CharacterFigure { BaseColor = new Color(0.9f, 0.3f, 0.2f) };
        AddChild(_p1Figure);
        AddChild(_p2Figure);

        _camera = new FightCamera();
        AddChild(_camera);

        var light = new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-45, -30, 0),
        };
        AddChild(light);
        AddChild(BuildFloor(Context.Stage));

        _hud = new FightHud();
        AddChild(_hud);

        UpdateView();
    }

    public override void _PhysicsProcess(double delta)
    {
        var p1Input = InputMapper.Read(1);
        var p2Input = ReadP2Input();

        var beforePhase = State.Phase;
        State = MatchSimulator.Step(State, p1Input, p2Input, Context);

        if (beforePhase != RoundPhase.RoundEnd && State.Phase == RoundPhase.RoundEnd)
        {
            OnRoundEnd();
        }

        UpdateView();
        OnAfterStep();
    }

    protected abstract InputFrame ReadP2Input();

    protected virtual void OnRoundEnd() => GetGameState().AddRoundResult(State.LastRoundReason, State.LastRoundWinners);

    protected virtual void OnAfterStep()
    {
    }

    protected GameState GetGameState() => GetNode<GameState>("/root/GameState");

    private MatchContext BuildContext() => GetGameState().LoadContext();

    private static MeshInstance3D BuildFloor(StageData stage)
    {
        var size = FixConvert.ToFloat(stage.Size);
        var floor = new MeshInstance3D
        {
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.3f, 0.3f, 0.35f) },
        };
        if (stage.Shape == RingShape.Square)
        {
            floor.Mesh = new PlaneMesh { Size = new Vector2(size * 2, size * 2) };
        }
        else
        {
            floor.Mesh = new CylinderMesh { TopRadius = size, BottomRadius = size, Height = 0.02f };
            floor.Position = new Vector3(0, -0.01f, 0);
        }

        return floor;
    }

    private void UpdateView()
    {
        if (_p1Figure is null || _p2Figure is null || _camera is null || _hud is null)
        {
            return;
        }

        Place(_p1Figure, State.GetPlayer(0));
        Place(_p2Figure, State.GetPlayer(1));
        _camera.Apply(State);
        _hud.Apply(State, Context);
    }

    private void Place(CharacterFigure figure, in PlayerState player)
    {
        figure.Position = FixConvert.ToVector3(player.Position);
        figure.Rotation = new Vector3(0, FixConvert.ToRadians(player.Facing), 0);
        figure.Apply(player, Context);
    }
}
