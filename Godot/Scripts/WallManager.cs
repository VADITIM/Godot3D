using Godot;

public partial class WallManager : Node
{
	[Export] public RayCast3D[] wallRayCast = new RayCast3D[2];
	public Timer wallTimer;

	public bool isWalling;
	public bool IsWalling { get => isWalling; set => isWalling = value; }

	public bool isWallJumping = false;

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
		if (!PlayerComponents.Instance.Movement.isSprinting) return;

		if ((wallRayCast[0].IsColliding() && wallTimer.IsStopped()) ||
				(wallRayCast[1].IsColliding() && wallTimer.IsStopped()))
		{
			isWalling = true;
			PlayerComponents.Instance.Movement.baseSpeed = 20;
		}
		else
		{
			isWalling = false;
		}
	}



	public void HandleWalling(float currentSpeed)
	{
		WallJumping();

		currentSpeed = PlayerComponents.Instance.Movement.currentSpeed;
		var gravity = PlayerComponents.Instance.Movement.gravity;

		if (isWalling && currentSpeed > 10)
		{
			PlayerComponents.Instance.Movement.velocity.Y = 0;
			gravity = 0;
		}
		else
			gravity = 9.8f;
	}

	public void HandleWallJump()
	{
		if (isWalling && Input.IsActionJustPressed("jump"))
		{
			wallTimer.Start();
			isWallJumping = true;
			isWalling = false;

			GD.Print("Wall Jump");
			PlayerComponents.Instance.Movement.velocity.Y = PlayerComponents.Instance.Movement.jumpForce;
		}
	}

	public void WallJumping()
	{
		if (isWallJumping)
		{
			PlayerComponents.Instance.Movement.baseSpeed = 90;
		}
	}

}
