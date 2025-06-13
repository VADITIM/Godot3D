using Godot;
using System;

public partial class Shooting : Node
{
    [Export] Player Player;
    [Export] private Camera3D playerCamera;
    [Export] private Node3D firingPoint;

    [Export] private PackedScene bulletScene;

    [Export] private float bulletSpeed = 200f;
    [Export] private float fireRate = 0.1f;
    [Export] private float bulletSpread = 0f;

    [Export] private float maxRange = 1000f;
    [Export] private float bulletLifetime = 5f;
    [Export] private uint raycastCollisionLayers = 1;

    private float fireTimer = 0f;

    public override void _Ready()
    {
        if (firingPoint == null)
        {
            firingPoint = Player.GetNode<Node3D>("Firing Point");
        }
    }

    public override void _Process(double delta)
    {
        fireTimer -= (float)delta;

        if (Input.IsActionJustPressed("shoot") && fireTimer <= 0f)
        {
            Shoot();
            fireTimer = fireRate;
        }
    }

    private void Shoot()
    {
        GD.Print("Shooting bullet...");

        RigidBody3D bulletInstance = bulletScene.Instantiate() as RigidBody3D;
        GetTree().Root.AddChild(bulletInstance);

        Vector3 firingPosition = firingPoint.GlobalPosition;

        Vector3 targetPoint = GetCrosshairTargetPoint();

        Vector3 shootDirection = (targetPoint - firingPosition).Normalized();

        if (bulletSpread > 0f)
        {
            float spreadX = (float)GD.RandRange(-bulletSpread, bulletSpread);
            float spreadY = (float)GD.RandRange(-bulletSpread, bulletSpread);

            Vector3 right = playerCamera.GlobalTransform.Basis.X;
            Vector3 up = playerCamera.GlobalTransform.Basis.Y;

            Vector3 spreadOffset = right * spreadX + up * spreadY;
            shootDirection = (shootDirection + spreadOffset).Normalized();
        }

        bulletInstance.GlobalPosition = firingPosition;
        bulletInstance.LookAt(firingPosition + shootDirection, Vector3.Up);
        bulletInstance.LinearVelocity = shootDirection * bulletSpeed;
    }

    private Vector3 GetCrosshairTargetPoint()
    {
        Vector3 cameraOrigin = playerCamera.GlobalPosition;
        Vector3 cameraForward = -playerCamera.GlobalTransform.Basis.Z;

        var spaceState = playerCamera.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(
            cameraOrigin,
            cameraOrigin + cameraForward * maxRange
        );

        query.CollisionMask = raycastCollisionLayers;

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
            return (Vector3)result["position"];
        else
            return cameraOrigin + cameraForward * maxRange;
    }
}
