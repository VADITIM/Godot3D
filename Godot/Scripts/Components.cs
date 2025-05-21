using Godot;
using System;

public partial class Components : Node
{
    public Walling WallManager;
    public Player Player;
    public Camera Camera;
    public Movement Movement;
    
    public static Components Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;

        Player = GetNode<Player>("/root/Main/Player");
        WallManager = GetNode<Walling>("/root/Main/Player/Components/WallManager");
        Camera = GetNode<Camera>("/root/Main/Player/Components/Camera");
        Movement = GetNode<Movement>("/root/Main/Player/Components/Movement");
    }
}
