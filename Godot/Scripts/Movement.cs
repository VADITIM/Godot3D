using Godot;
using System;

public partial class Movement : Node
{
    [Export] public float speed = 5.0f;
    public float currentSpeed = 0.0f;
    [Export] private float sprintMultiplier = 3.5f;
    [Export] public float jumpForce = 8.5f;
    public float gravity = 9.8f;
    public Vector3 velocity = Vector3.Zero;
    private bool isGrounded = true;

    private bool isJumping = false;
    private bool isSprinting = false;


    private Vector3 GetDirection()
    {
        Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
        Vector3 direction = new Vector3(inputDir.X, 0, inputDir.Y).Normalized();

        direction = PlayerComponents.Instance.Player.rb.Transform.Basis * direction;

        return direction;
    }

    public void HandleMovement(float delta)
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

        currentSpeed = speed;

        if (Input.IsActionPressed("sprint"))
        {
            currentSpeed *= sprintMultiplier;
            isSprinting = true;
        }

        horizontalVelocity *= currentSpeed;

        velocity.X = horizontalVelocity.X;
        velocity.Z = horizontalVelocity.Z;

        PlayerComponents.Instance.Player.Velocity = velocity;
        PlayerComponents.Instance.Player.MoveAndSlide();

        isGrounded = PlayerComponents.Instance.Player.IsOnFloor();
        velocity = PlayerComponents.Instance.Player.Velocity;
    }
    
    
    public void HandleGravity(float delta)
    {
        if (!isGrounded)
            velocity.Y -= gravity * delta;
        else
            velocity.Y = 0;
    }


}
