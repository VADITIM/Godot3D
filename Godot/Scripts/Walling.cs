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
		
		// Store original gravity for restoration when not on wall
		float originalGravity = 13.8f;
		
		if (onWall && isSprinting)
		{
			// Zero out vertical velocity and gravity when on wall
			Components.Instance.Movement.velocity.Y = 0;
			Components.Instance.Movement.gravity = 0;
		}
		else
		{
			// Restore normal gravity when not on wall
			Components.Instance.Movement.gravity = originalGravity;
		}
	}

	public void HandleWallJump()
	{
		bool collision = leftWallCollision || rightWallCollision;

		if (onWall && Input.IsActionJustPressed("jump"))
		{
			wallTimer.Start();
			isWallJumping = true;
			onWall = false;
			
			// Calculate wall jump direction based on which wall we're on
			Vector3 jumpDirection = Vector3.Zero;
			if (leftWallCollision)
			{
				jumpDirection = new Vector3(1, 0, 0); // Push right when jumping from left wall
			}
			else if (rightWallCollision)
			{
				jumpDirection = new Vector3(-1, 0, 0); // Push left when jumping from right wall
			}
			
			// Apply wall jump force and restore gravity
			Components.Instance.Movement.velocity.Y = Components.Instance.Movement.jumpForce;
			Components.Instance.Movement.gravity = 13.8f;
			
			// Apply horizontal push based on wall side
			if (jumpDirection != Vector3.Zero)
			{
				// Transform direction to align with camera
				jumpDirection = Components.Instance.Player.rb.Transform.Basis * jumpDirection;
				Components.Instance.Movement.velocity += jumpDirection * 5.0f;
			}
			
			WallJumping();
			GD.Print("Wall Jump");
		}
	}

	public void WallJumping()
	{
		if (!isWallJumping) return;
		float currentSpeed = Components.Instance.Movement.currentSpeed;

		if (currentSpeed <= 70)
			Components.Instance.Movement.currentSpeed += currentSpeed / 2;
		else if (currentSpeed > 70)
			Components.Instance.Movement.currentSpeed += currentSpeed / 10;
	}
}
