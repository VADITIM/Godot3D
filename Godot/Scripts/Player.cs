using Godot;

public partial class Player : CharacterBody3D
{
    [Export] public CharacterBody3D player;

    [Export] private float speed = 5.0f;
    [Export] private float sprintMultiplier = 3.5f;
    [Export] public float jumpForce = 8.5f;
    public float gravity = 9.8f;
    public Vector3 velocity = Vector3.Zero;

    private bool isJumping = false;
    private bool isSprinting = false;
    private bool isGrounded = true;

    public bool IsJumping { get => isJumping; set => isJumping = value; }

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleMovement((float)delta);
        PlayerComponents.Instance.WallManager.HandleWallJump();
    }

    public override void _Process(double delta)
    {
        HandleGravity((float)delta);
        PlayerComponents.Instance.WallManager.CheckWall();
        PlayerComponents.Instance.WallManager.HandleWalling();
    }

    private void HandleGravity(float delta)
    {
        if (!isGrounded)
            velocity.Y -= gravity * delta;
        else
            velocity.Y = 0;
    }

    private Vector3 GetDirection()
    {
        Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
        Vector3 direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();

        direction = player.Transform.Basis * direction;

        return direction;
    }
    
    private void HandleMovement(float delta)
    {
        GetDirection();
        HandleGravity(delta);
        Vector3 direction = GetDirection();

        if (Input.IsActionJustPressed("jump") && isGrounded)
        {
            velocity.Y = jumpForce;
            isGrounded = false;
        }

        Vector3 horizontalVelocity = new Vector3(direction.X, 0, direction.Z);

        float currentSpeed = speed;

        if (Input.IsActionPressed("sprint"))
        {
            currentSpeed *= sprintMultiplier;
            isSprinting = true;
        }

        horizontalVelocity *= currentSpeed;

        velocity.X = horizontalVelocity.X;
        velocity.Z = horizontalVelocity.Z;

        Velocity = velocity;
        MoveAndSlide();

        isGrounded = IsOnFloor();

        velocity = Velocity;
    }
}
