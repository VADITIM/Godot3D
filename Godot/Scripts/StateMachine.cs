using Godot;
using System;
using System.Collections.Generic;

public partial class StateMachine : Node
{
    public event Action<string> StateChanged;
    private string currentState = "";
    private string previousState = "";
    public string CurrentState => currentState;
    public string PreviousState => previousState;
    private float stateUpdateInterval = 0.05f;
    private float stateUpdateTimer = 0f;
    public void TriggerState(string stateName) { OnStateChanged(stateName); }

    private bool isTransitioningFromJumpToFall = false;
    private bool isTransitioningFromFallToGround = false;

    private bool jumping;
    private bool falling;
    private bool moving;
    private bool sprinting;
    private bool grounded;
    private bool onWall;
    private bool wallJump;
    private float velocityY;
    private float currentSpeed;
    private Vector3 direction;

    public Color GetStateColor(string state) => StateColors.ContainsKey(state) ? StateColors[state] : Colors.Gray;
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

    public override void _Process(double delta)
    {
        stateUpdateTimer += (float)delta;

        if (stateUpdateTimer >= stateUpdateInterval)
        {
            stateUpdateTimer = 0;
            UpdatePrivateBools();
            UpdateCurrentState();
        }
    }

    private void UpdatePrivateBools()
    {
        jumping = Components.Instance.Movement.isJumping;
        falling = Components.Instance.Movement.velocity.Y < 0;
        moving = Components.Instance.Movement.isMoving;
        sprinting = Components.Instance.Movement.isSprinting;
        grounded = Components.Instance.Movement.isGrounded;
        onWall = Components.Instance.WallManager.onWall;
        wallJump = Components.Instance.WallManager.isWallJumping;
        velocityY = Components.Instance.Movement.velocity.Y;
        currentSpeed = Components.Instance.Movement.currentSpeed;
        direction = Components.Instance.Movement.direction;
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
        if (wallJump && velocityY > 0 && !grounded)
            return "Wall Jumping";

        if (onWall && (moving || sprinting) && !grounded)
            return "Wall Running";

        if (!grounded)
        {
            if (onWall && Input.IsActionJustPressed("jump"))
                return "Wall Jumping";
            else if (jumping && velocityY > 0)
                return "Jumping";
            else if (velocityY < 0)
                return "Falling";
            else
                return "Jumping";
        }

        if (sprinting) return "Sprinting";
        if (moving) return "Moving";

        return "Idle";
    }

    public string GetCurrentMovementState()
    {
        State state = GetMovementState(direction);

        return state switch
        {
            State.WallMoving => "Wall Running",
            State.WallJumping => "Wall Jumping",
            State.Sprinting => "Sprinting",
            State.Moving => "Moving",
            State.Airborne => "Airborne",
            State.Falling => "Falling",
            State.OnGround => "Idle",
            _ => "Idle"
        };
    }

    private State GetMovementState(Vector3 direction)
    {
        bool isMoving = direction.LengthSquared() > 0.01f;

        if (onWall && (sprinting || moving) && !grounded)
            return State.WallMoving;

        if (onWall && Input.IsActionJustPressed("jump") && !grounded)
            return State.WallJumping;

        if (isMoving && sprinting && grounded)
            return State.Sprinting;

        if (isMoving && grounded)
            return State.Moving;

        if (!grounded && velocityY > 0)
            return State.Airborne;

        if (!grounded && velocityY < 0)
            return State.Falling;

        if (grounded)
            return State.OnGround;

        return State.Idle;
    }

