using Godot;
using System;
using System.Collections.Generic;
public partial class GameUI : Control
{
    public static GameUI Instance { get; private set; }

    [Export] private float glowIntensity = 0.3f;
    [Export] private float maxFallDistance = 100.0f;


    private RayCast3D groundDetectionRay;

    public override void _Ready()
    {
        Instance = this;

        SetupGroundDetection();
    }


    public void SetupGroundDetection()
    {
        groundDetectionRay = new RayCast3D();
        groundDetectionRay.Enabled = true;
        groundDetectionRay.TargetPosition = new Vector3(0, -maxFallDistance, 0);
        groundDetectionRay.CollisionMask = 1;
        // Components.Instance.Player.AddChild(groundDetectionRay);
    }

    public float GetDistanceToGround()
    {
        groundDetectionRay.ForceRaycastUpdate();
        if (!groundDetectionRay.IsColliding())
            return maxFallDistance;

        Vector3 hitPoint = groundDetectionRay.GetCollisionPoint();
        Vector3 rayOrigin = groundDetectionRay.GlobalTransform.Origin;
        return rayOrigin.DistanceTo(hitPoint);
    }

    public float GetNormalizedDistanceToGround()
    {
        float distance = GetDistanceToGround();
        return Mathf.Clamp(distance / maxFallDistance, 0.0f, 1.0f);
    }

}
