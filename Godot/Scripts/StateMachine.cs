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
        { "Moving", Colors.DeepSkyBlue },
        { "Idle", Colors.LightGray },
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
        bool isMoving = Components.Instance.Movement.direction.LengthSquared() > 0.01f;
        
        if (Components.Instance.WallManager.isWallJumping)
            return "Wall Jumping";

        if (Components.Instance.WallManager.onWall && Components.Instance.Movement.isSprinting)
            return "Wall Running";

        if (!Components.Instance.Movement.isGrounded)
        {
            if (Components.Instance.Movement.isJumping && Components.Instance.Movement.velocity.Y > 0)
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

        return "Idle";
    }
    private void OnStateChanged(string newState)
    {
        if (currentState == newState) return;

        previousState = currentState;
        currentState = newState;
        StateChanged?.Invoke(newState);

        // Update GameUI with state information
        if (Components.Instance.UIAnimations != null)
        {
            Components.Instance.UIAnimations.UpdatePreviousState(previousState);
            Components.Instance.UIAnimations.UpdateCurrentPositions();
        }

        TriggerUIAnimation(newState);
    }

    public void TriggerState(string stateName)
    {
        OnStateChanged(stateName);
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
