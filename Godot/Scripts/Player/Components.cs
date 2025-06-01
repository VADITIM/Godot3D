using Godot;
using System;

public partial class Components : Node
{
    public static Components Instance { get; private set; }

    public Walling WallManager;
    public Player Player;
    public Camera Camera;
    public Movement Movement;
    public StateMachine StateMachine;
    public GameUI GameUI;
    public UIAnimations UIAnimations;

    public override void _Ready()
    {
        Instance = this;

        Player = GetNode<Player>("/root/Main/Player");
        WallManager = GetNode<Walling>("/root/Main/Player/Components/WallManager");
        Camera = GetNode<Camera>("/root/Main/Player/Components/Camera");
        Movement = GetNode<Movement>("/root/Main/Player/Components/Movement");
        StateMachine = GetNode<StateMachine>("/root/Main/Player/Components/StateMachine");

        GameUI = GetNode<GameUI>("/root/Main/Game UI");
        UIAnimations = GetNode<UIAnimations>("/root/Main/Game UI/UIAnimations");
    }
}
