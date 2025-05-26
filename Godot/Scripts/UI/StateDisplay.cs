using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class StateDisplay : Label
{
    [Export] private NodePath movementNodePath;
    [Export] private NodePath wallingNodePath;
    [Export] private string prefix = "State: ";
    [Export] private float updateInterval = 0.05f; // Faster updates for smoother transitions
    [Export] private bool enableColorTransitions = true;
    [Export] private bool enableGameUIIntegration = true;

    private Movement movementComponent;
    private Walling wallingComponent;
    private float updateTimer = 0f;
    private string lastState = "";
    private Tween colorTween;

    // State colors for smooth transitions
    private readonly Dictionary<string, Color> stateColors = new Dictionary<string, Color>
    {
        { "Wall Running", Colors.Cyan },
        { "Wall Jumping", Colors.Orange },
        { "Jumping", Colors.LimeGreen },
        { "Falling", Colors.OrangeRed },
        { "Airborne", Colors.SkyBlue },
        { "Sprinting", Colors.Purple },
        { "Running", Colors.DeepSkyBlue },
        { "Walking", Colors.LightBlue },
        { "Grounded", Colors.White },
        { "Idle", Colors.LightGray },
        { "Unknown", Colors.Gray }
    };

    public override void _Ready()
    {
        // Try to get Movement component
        if (movementNodePath != null && !movementNodePath.IsEmpty)
        {
            Node targetNode = GetNode(movementNodePath);
            if (targetNode != null && targetNode is Movement)
            {
                movementComponent = targetNode as Movement;
            }
        }

        if (movementComponent == null && Components.Instance != null && Components.Instance.Movement != null)
        {
            movementComponent = Components.Instance.Movement;
        }

        // Try to get Walling component
        if (wallingNodePath != null && !wallingNodePath.IsEmpty)
        {
            Node targetNode = GetNode(wallingNodePath);
            if (targetNode != null && targetNode is Walling)
            {
                wallingComponent = targetNode as Walling;
            }
        }

        if (wallingComponent == null && Components.Instance != null && Components.Instance.WallManager != null)
        {
            wallingComponent = Components.Instance.WallManager;
        }

        if (movementComponent == null)
        {
            Text = "Movement component not found!";
            GD.PrintErr("StateDisplay: Movement component not found!");
        }
    }

    public override void _Process(double delta)
    {
        updateTimer += (float)delta;

        if (updateTimer >= updateInterval && movementComponent != null)
        {
            updateTimer = 0;
            UpdateStateDisplay();
        }
    }
    private void UpdateStateDisplay()
    {
        string currentState = GetCurrentState();

        // Only update if state has changed
        if (currentState != lastState)
        {
            lastState = currentState;
            Text = $"{prefix}{currentState}";

            // Trigger smooth color transition
            if (enableColorTransitions)
            {
                AnimateColorTransition(currentState);
            }

            // Integrate with GameUI for enhanced animations
            if (enableGameUIIntegration && GameUI.Instance != null)
            {
                GameUI.Instance.OnStateChanged(currentState);
            }
        }
    }
    private void AnimateColorTransition(string state)
    {
        if (!stateColors.ContainsKey(state)) return;

        Color targetColor = stateColors[state];

        // Kill existing color tween
        colorTween?.Kill();

        // Get current color or use white as default
        Color currentColor = GetThemeColor("font_color", "Label");
        if (currentColor == Colors.Black) // Default theme color, use white instead
            currentColor = Colors.White;

        // Create smooth color transition
        colorTween = CreateTween();
        colorTween.TweenMethod(Callable.From<Color>(SetFontColor),
            currentColor, targetColor, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    private void SetFontColor(Color color)
    {
        AddThemeColorOverride("font_color", color);
    }
    private string GetCurrentState()
    {
        if (movementComponent == null)
            return "Unknown";

        // Check for wall running first (highest priority)
        if (wallingComponent != null && wallingComponent.onWall && movementComponent.isSprinting)
        {
            return "Wall Running";
        }

        // Check for wall jumping
        if (wallingComponent != null && wallingComponent.isWallJumping)
        {
            return "Wall Jumping";
        }

        // Check if player is in the air
        if (!movementComponent.isGrounded)
        {
            if (movementComponent.isJumping && movementComponent.velocity.Y > 0)
            {
                return "Jumping";
            }
            else if (movementComponent.velocity.Y < 0)
            {
                return "Falling";
            }
            else
            {
                return "Airborne";
            }
        }

        // Player is grounded - check movement states
        bool isMoving = movementComponent.direction.LengthSquared() > 0.01f;

        if (!isMoving)
        {
            return "Idle";
        }

        if (movementComponent.isSprinting)
        {
            return "Sprinting";
        }

        // Check speed to determine if running or walking
        if (movementComponent.currentSpeed > movementComponent.maxSpeed * 0.7f)
        {
            return "Running";
        }
        else if (movementComponent.currentSpeed > 0.1f)
        {
            return "Walking";
        }

        return "Grounded";
    }

    public void SetMovementComponent(Movement movement)
    {
        movementComponent = movement;
    }

    public void SetWallingComponent(Walling walling)
    {
        wallingComponent = walling;
    }
}
