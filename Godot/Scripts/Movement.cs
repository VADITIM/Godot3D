using System;
using Godot;

public partial class Movement : Node
{
    [Export] public float baseSpeed = 10.0f;
    [Export] public float maxSpeed = 30.0f;
    [Export] public float acceleration = 5.5f;
    [Export] public float deceleration = 15f;
    [Export] public float jumpForce = 8.5f;
    [Export] private float sprintMultiplier = 1.55f;

    public float gravity = 9.8f;
    public float currentSpeed = 0.0f;
    public Vector3 velocity = Vector3.Zero;

    public bool isGrounded = true;
    public bool isJumping = false;
    public bool isSprinting = false;

    private Vector3 GetDirection()
    {
        Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
        Vector3 direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();
        direction = PlayerComponents.Instance.Player.rb.Transform.Basis * direction;
        return direction;
    }

    public void HandleMovement(float delta)
    {
        Vector3 direction = GetDirection();

        if (Input.IsActionJustPressed("jump") && isGrounded)
        {
            velocity.Y = jumpForce;
            isGrounded = false;
        }

        isSprinting = Input.IsActionPressed("sprint");

        ApplyAcceleration(direction, delta);

        Vector3 horizontalVelocity = direction * currentSpeed;
        velocity.X = horizontalVelocity.X;
        velocity.Z = horizontalVelocity.Z;

        PlayerComponents.Instance.Player.Velocity = velocity;
        PlayerComponents.Instance.Player.MoveAndSlide();

        isGrounded = PlayerComponents.Instance.Player.IsOnFloor();
        velocity = PlayerComponents.Instance.Player.Velocity;

    }

    private void ApplyAcceleration(Vector3 direction, float delta)
    {
        bool isWalling = PlayerComponents.Instance.WallManager.isWalling;
        float accelRate = acceleration * (isWalling ? 2f : 1f); // Keep wall-specific accel if intended
        float targetSpeed = isSprinting ? baseSpeed * sprintMultiplier : baseSpeed;

        if (direction.LengthSquared() > 0)
        {
            // Smoothly interpolate currentSpeed toward targetSpeed
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, accelRate * delta);
        }
        else
        {
            // Smoothly decelerate to 0
            currentSpeed = Mathf.Lerp(currentSpeed, 0, deceleration * delta);
        }

        // Allow currentSpeed to go up to maxSpeed
        currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);
    }
    public void HandleGravity(float delta)
    {
        if (!isGrounded)
        {
            PlayerComponents.Instance.WallManager.isWallJumping = false;
            velocity.Y -= gravity * delta;
            baseSpeed = 10;
        }
        else
            velocity.Y = 0;
    }


}
