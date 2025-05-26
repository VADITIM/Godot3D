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
		// Only check for walls if sprinting and not grounded, and timer allows detection
		if (!Components.Instance.Movement.isSprinting || Components.Instance.Movement.isGrounded)
		{
			onWall = false;
			leftWallCollision = false;
			rightWallCollision = false;
			return;
		}

		// Only detect walls if the timer has finished (preventing immediate re-detection after wall jump)
		if (!wallTimer.IsStopped())
		{
			onWall = false;
			leftWallCollision = false;
			rightWallCollision = false;
			return;
		}

		// Check actual raycast collisions
		bool rightHit = rightWallRayCast.IsColliding();
		bool leftHit = leftWallRayCast.IsColliding();

		rightWallCollision = rightHit;
		leftWallCollision = leftHit;

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
	}

	public void HandleWalling()
	{
		bool isSprinting = Components.Instance.Movement.isSprinting;

		float originalGravity = 13.8f;

		if (onWall && isSprinting)
		{
			Components.Instance.Movement.velocity.Y = 0;
			Components.Instance.Movement.gravity = 0;
		}
		else
		{
			Components.Instance.Movement.gravity = originalGravity;
		}
	}

	public void HandleWallJump()
	{
		if (onWall && Input.IsActionJustPressed("jump"))
		{
			wallTimer.Start();
			isWallJumping = true;
			onWall = false;

			bool wasLeftWall = leftWallCollision;
			bool wasRightWall = rightWallCollision;
			leftWallCollision = false;
			rightWallCollision = false;

			Vector3 jumpDirection = Vector3.Zero;
			if (wasLeftWall)
			{
				jumpDirection = new Vector3(100, 0, 0); 
			}
			else if (wasRightWall)
			{
				jumpDirection = new Vector3(-1, 0, 0);
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
