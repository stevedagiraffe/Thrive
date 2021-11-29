using Godot;

/// <summary>
///   Handles the microbe cheat menu
/// </summary>
public class MicrobeCheatMenu : CheatMenu
{
    [Export]
    public NodePath InfiniteCompoundsPath;

    [Export]
    public NodePath GodModePath;

    [Export]
    public NodePath DisableAIPath;

    [Export]
    public NodePath SpeedSliderPath;

    [Export]
    public NodePath PlayerDividePath;

    [Export]
    public NodePath GenerateSpawnMapPath;

    [Export]
    public NodePath CurrentSectorPath;

    [Export]
    public NodePath MicrobeStagePath;

    private CheckBox infiniteCompounds;
    private CheckBox godMode;
    private CheckBox disableAI;
    private Slider speed;
    private Button playerDivide;
    private Button generateSpawnMap;
    private Label currentSector;
    private MicrobeStage microbeStage;

    public override void _Ready()
    {
        infiniteCompounds = GetNode<CheckBox>(InfiniteCompoundsPath);
        godMode = GetNode<CheckBox>(GodModePath);
        disableAI = GetNode<CheckBox>(DisableAIPath);
        speed = GetNode<Slider>(SpeedSliderPath);
        playerDivide = GetNode<Button>(PlayerDividePath);
        generateSpawnMap = GetNode<Button>(GenerateSpawnMapPath);
        currentSector = GetNode<Label>(CurrentSectorPath);
        microbeStage = GetNode<MicrobeStage>(MicrobeStagePath);

        playerDivide.Connect("pressed", this, nameof(OnPlayerDivideClicked));
        generateSpawnMap.Connect("pressed", this, nameof(OnGenerateMapClicked));
        base._Ready();
    }

    public override void _Process(float delta)
    {
        currentSector.Text = microbeStage.Spawner.CurrentSector.ToString();
        base._Process(delta);
    }

    public override void ReloadGUI()
    {
        infiniteCompounds.Pressed = CheatManager.InfiniteCompounds;
        godMode.Pressed = CheatManager.GodMode;
        disableAI.Pressed = CheatManager.NoAI;
        speed.Value = CheatManager.Speed;
    }

    private void OnPlayerDivideClicked()
    {
        CheatManager.PlayerDuplication();
    }

    private void OnGenerateMapClicked()
    {
        microbeStage.Spawner.GenerateNoiseImage();
    }
}
