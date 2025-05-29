using Godot;
using System;
using System.Collections.Generic;

public partial class StateMachine : Node
{
    [Export] private float stateUpdateInterval = 0.05f;

    private float stateUpdateTimer = 0f;

    private string currentState = "";
    private string previousState = "";

    // Movement state enum for internal logic
    private enum MovementState
    {
        Idle,
        Moving,
        Sprinting,
        WallMoving,
        WallJumping,
        Airborne,
        Falling,
        OnGround
    }

    // State colors for UI
    public readonly Dictionary<string, Color> StateColors = new Dictionary<string, Color>
    {
        { "Wall Running", Colors.Cyan },
        { "Wall Jumping", Colors.Orange },
        { "Jumping", Colors.LimeGreen },
        { "Falling", Colors.OrangeRed },
        { "Sprinting", Colors.Purple },
        { "Moving", Colors.DeepSkyBlue },
        { "Idle", Colors.LightGray },
        { "Airborne", Colors.Yellow },
        { "Unknown", Colors.Gray }
    };

    public event Action<string> StateChanged;

    public string CurrentState => currentState;
    public string PreviousState => previousState;
    public Color GetStateColor(string state) => StateColors.ContainsKey(state) ? StateColors[state] : Colors.Gray;

    public override void _Ready()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
    }

    public override void _Process(double delta)
    {
        stateUpdateTimer += (float)delta;

        if (stateUpdateTimer >= stateUpdateInterval)
        {
            stateUpdateTimer = 0;
            UpdateCurrentState();
        }
    }

    private void UpdateCurrentState()
    {
        string newState = DetectCurrentState();
        if (newState != currentState)
        {
            OnStateChanged(newState);
        }
    }
    private string DetectCurrentState()
    {
        bool isMoving = Components.Instance.Movement.isMoving;

        if (Components.Instance.WallManager.isWallJumping && Components.Instance.Movement.velocity.Y > 0)
            return "Wall Jumping";

        if (Components.Instance.WallManager.onWall && Components.Instance.Movement.isSprinting && !Components.Instance.Movement.isGrounded)
            return "Wall Running";

        if (!Components.Instance.Movement.isGrounded)
        {
            if (Components.Instance.WallManager.onWall && Input.IsActionJustPressed("jump"))
                return "Wall Jumping";
            else if (Components.Instance.Movement.isJumping && Components.Instance.Movement.velocity.Y > 0)
                return "Jumping";
            else if (Components.Instance.Movement.velocity.Y < 0)
                return "Falling";
            else
                return "Airborne";
        }

        if (Components.Instance.Movement.isSprinting)
            return "Sprinting";

        if (isMoving)
            return "Moving";
        else
            return "Idle";
    }

    // Movement state logic - moved from Movement.cs
    private MovementState GetMovementState(Vector3 direction)
    {
        bool isMoving = direction.LengthSquared() > 0.01f;

        if (Components.Instance.WallManager.onWall && Components.Instance.Movement.isSprinting && !Components.Instance.Movement.isGrounded)
            return MovementState.WallMoving;

        if (Components.Instance.WallManager.onWall && Input.IsActionJustPressed("jump") && !Components.Instance.Movement.isGrounded)
            return MovementState.WallJumping;

        if (isMoving && Components.Instance.Movement.isSprinting && Components.Instance.Movement.isGrounded)
            return MovementState.Sprinting;

        if (isMoving && Components.Instance.Movement.isGrounded)
            return MovementState.Moving;

        if (!Components.Instance.Movement.isGrounded && Components.Instance.Movement.velocity.Y > 0)
            return MovementState.Airborne;

        if (!Components.Instance.Movement.isGrounded && Components.Instance.Movement.velocity.Y < 0)
            return MovementState.Falling;

        if (Components.Instance.Movement.isGrounded)
            return MovementState.OnGround;

        return MovementState.Idle;
    }

    // Movement acceleration logic - moved from Movement.cs
    public void HandleAcceleration(Vector3 direction, float delta, bool justLanded = false)
    {
        MovementState state = GetMovementState(direction);
        bool isMoving = direction.LengthSquared() > 0.01f;
        float maxSpeed = 0f;

        switch (state)
        {
            case MovementState.Sprinting: maxSpeed = Components.Instance.Movement.maxSprintSpeed; break;
            case MovementState.Moving: maxSpeed = Components.Instance.Movement.maxSpeed; break;
            case MovementState.WallMoving: maxSpeed = Components.Instance.Movement.maxWallSpeed; break;
            case MovementState.Airborne: maxSpeed = 0f; break;
        }

        ProcessMovementStates(delta);

        if (isMoving)
        {
            float accelRate = Components.Instance.Movement.isGrounded ?
                Components.Instance.Movement.groundAcceleration :
                (Components.Instance.WallManager.onWall ?
                    Components.Instance.Movement.wallAcceleration :
                    Components.Instance.Movement.airAcceleration);

            if (Components.Instance.Movement.currentSpeed < maxSpeed)
            {
                Components.Instance.Movement.currentSpeed += accelRate * delta * 10;
                Components.Instance.Movement.currentSpeed = Mathf.Min(Components.Instance.Movement.currentSpeed, maxSpeed);
            }
            else if (Components.Instance.Movement.isGrounded && Components.Instance.Movement.currentSpeed > maxSpeed)
            {
                Components.Instance.Movement.currentSpeed = Mathf.MoveToward(
                    Components.Instance.Movement.currentSpeed,
                    maxSpeed,
                    Components.Instance.Movement.speedExcessDeceleration * delta * 10);
            }
        }
        else
        {
            if (Components.Instance.Movement.isGrounded)
            {
                Components.Instance.Movement.currentSpeed = Mathf.MoveToward(
                    Components.Instance.Movement.currentSpeed,
                    0,
                    Components.Instance.Movement.groundDeceleration * delta * 10);
            }
        }

        Components.Instance.Movement.currentSpeed = Mathf.Max(Components.Instance.Movement.currentSpeed, 0);
    }

    // Movement states processing - moved from Movement.cs
    public void ProcessMovementStates(float delta)
    {
        MovementState state = GetMovementState(Components.Instance.Movement.direction);

        if (state == MovementState.WallMoving)
        {
            Components.Instance.Movement.currentSpeed = Mathf.MoveToward(
                Components.Instance.Movement.currentSpeed,
                Components.Instance.Movement.maxWallSpeed,
                Components.Instance.Movement.wallAcceleration * delta * 10);
        }

        if (state == MovementState.Airborne)
        {
            if (Components.Instance.UIAnimations != null)
                Components.Instance.UIAnimations.MoveLabel();

            if (Components.Instance.Movement.currentSpeed < 30)
            {
                Components.Instance.Movement.airDeceleration = 0.1f;
                Components.Instance.Movement.currentSpeed = Mathf.MoveToward(
                    Components.Instance.Movement.currentSpeed,
                    0,
                    Components.Instance.Movement.airDeceleration / 3 * delta * 10);
            }
            else
            {
                Components.Instance.Movement.airDeceleration = 0.9f;
                Components.Instance.Movement.currentSpeed = Mathf.MoveToward(
                    Components.Instance.Movement.currentSpeed,
                    0,
                    Components.Instance.Movement.airDeceleration * delta * 10);
            }
        }

        if (state == MovementState.Falling)
        {
            if (Components.Instance.UIAnimations != null)
                Components.Instance.UIAnimations.BounceLabel();

            if (Components.Instance.Movement.currentSpeed < 30)
            {
                Components.Instance.Movement.airDeceleration = 0.1f;
                Components.Instance.Movement.currentSpeed = Mathf.MoveToward(
                    Components.Instance.Movement.currentSpeed,
                    0,
                    Components.Instance.Movement.airDeceleration / 3 * delta * 10);
            }
            else
            {
                Components.Instance.Movement.airDeceleration = 0.9f;
                Components.Instance.Movement.currentSpeed = Mathf.MoveToward(
                    Components.Instance.Movement.currentSpeed,
                    0,
                    Components.Instance.Movement.airDeceleration * delta * 10);
            }
        }

        if (state == MovementState.OnGround)
        {
            if (Components.Instance.UIAnimations != null)
                Components.Instance.UIAnimations.SnapLabel();
        }
    }
    private void OnStateChanged(string newState)
    {
        if (currentState == newState) return;

        previousState = currentState;
        currentState = newState;
        StateChanged?.Invoke(newState);

        Components.Instance.UIAnimations.UpdatePreviousState(previousState);
        Components.Instance.UIAnimations.UpdateCurrentPositions();

        TriggerUIAnimation(newState);
    }

    public void TriggerState(string stateName)
    {
        OnStateChanged(stateName);
    }

    public string GetCurrentMovementState()
    {
        Vector3 direction = Components.Instance.Movement.direction;
        MovementState state = GetMovementState(direction);

        return state switch
        {
            MovementState.WallMoving => "Wall Running",
            MovementState.WallJumping => "Wall Jumping",
            MovementState.Sprinting => "Sprinting",
            MovementState.Moving => "Moving",
            MovementState.Airborne => "Airborne",
            MovementState.Falling => "Falling",
            MovementState.OnGround => "Idle",
            _ => "Idle"
        };
    }

    private void TriggerUIAnimation(string state)
    {
        if (Components.Instance.UIAnimations == null) return;

        switch (state)
        {
            case "Wall Running": Components.Instance.UIAnimations.WallRunAnimation(); break;
            case "Wall Jumping": Components.Instance.UIAnimations.WallJumpAnimation(); break;
            case "Jumping": Components.Instance.UIAnimations.JumpAnimation(); break;
            case "Falling": Components.Instance.UIAnimations.FallAnimation(); break;
            case "Sprinting": Components.Instance.UIAnimations.SprintAnimation(); break;
            case "Moving": Components.Instance.UIAnimations.MoveAnimation(); break;
            case "Idle": Components.Instance.UIAnimations.IdleAnimation(); break;
            default: Components.Instance.UIAnimations.ResetToIdle(); break;
        }
    }
}
