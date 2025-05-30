using Godot;
using System;

public partial class SpeedDisplay : Label
{
    [Export] private NodePath movementNodePath;
    [Export] private bool roundValue = true;
    [Export] private int decimalPlaces = 1;
    [Export] private string prefix = "Speed: ";
    [Export] private string suffix = " u/s";
    [Export] private float updateInterval = 0.05f; // Update every 50ms

    private Movement movementComponent;
    private float updateTimer = 0f;

    public override void _Ready()
    {
        if (movementNodePath != null && !movementNodePath.IsEmpty)
        {
            Node targetNode = GetNode(movementNodePath);
            if (targetNode != null && targetNode is Movement)
            {
                movementComponent = targetNode as Movement;
            }
        }
        
        if (movementComponent == null && Components.Instance != null && Components.Instance.Player != null)
        {
            movementComponent = Components.Instance.Player.GetNodeOrNull<Movement>("Movement");
        }
    }

    public override void _Process(double delta)
    {
        updateTimer += (float)delta;
        
        if (updateTimer >= updateInterval && movementComponent != null)
        {
            updateTimer = 0;
            UpdateSpeedDisplay();
        }
    }

    private void UpdateSpeedDisplay()
    {
        float speedValue = movementComponent.currentSpeed;
        
        if (roundValue)
        {
            string format = $"F{decimalPlaces}";
            Text = $"{prefix}{speedValue.ToString(format)}{suffix}";
        }
        else
        {
            Text = $"{prefix}{speedValue}{suffix}";
        }
    }
    
    // Method to manually set the Movement component if needed
    public void SetMovementComponent(Movement movement)
    {
        movementComponent = movement;
    }
}
