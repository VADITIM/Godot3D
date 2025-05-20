using Godot;

public partial class WallManager : Node
{
	[Export] public RayCast3D[] wallRayCast = new RayCast3D[2];
	public Timer wallTimer;

	public bool isWalling = false;
	public bool IsWalling { get => isWalling; set => isWalling = value; }

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
		if ((wallRayCast[0].IsColliding() && wallTimer.IsStopped()) ||
			(wallRayCast[1].IsColliding() && wallTimer.IsStopped()))
		{
			isWalling = true;
		}
		else
		{
			isWalling = false;
		}
	}
	
	public void HandleWalling(float currentSpeed)
	{
		if (isWalling && PlayerComponents.Instance.Movement.currentSpeed > 10)
		{
			PlayerComponents.Instance.Movement.velocity.Y = 0;
			PlayerComponents.Instance.Movement.gravity = 0;
		}
		else 
			PlayerComponents.Instance.Movement.gravity = 9.8f;
	}

	public void HandleWallJump()
	{
		{
			if (isWalling && Input.IsActionJustPressed("jump"))
			{
				isWalling = false;

				wallTimer.Start();
				
				GD.Print("Wall Jump");
				PlayerComponents.Instance.Movement.velocity.Y = PlayerComponents.Instance.Movement.jumpForce;
			}
		}
	}

}
