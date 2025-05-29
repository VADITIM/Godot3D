using System;
using Godot;

public partial class Movement : Node
{
    public float currentSpeed = 0.0f;
    [Export] public float maxSpeed = 20.0f;
    [Export] public float maxSprintSpeed = 28.0f;
    [Export] public float maxAirSpeed = 500.0f;
    [Export] public float maxWallSpeed = 150.0f;

    [Export] public float groundAcceleration = 1.5f; // Acceleration rate on ground
    [Export] public float airAcceleration = 1.0f;
    [Export] public float wallAcceleration = 1.25f;

    [Export] public float groundDeceleration = 2.5f;
    [Export] public float speedExcessDeceleration = 4.5f;
    [Export] public float airDeceleration = .55f;

    [Export] public float jumpForce = 4.5f;
    [Export] public float jumpBoostDuration = 0.1f; // Duration of initial jump boost
    [Export] public float jumpBoostMultiplier = 1.2f; // Multiplier for jump boost

    public float gravity = 13.8f;
    [Export] public float fallingGravityMultiplier = 1.4f; // Gravity multiplier when falling
    [Export] public float apexGravityMultiplier = .6f; // Gravity multiplier at the apex of jump
    [Export] public float apexThreshold = 2.0f; // Velocity threshold to consider as apex

    private float jumpBoostTimer = 0f;
    private bool isJumpBoosting = false;

    public Vector3 velocity = Vector3.Zero;

    public bool isGrounded = true;
    public bool isJumping = false;
    public bool isWallJumping = false;
    public bool isSprinting = false;
    private bool wasGrounded = true;
    public Vector3 direction = Vector3.Zero;

    public bool isLateralMovementLocked = false;
    public bool isMoving = false;

    private Vector3 GetDirection()
    {
        Vector2 inputDir = Input.GetVector("left", "right", "up", "down");

        if (isLateralMovementLocked)
        {
            inputDir.X = 0;
        }

        Vector3 direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();
        direction = Components.Instance.Player.rb.Transform.Basis * direction;
        return direction;
    }

    public void HandleMovement(float delta)
    {
        Vector3 direction = GetDirection();
        isMoving = direction.LengthSquared() > 0.01f;
        bool justLanded = !wasGrounded && isGrounded;
        wasGrounded = isGrounded;

        if (Input.IsActionJustPressed("jump") && isGrounded)
        {
            // Initial jump burst
            velocity.Y = jumpForce;
            isGrounded = false;
            isJumping = true;
            isWallJumping = false; // Ensure wall jumping is false for regular jumps
            isJumpBoosting = true;
            jumpBoostTimer = 0f;
        }
        else if (Input.IsActionJustPressed("jump") && !isGrounded && !Components.Instance.WallManager.onWall)
        {
            // Allow jump boost if already jumping
            isJumpBoosting = true;
            jumpBoostTimer = 0f;
            isJumping = false;
            // Don't reset isWallJumping here - let it persist if it was a wall jump
        }

        if (isJumpBoosting)
        {
            jumpBoostTimer += delta;

            if (jumpBoostTimer <= jumpBoostDuration && isJumping)
            {
                velocity.Y += jumpForce * jumpBoostMultiplier * delta * 10;
            }
            else
            {
                isJumpBoosting = false;
            }
        }

        isSprinting = Input.IsActionPressed("sprint");

        Components.Instance.StateMachine.HandleAcceleration(direction, delta, justLanded);

        Vector3 horizontalVelocity = direction * currentSpeed;
        velocity.X = horizontalVelocity.X;
        velocity.Z = horizontalVelocity.Z;

        Components.Instance.Player.Velocity = velocity;
        Components.Instance.Player.MoveAndSlide();

        isGrounded = Components.Instance.Player.IsOnFloor();
        velocity = Components.Instance.Player.Velocity;

        if (isGrounded)
        {
            isJumping = false;
            isWallJumping = false; // Only reset wall jumping when landing
        }
    }

    public void HandleGravity(float delta)
    {
        if (Components.Instance.WallManager.onWall && isGrounded)
        {
            Components.Instance.WallManager.ForceResetWallState();
        }

        if (Components.Instance.WallManager.onWall && isSprinting)
        {
            return;
        }

        if (!isGrounded)
        {
            Components.Instance.WallManager.isWallJumping = false;

            float gravityMultiplier = 1.0f;

            if (velocity.Y < 0)
            {
                gravityMultiplier = fallingGravityMultiplier;
            }
            else if (Mathf.Abs(velocity.Y) < apexThreshold)
            {
                gravityMultiplier = apexGravityMultiplier;
            }

            velocity.Y -= gravity * gravityMultiplier * delta;

            if (isJumpBoosting && isJumping && !Input.IsActionPressed("jump") && velocity.Y > 0)
            {
                velocity.Y *= 0.5f;
            }
        }
        else
        {
            velocity.Y = 0;
        }
    }
}