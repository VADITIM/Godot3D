using Godot;

public partial class WallManager : Node
{
	public bool isWalling = false;
	public bool IsWalling { get => isWalling; set => isWalling = value; }

	[Export] public RayCast3D[] wallRayCast = new RayCast3D[2];
	public Timer wallTimer;

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
	
	
	public void HandleWalling()
	{
		if (isWalling)
		{
			PlayerComponents.Instance.Player.velocity.Y = 0;
			PlayerComponents.Instance.Player.gravity = 0;
		}
		else 
			PlayerComponents.Instance.Player.gravity = 9.8f;
	}

	public void HandleWallJump()
	{
		{
			if (isWalling && Input.IsActionJustPressed("jump"))
			{
				isWalling = false;

				wallTimer.Start();
				
				GD.Print("Wall Jump");
				PlayerComponents.Instance.Player.velocity.Y = PlayerComponents.Instance.Player.jumpForce;
			}
		}
	}

}
