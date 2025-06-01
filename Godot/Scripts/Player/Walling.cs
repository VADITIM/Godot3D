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

	// Debug tracking
	private bool previousOnWall = false;

	// State dampening
	private float wallStateChangeTimer = 0.0f;
	private const float MIN_WALL_STATE_CHANGE_INTERVAL = 0.1f; // Minimum time between wall state changes

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
		// Update state change timer
		wallStateChangeTimer += (float)GetProcessDeltaTime();

		// Only check for walls if sprinting and not grounded, and timer allows detection
		if (!Components.Instance.Movement.isSprinting || Components.Instance.Movement.isGrounded)
		{
			// Clear wall state completely when conditions aren't met
			if (onWall)
			{
				GD.Print("Clearing wall state: not sprinting or on ground");
			}
			onWall = false;
			leftWallCollision = false;
			rightWallCollision = false;
			wallStateChangeTimer = 0.0f; // Reset timer when clearing state
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

		// Get movement direction to check if player is moving towards the wall
		Vector3 playerVelocity = Components.Instance.Movement.velocity;
		Vector3 horizontalVelocity = new Vector3(playerVelocity.X, 0, playerVelocity.Z);

		// Only consider wall collision if moving towards the wall or moving slowly
		bool isMovingTowardsWall = false;

		if (rightHit)
		{
			Vector3 wallNormal = rightWallRayCast.GetCollisionNormal();
			float dotProduct = horizontalVelocity.Normalized().Dot(-wallNormal);
			isMovingTowardsWall = dotProduct > -0.5f; // Allow some tolerance for parallel movement
		}
		else if (leftHit)
		{
			Vector3 wallNormal = leftWallRayCast.GetCollisionNormal();
			float dotProduct = horizontalVelocity.Normalized().Dot(-wallNormal);
			isMovingTowardsWall = dotProduct > -0.5f; // Allow some tolerance for parallel movement
		}

		// Calculate what the new wall state should be
		bool newLeftWallCollision = false;
		bool newRightWallCollision = false;

		// Update collision states only if moving towards wall or moving slowly
		if (horizontalVelocity.Length() < 1.0f || isMovingTowardsWall)
		{
			newRightWallCollision = rightHit;
			newLeftWallCollision = leftHit;
		}

		bool newOnWall = newLeftWallCollision || newRightWallCollision;

		// Only change state if enough time has passed since last change (dampening)
		if (newOnWall != onWall && wallStateChangeTimer >= MIN_WALL_STATE_CHANGE_INTERVAL)
		{
			rightWallCollision = newRightWallCollision;
			leftWallCollision = newLeftWallCollision;
			onWall = newOnWall;
			wallStateChangeTimer = 0.0f; // Reset timer
		}
		else if (newOnWall == onWall)
		{
			// If state would remain the same, update immediately
			rightWallCollision = newRightWallCollision;
			leftWallCollision = newLeftWallCollision;
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
		// Debug: Reset wall state with a key press (useful for testing)
		if (Input.IsActionJustPressed("ui_cancel")) // ESC key by default
		{
			ForceResetWallState();
			return;
		}

		if (onWall && Input.IsActionJustPressed("jump"))
		{
			wallTimer.Start();
			isWallJumping = true;

			// Store wall info before clearing
			bool wasLeftWall = leftWallCollision;
			bool wasRightWall = rightWallCollision;

			// Clear wall state immediately and prevent re-detection
			onWall = false;
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
			Components.Instance.Movement.velocity.Y = Components.Instance.Movement.jumpForce * 2;
			Components.Instance.Movement.gravity = 13.8f;

			// Apply horizontal push based on wall side
			if (jumpDirection != Vector3.Zero)
			{
				// Transform direction to align with camera
				jumpDirection = Components.Instance.Player.rb.Transform.Basis * jumpDirection;
				Components.Instance.Movement.velocity += jumpDirection * 5.0f;
			}

			WallJumping();
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


	public void ForceResetWallState()
	{
		onWall = false;
		leftWallCollision = false;
		rightWallCollision = false;
		isWallJumping = false;
		wallStateChangeTimer = 0.0f;
	}
}
