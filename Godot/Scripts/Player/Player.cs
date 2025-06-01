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
        Components.Instance.WallManager.CheckWall(); // Check wall state first
        Components.Instance.WallManager.HandleWallJump(); // Then handle wall jump (which can modify wall state)
        Components.Instance.WallManager.HandleWalling(); // Finally apply walling effects
    }
    
    public override void _Process(double delta)
    {
        Components.Instance.Movement.HandleGravity((float)delta);
    }
}
