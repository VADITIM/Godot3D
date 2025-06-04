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
        Components.Instance.WallManager.CheckWall();

        if (Input.IsActionPressed("jump"))
        {
            Components.Instance.Movement.isHoldingJump = true;
        }
        else if (Input.IsActionJustReleased("jump"))
        {
            Components.Instance.WallManager.HandleWallJump();
            Components.Instance.Movement.isHoldingJump = false;
        }

        Components.Instance.WallManager.HandleWalling();
    }

    public override void _Process(double delta)
    {
        Components.Instance.Movement.HandleGravity((float)delta);
    }
}
