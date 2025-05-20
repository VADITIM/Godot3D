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
        PlayerComponents.Instance.Movement.HandleMovement((float)delta);
        PlayerComponents.Instance.WallManager.HandleWallJump();
    }

    public override void _Process(double delta)
    {
        PlayerComponents.Instance.Movement.HandleGravity((float)delta);
        PlayerComponents.Instance.WallManager.CheckWall();
        PlayerComponents.Instance.WallManager.HandleWalling(currentSpeed: PlayerComponents.Instance.Movement.speed);
    }
}
