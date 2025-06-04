using Godot;

public partial class WallManager : Node
{
	[Export] public RayCast3D leftWallRayCast;
	[Export] public RayCast3D rightWallRayCast;
	public Timer wallTimer;

	public bool onWall;
	public bool OnWall { get => onWall; set => onWall = value; }

	public bool isWallJumping = false;

	private bool leftWallCollision = false;
	private bool rightWallCollision = false;

	private bool previousOnWall = false;

	private float wallStateChangeTimer = 0.0f;
	private const float MIN_WALL_STATE_CHANGE_INTERVAL = 0.1f;

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
		wallStateChangeTimer += (float)GetProcessDeltaTime();

		if (!Components.Instance.Movement.isSprinting || Components.Instance.Movement.isGrounded)
		{
			onWall = false;
			leftWallCollision = false;
			rightWallCollision = false;
			wallStateChangeTimer = 0.0f;
			return;
		}

		if (!wallTimer.IsStopped())
		{
			onWall = false;
			leftWallCollision = false;
			rightWallCollision = false;
			return;
		}

		bool rightHit = rightWallRayCast.IsColliding();
		bool leftHit = leftWallRayCast.IsColliding();

		Vector3 playerVelocity = Components.Instance.Movement.velocity;
		Vector3 horizontalVelocity = new Vector3(playerVelocity.X, 0, playerVelocity.Z);

		bool isMovingTowardsWall = false;

		if (rightHit)
		{
			Vector3 wallNormal = rightWallRayCast.GetCollisionNormal();
			float dotProduct = horizontalVelocity.Normalized().Dot(-wallNormal);
			isMovingTowardsWall = dotProduct > -0.5f;
		}
		else if (leftHit)
		{
			Vector3 wallNormal = leftWallRayCast.GetCollisionNormal();
			float dotProduct = horizontalVelocity.Normalized().Dot(-wallNormal);
			isMovingTowardsWall = dotProduct > -0.5f;
		}

		bool newLeftWallCollision = false;
		bool newRightWallCollision = false;

		if (horizontalVelocity.Length() < 1.0f || isMovingTowardsWall)
		{
			newRightWallCollision = rightHit;
			newLeftWallCollision = leftHit;
		}

		bool newOnWall = newLeftWallCollision || newRightWallCollision;

		if (newOnWall != onWall && wallStateChangeTimer >= MIN_WALL_STATE_CHANGE_INTERVAL)
		{
			rightWallCollision = newRightWallCollision;
			leftWallCollision = newLeftWallCollision;
			onWall = newOnWall;
			wallStateChangeTimer = 0.0f; // Reset timer
		}
		else if (newOnWall == onWall)
		{
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
		var Movement = Components.Instance.Movement;

		if (onWall && Input.IsActionJustPressed("jump"))
		{
			wallTimer.Start();
			isWallJumping = true;

			bool wasLeftWall = leftWallCollision;
			bool wasRightWall = rightWallCollision;

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

			Movement.velocity.Y = Movement.jumpForce * Movement.jumpBoostMultiplier;
			Movement.gravity = 13.8f;

			if (jumpDirection != Vector3.Zero)
			{
				jumpDirection = Components.Instance.Player.rb.Transform.Basis * jumpDirection;
				Movement.velocity += jumpDirection * 5.0f;
			}

			WallJumping();
		}
	}

	public void WallJumping()
	{
		if (!isWallJumping) return;
		float currentSpeed = Components.Instance.Movement.currentSpeed;

		if (currentSpeed <= 35)
			Components.Instance.Movement.currentSpeed += currentSpeed / 5;
		else if (currentSpeed > 35)
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
