using Godot;

public partial class Walling : Node
{
	[Export] public RayCast3D leftWallRayCast;
	[Export] public RayCast3D rightWallRayCast;
	public Timer wallTimer;

	public bool onWall;
	public bool OnWall { get => onWall; set => onWall = value; }

	public bool isWallJumping = false;

	// Variables to track which side of the wall the player is on
	private bool leftWallCollision = false;
	private bool rightWallCollision = false;

	public override void _Ready()
	{
		AddWallTimer();
	}

	public void AddWallTimer()
	{
		wallTimer = new Timer();
		wallTimer.OneShot = true;
		wallTimer.WaitTime = 0.5f;
		AddChild(wallTimer);
	}

	public void CheckWall()
	{
		// var collision = leftWallCollision  && wallTimer.IsStopped() || rightWallCollision && wallTimer.IsStopped();
		
		if (!Components.Instance.Movement.isSprinting || Components.Instance.Movement.isGrounded) return;

		rightWallCollision = rightWallRayCast.IsColliding() && wallTimer.IsStopped();
		leftWallCollision = leftWallRayCast.IsColliding() && wallTimer.IsStopped();

		if (leftWallCollision || rightWallCollision)
		{
			onWall = true;
		}
		else
		{
			onWall = false;
			leftWallCollision = false;
			rightWallCollision = false;
		}

		if (leftWallCollision)
		{
			GD.Print("Left wall collision detected");
		}
		else if (rightWallCollision)
		{
			GD.Print("Right wall collision detected");
		}
	}

	public void HandleWalling()
	{
		bool isSprinting = Components.Instance.Movement.isSprinting;
		float gravity = Components.Instance.Movement.gravity;

		if (onWall && isSprinting)
		{
			Components.Instance.Movement.velocity.Y = 0;
			gravity = 0;
		}
		else
			gravity = 13.8f;
	}

	public void HandleWallJump()
	{
		var collision = leftWallCollision || rightWallCollision;

		if (onWall && Input.IsActionJustPressed("jump"))
		{
			wallTimer.Start();
			isWallJumping = true;
			onWall = false;
			WallJumping();
			GD.Print("Wall Jump");

			if (collision)
			{
				Components.Instance.Movement.velocity.Y = Components.Instance.Movement.jumpForce / 2;
			}
		}
	}

	public void WallJumping()
	{
		if (!isWallJumping) return;
		float currentSpeed = Components.Instance.Movement.currentSpeed;

		if (currentSpeed <= 70)
			Components.Instance.Movement.currentSpeed += Components.Instance.Movement.currentSpeed / 2;
		else if (currentSpeed > 70)
			Components.Instance.Movement.currentSpeed += Components.Instance.Movement.currentSpeed / 10;
	}
}
