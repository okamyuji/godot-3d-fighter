using Godot;
using Godot3dFighter.Core;
using Godot3dFighter.Core.Input;
using Godot3dFighter.Core.Sim;
using Godot3dFighter.Core.State;

namespace Godot3dFighter.Game.Screens;

/// <summary>試合とトレーニングで共通する、1物理フレームごとのMatchSimulator.Step呼び出しと箱の表示（03-screens-and-e2e.md）。</summary>
public abstract partial class FightScreenBase : Node3D, IE2eMatchState
{
    private MeshInstance3D? _p1Box;
    private MeshInstance3D? _p2Box;
    private Label? _infoLabel;

    protected MatchContext Context { get; private set; } = null!;

    protected MatchState State { get; set; }

    public RoundPhase Phase => State.Phase;

    public RoundEndReason LastRoundReason => State.LastRoundReason;

    public byte LastRoundWinners => State.LastRoundWinners;

    public override void _Ready()
    {
        Context = BuildContext();
        State = MatchSimulator.CreateMatch(Context);

        _p1Box = BuildBox(new Color(0.2f, 0.4f, 0.9f));
        _p2Box = BuildBox(new Color(0.9f, 0.3f, 0.2f));

        var camera = new Camera3D
        {
            Position = new Vector3(0, 1.6f, 4.5f),
        };
        AddChild(camera);
        camera.LookAt(new Vector3(0, 1, 0), Vector3.Up);

        var light = new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-45, -30, 0),
        };
        AddChild(light);

        var floor = new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(8, 8) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.3f, 0.3f, 0.35f) },
        };
        AddChild(floor);

        var canvas = new CanvasLayer();
        _infoLabel = new Label
        {
            Position = new Vector2(16, 16),
        };
        canvas.AddChild(_infoLabel);
        AddChild(canvas);

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

    private static MeshInstance3D BuildBox(Color color)
    {
        var mesh = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.5f, 1.7f, 0.4f) },
        };
        var material = new StandardMaterial3D { AlbedoColor = color };
        mesh.MaterialOverride = material;
        return mesh;
    }

    private void UpdateView()
    {
        var p1 = State.GetPlayer(0);
        var p2 = State.GetPlayer(1);

        if (_p1Box is not null)
        {
            _p1Box.Position = FixConvert.ToVector3(p1.Position) + new Vector3(0, 0.85f, 0);
            _p1Box.Rotation = new Vector3(0, FixConvert.ToRadians(p1.Facing), 0);
            if (_p1Box.GetParent() is null)
            {
                AddChild(_p1Box);
            }
        }

        if (_p2Box is not null)
        {
            _p2Box.Position = FixConvert.ToVector3(p2.Position) + new Vector3(0, 0.85f, 0);
            _p2Box.Rotation = new Vector3(0, FixConvert.ToRadians(p2.Facing), 0);
            if (_p2Box.GetParent() is null)
            {
                AddChild(_p2Box);
            }
        }

        if (_infoLabel is not null)
        {
            var secondsLeft = (State.RoundTimerFrames + Limits.FramesPerSecond - 1) / Limits.FramesPerSecond;
            _infoLabel.Text = $"P1 HP:{p1.Health}  P2 HP:{p2.Health}  time:{secondsLeft}  round:{State.RoundNumber}  wins:{State.GetRoundWins(0)}-{State.GetRoundWins(1)}  phase:{State.Phase}";
        }
    }
}
