using System;
using Godot;

public partial class Movement : Node
{
    [Export] public float baseSpeed = 20.0f; 
    [Export] public float sprintSpeed = 28.0f;
    [Export] public float maxAirSpeed = 500.0f; 
    [Export] public float maxWallSpeed = 150.0f;
    
    [Export] public float groundAcceleration = 1.5f; // Acceleration rate on ground
    [Export] public float airAcceleration = 1.0f;
    [Export] public float wallAcceleration = 1.15f;
    
    [Export] public float groundDeceleration = 2.5f;
    [Export] public float speedExcessDeceleration = 4.5f; 
    [Export] public float airDeceleration = .9f;

    [Export] public float jumpForce = 15.5f;

    public float gravity = 13.8f;
    public float currentSpeed = 0.0f;
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

    public void LockLateralMovement()
    {
        isLateralMovementLocked = true;
    }

    public void UnlockLateralMovement()
    {
        isLateralMovementLocked = false;
    }

    public void SetLateralMovementLock(bool locked)
    {
        isLateralMovementLocked = locked;
    }


    public void HandleMovement(float delta)
    {
        Vector3 direction = GetDirection();
        bool justLanded = !wasGrounded && isGrounded;
        wasGrounded = isGrounded;

        if (Input.IsActionJustPressed("jump") && isGrounded)
        {
            velocity.Y = jumpForce;
            isGrounded = false;
            isJumping = true;
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
        if (!isGrounded)
        {
            Components.Instance.WallManager.isWallJumping = false;
            velocity.Y -= gravity * delta;
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
        Airborne
    }

    private MovementState GetMovementState(Vector3 direction)
    {
        bool isMoving = direction.LengthSquared() > 0;

        if (Components.Instance.WallManager.onWall && isMoving)
            return MovementState.WallMoving;

        if (isMoving && isSprinting && isGrounded)
            return MovementState.Sprinting;

        if (isMoving && isGrounded)
            return MovementState.Moving;

        if (!isGrounded)
            return MovementState.Airborne;

        return MovementState.Idle;
    }

    private void HandleAcceleration(Vector3 direction, float delta, bool justLanded = false)
    {
        MovementState state = GetMovementState(direction);
        bool hasInput = direction.LengthSquared() > 0.01f;
        float speedThreshold = 0f;

        switch (state)
        {
            case MovementState.Sprinting:
                speedThreshold = sprintSpeed;
                break;
            case MovementState.Moving:
                speedThreshold = baseSpeed;
                break;
            case MovementState.WallMoving:
                speedThreshold = maxWallSpeed;
                break;
            case MovementState.Airborne:
                speedThreshold = 0f;
                break;
        }
        
        if (hasInput)
        {
            float accelRate = isGrounded ? groundAcceleration : (Components.Instance.WallManager.onWall ? wallAcceleration : airAcceleration);
            
            if (currentSpeed < speedThreshold)
            {
                currentSpeed += accelRate * delta * 10;
                currentSpeed = Mathf.Min(currentSpeed, speedThreshold);
            }
            
            else if (isGrounded && currentSpeed > speedThreshold)
            {
                currentSpeed = Mathf.MoveToward(currentSpeed, speedThreshold, speedExcessDeceleration * delta * 10);
            }
        }
        else
        {
            if (isGrounded)
            {
                currentSpeed = Mathf.MoveToward(currentSpeed, 0, groundDeceleration * delta * 10);
				// Components.Instance.Movement.UnlockLateralMovement();
            }
        }

        if (state == MovementState.WallMoving)
        {
            if (Components.Instance.WallManager.onWall)
            {
                currentSpeed = Mathf.MoveToward(currentSpeed, maxWallSpeed, wallAcceleration * delta * 10);
                // Components.Instance.Movement.LockLateralMovement();
            }
        }

        if (state == MovementState.Airborne)
        {
            if (currentSpeed < 30)
            {
                airDeceleration = 0.1f;
                currentSpeed = Mathf.MoveToward(currentSpeed, 0, airDeceleration / 3 * delta * 10);
				// Components.Instance.Movement.UnlockLateralMovement();
            }
            else
            {
                airDeceleration = 0.9f;
                currentSpeed = Mathf.MoveToward(currentSpeed, 0, airDeceleration * delta * 10);
				// Components.Instance.Movement.UnlockLateralMovement();
            }
        }
        
        currentSpeed = Mathf.Max(currentSpeed, 0);
    }
}