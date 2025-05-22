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
    public bool isSprinting = false;
    private bool wasGrounded = true;
    public Vector3 direction = Vector3.Zero;

    public bool isLateralMovementLocked = false;

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
        bool justLanded = !wasGrounded && isGrounded;
        wasGrounded = isGrounded;

        if (Input.IsActionJustPressed("jump") && isGrounded)
        {
            // Initial jump burst
            velocity.Y = jumpForce;
            isGrounded = false;
            isJumping = true;
            isJumpBoosting = true;
            jumpBoostTimer = 0f;
        }

        // Handle jump boost for responsiveness
        if (isJumpBoosting)
        {
            jumpBoostTimer += delta;
            
            if (jumpBoostTimer <= jumpBoostDuration && isJumping)
            {
                // Always apply boost for initial duration to ensure consistent jump height
                velocity.Y += jumpForce * jumpBoostMultiplier * delta * 10;
            }
            else
            {
                isJumpBoosting = false;
            }
        }

        isSprinting = Input.IsActionPressed("sprint");

        HandleAcceleration(direction, delta, justLanded);

        Vector3 horizontalVelocity = direction * currentSpeed;
        velocity.X = horizontalVelocity.X;
        velocity.Z = horizontalVelocity.Z;

        Components.Instance.Player.Velocity = velocity;
        Components.Instance.Player.MoveAndSlide();

        isGrounded = Components.Instance.Player.IsOnFloor();
        velocity = Components.Instance.Player.Velocity;

        if (isGrounded)
            isJumping = false;
    }

    public void HandleGravity(float delta)
    {
        // Don't apply gravity when on walls - this is now managed by Walling class
        if (Components.Instance.WallManager.onWall && isSprinting)
        {
            return;
        }

        if (!isGrounded)
        {
            Components.Instance.WallManager.isWallJumping = false;
            
            float gravityMultiplier = 1.0f;
            
            // Apply higher gravity when falling for faster descent
            if (velocity.Y < 0) 
            {
                gravityMultiplier = fallingGravityMultiplier;
            }
            // Apply slightly higher gravity at the apex to reduce hovering
            else if (Mathf.Abs(velocity.Y) < apexThreshold)
            {
                gravityMultiplier = apexGravityMultiplier;
            }
            
            // Apply gravity with appropriate multiplier
            velocity.Y -= gravity * gravityMultiplier * delta;
            
            // Only cut momentum if the player is still in jump boost phase
            // This allows for variable jump height during initial jump only
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

    private enum MovementState
    {
        Idle,
        Moving,
        Sprinting,
        WallMoving,
        Airborne,
        Falling,
        OnGround
    }

    private MovementState GetMovementState(Vector3 direction)
    {
        bool isMoving = direction.LengthSquared() > 0.01f;

        if (Components.Instance.WallManager.onWall && isMoving) return MovementState.WallMoving;

        if (isMoving && isSprinting && isGrounded) return MovementState.Sprinting;

        if (isMoving && isGrounded) return MovementState.Moving;

        if (!isGrounded && velocity.Y > 0) return MovementState.Airborne;

        if (!isGrounded && velocity.Y < 0) return MovementState.Falling;

        if (isGrounded) return MovementState.OnGround;

        return MovementState.Idle;
    }

    private void HandleAcceleration(Vector3 direction, float delta, bool justLanded = false)
    {
        MovementState state = GetMovementState(direction);
        bool isMoving = direction.LengthSquared() > 0.01f;
        float maxSpeed = 0f;

        switch (state)
        {
            case MovementState.Sprinting:   maxSpeed = maxSprintSpeed;  break;
            case MovementState.Moving:      maxSpeed = this.maxSpeed;   break;
            case MovementState.WallMoving:  maxSpeed = maxWallSpeed;    break;
            case MovementState.Airborne:    maxSpeed = 0f;              break;
        }

        States(delta);


        if (isMoving)
        {
            float accelRate = isGrounded ? groundAcceleration : (Components.Instance.WallManager.onWall ? wallAcceleration : airAcceleration);

            if (currentSpeed < maxSpeed)
            {
                currentSpeed += accelRate * delta * 10;
                currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
            }

            else if (isGrounded && currentSpeed > maxSpeed)
            {
                currentSpeed = Mathf.MoveToward(currentSpeed, maxSpeed, speedExcessDeceleration * delta * 10);
            }
        }
        else
        {
            if (isGrounded)
            {
                currentSpeed = Mathf.MoveToward(currentSpeed, 0, groundDeceleration * delta * 10);
            }
        }

        currentSpeed = Mathf.Max(currentSpeed, 0);
    }

    public void States(float delta)
    {
        MovementState state = GetMovementState(direction);

        if (state == MovementState.WallMoving)
        {
            currentSpeed = Mathf.MoveToward(currentSpeed, maxWallSpeed, wallAcceleration * delta * 10);
        }

        if (state == MovementState.Airborne)
        {
            GameUI.Instance.MoveLabel();
            if (currentSpeed < 30)
            {
                airDeceleration = 0.1f;
                currentSpeed = Mathf.MoveToward(currentSpeed, 0, airDeceleration / 3 * delta * 10);
            }
            else
            {
                airDeceleration = 0.9f;
                currentSpeed = Mathf.MoveToward(currentSpeed, 0, airDeceleration * delta * 10);
            }
        }

        if (state == MovementState.Falling)
        {
            GameUI.Instance.BounceLabel();
            if (currentSpeed < 30)
            {
                airDeceleration = 0.1f;
                currentSpeed = Mathf.MoveToward(currentSpeed, 0, airDeceleration / 3 * delta * 10);
            }
            else
            {
                airDeceleration = 0.9f;
                currentSpeed = Mathf.MoveToward(currentSpeed, 0, airDeceleration * delta * 10);
            }
        }

        if (state == MovementState.OnGround)
        {
            GameUI.Instance.SnapLabel();

        }

    }
}