    public void HandleAcceleration(Vector3 direction, float delta, bool justLanded = false)
    {
        State state = GetMovementState(direction);
        bool isMoving = direction.LengthSquared() > 0.01f;
        float maxSpeed = 0f;

        switch (state)
        {
            case State.Sprinting: maxSpeed = Components.Instance.Movement.maxSprintSpeed; break;
            case State.Moving: maxSpeed = Components.Instance.Movement.maxSpeed; break;
            case State.WallMoving: maxSpeed = Components.Instance.Movement.maxWallSpeed; break;
            case State.Airborne: maxSpeed = 0f; break;
        }

        ProcessStates(delta);

        if (isMoving)
        {
            float accelRate = grounded ?
                Components.Instance.Movement.groundAcceleration :
                (onWall ?
                    Components.Instance.Movement.wallAcceleration :
                    Components.Instance.Movement.airAcceleration);

            if (currentSpeed < maxSpeed)
            {
                Components.Instance.Movement.currentSpeed += accelRate * delta * 10;
                Components.Instance.Movement.currentSpeed = Mathf.Min(Components.Instance.Movement.currentSpeed, maxSpeed);
            }
            else if (grounded && currentSpeed > maxSpeed)
            {
                Components.Instance.Movement.currentSpeed = Mathf.MoveToward(
                    Components.Instance.Movement.currentSpeed,
                    maxSpeed,
                    Components.Instance.Movement.speedExcessDeceleration * delta * 10);
            }
        }
        else
        {
            if (grounded)
            {
                Components.Instance.Movement.currentSpeed = Mathf.MoveToward(
                    Components.Instance.Movement.currentSpeed,
                    0,
                    Components.Instance.Movement.groundDeceleration * delta * 10);
            }
        }

        Components.Instance.Movement.currentSpeed = Mathf.Max(Components.Instance.Movement.currentSpeed, 0);
    }

    public void ProcessStates(float delta)
    {
        State state = GetMovementState(direction);

        if (state == State.WallMoving)
        {
            Components.Instance.Movement.currentSpeed = Mathf.MoveToward(
                Components.Instance.Movement.currentSpeed,
                Components.Instance.Movement.maxWallSpeed,
                Components.Instance.Movement.wallAcceleration * delta * 10);
        }
        if (state == State.Airborne)
        {
            if (currentSpeed < 30)
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

        if (state == State.Falling)
        {
            if (currentSpeed < 30)
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
    }

    private void OnStateChanged(string newState)
    {
        if (currentState == newState) return;

        isTransitioningFromJumpToFall = (currentState == "Jumping" || currentState == "Airborne") && newState == "Falling";
        isTransitioningFromFallToGround = currentState == "Falling" && (newState == "Idle" || newState == "Moving" || newState == "Sprinting");

        previousState = currentState;
        currentState = newState;
        StateChanged?.Invoke(newState);

        Components.Instance.UIAnimations.UpdatePreviousState(previousState);
        Components.Instance.UIAnimations.UpdateCurrentPositions();

        TriggerUIAnimation(newState);
    }

    private void TriggerUIAnimation(string state)
    {
        if (Components.Instance.UIAnimations == null) return;

        if (isTransitioningFromFallToGround && (state == "Idle" || state == "Moving" || state == "Sprinting"))
        {
            Components.Instance.UIAnimations.ElasticAnimation(state);
        }
        else
        {
            switch (state)
            {
                case "Wall Running":
                    Components.Instance.UIAnimations.WallRunAnimation();
                    break;
                case "Wall Jumping":
                    Components.Instance.UIAnimations.WallJumpAnimation();
                    break;
                case "Jumping":
                    Components.Instance.UIAnimations.JumpAnimation();
                    break;
                case "Falling":
                    Components.Instance.UIAnimations.FallAnimation();
                    break;
                case "Sprinting":
                    Components.Instance.UIAnimations.SprintAnimation();
                    break;
                case "Moving":
                    Components.Instance.UIAnimations.MoveAnimation();
                    break;
                case "Idle":
                    Components.Instance.UIAnimations.IdleAnimation();
                    break;
                default:
                    Components.Instance.UIAnimations.ResetToIdle();
                    break;
            }
        }

        isTransitioningFromJumpToFall = false;
        isTransitioningFromFallToGround = false;
    }
}
