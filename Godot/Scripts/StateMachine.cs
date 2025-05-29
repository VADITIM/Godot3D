using Godot;
using System;
using System.Collections.Generic;

public partial class StateMachine : Node
{
    [Export] private float stateUpdateInterval = 0.05f;

    private float stateUpdateTimer = 0f;

    private string currentState = "";
    private string previousState = "";

    public readonly Dictionary<string, Color> StateColors = new Dictionary<string, Color>
    {
        { "Wall Running", Colors.Cyan },
        { "Wall Jumping", Colors.Orange },
        { "Jumping", Colors.LimeGreen },
        { "Falling", Colors.OrangeRed },
        { "Sprinting", Colors.Purple },
        { "Running", Colors.DeepSkyBlue },
        { "Walking", Colors.LightBlue },
        { "Moving", Colors.DeepSkyBlue },
        { "Grounded", Colors.White },
        { "Idle", Colors.LightGray },
        { "Airborne", Colors.Yellow },
        { "Unknown", Colors.Gray }
    }; public event Action<string> StateChanged;

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
        if (Components.Instance.WallManager.onWall && Components.Instance.Movement.isSprinting)
            return "Wall Running";

        if (Components.Instance.WallManager.isWallJumping)
            return "Wall Jumping";

        if (!Components.Instance.Movement.isGrounded)
        {
            if (Components.Instance.Movement.isJumping && Components.Instance.Movement.velocity.Y > 0)
                return "Jumping";
            else if (Components.Instance.Movement.velocity.Y < 0)
                return "Falling";
            else
                return "Airborne";
        }

        bool isMoving = Components.Instance.Movement.direction.LengthSquared() > 0.01f;

        if (!isMoving)
            return "Idle";

        if (Components.Instance.Movement.isSprinting)
            return "Sprinting";

        if (Components.Instance.Movement.currentSpeed > Components.Instance.Movement.maxSpeed * 0.7f)
            return "Running";
        else if (Components.Instance.Movement.currentSpeed > 0.1f)
            return "Walking";

        return "Grounded";
    }
    private void OnStateChanged(string newState)
    {
        if (currentState == newState) return;

        previousState = currentState;
        currentState = newState;
        StateChanged?.Invoke(newState);

        // Update GameUI with state information
        if (Components.Instance.GameUI != null)
        {
            Components.Instance.GameUI.UpdatePreviousState(previousState);
            Components.Instance.GameUI.UpdateCurrentPositions();
        }

        TriggerUIAnimation(newState);
    }

    public void TriggerState(string stateName)
    {
        OnStateChanged(stateName);
    }

    private void TriggerUIAnimation(string state)
    {
        if (Components.Instance.GameUI == null) return;

        switch (state)
        {
            case "Wall Running": Components.Instance.GameUI.WallRunAnimation(); break;
            case "Wall Jumping": Components.Instance.GameUI.WallJumpAnimation(); break;
            case "Jumping": Components.Instance.GameUI.JumpAnimation(); break;
            case "Falling": Components.Instance.GameUI.FallAnimation(); break;
            case "Sprinting": Components.Instance.GameUI.SprintAnimation(); break;
            case "Running": Components.Instance.GameUI.RunAnimation(); break;
            case "Walking": Components.Instance.GameUI.WalkAnimation(); break;
            case "Grounded":
            case "Idle": Components.Instance.GameUI.IdleAnimation(); break;
            default: Components.Instance.GameUI.ResetToIdle(); break;
        }
    }
}
