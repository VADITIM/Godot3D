using Godot;
using System;

public partial class PlayerComponents : Node
{
    public WallManager WallManager;
    public Player Player;
    public Camera Camera;
    
    public static PlayerComponents Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;

        Player = GetNode<Player>("/root/Main/Player");
        WallManager = GetNode<WallManager>("/root/Main/Player/PlayerComponents/WallManager");
        Camera = GetNode<Camera>("/root/Main/Player/PlayerComponents/Camera");
    }
}
