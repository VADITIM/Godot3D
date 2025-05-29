using Godot;

public partial class Player : CharacterBody3D
{
    [Export] public CharacterBody3D rb;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _PhysicsProcess(double delta)
    {
        Components.Instance.Movement.HandleMovement((float)delta);
        Components.Instance.WallManager.HandleWallJump();
        Components.Instance.WallManager.CheckWall();
        Components.Instance.WallManager.HandleWalling();
    }
    
    public override void _Process(double delta)
    {
        Components.Instance.Movement.HandleGravity((float)delta);
    }
}
