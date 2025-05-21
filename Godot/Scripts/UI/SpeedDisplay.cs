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
        // If movementNodePath is set, try to get the movement component directly
        if (movementNodePath != null && !movementNodePath.IsEmpty)
        {
            Node targetNode = GetNode(movementNodePath);
            if (targetNode != null && targetNode is Movement)
            {
                movementComponent = targetNode as Movement;
            }
        }
        
        // If not set or not found, try to get it from the Components singleton
        if (movementComponent == null && Components.Instance != null && Components.Instance.Player != null)
        {
            // Try to find Movement component on the player
            movementComponent = Components.Instance.Player.GetNodeOrNull<Movement>("Movement");
        }
        
        if (movementComponent == null)
        {
            Text = "Movement component not found!";
            GD.PrintErr("SpeedDisplay: Movement component not found!");
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
            // Format to specified decimal places
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
