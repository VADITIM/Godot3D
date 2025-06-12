using Godot;

public partial class Components : Node
{
    public static Components Instance { get; private set; }

    public Camera Camera => Player.GetNode<Camera>("/root/Main/Player/Components/Camera");

    public Player Player => GetNode<Player>("/root/Main/Player");
    public Movement Movement => Player.GetNode<Movement>("Components/Movement");
    public WallManager WallManager => Player.GetNode<WallManager>("Components/WallManager");
    public StateMachine StateMachine => Player.GetNode<StateMachine>("Components/StateMachine");
    public Health Health => Player.GetNode<Health>("Components/Health");

    public GameUI GameUI => GetNode<GameUI>("/root/Main/Game UI");
    public UIAnimations UIAnimations => GameUI.GetNode<UIAnimations>("UIAnimations");

    public override void _Ready()
    {
        Instance = this;
    }
}
