using Godot;

public partial class WallSnapDetection : Node3D
{
	[Export] public float wallDetectionDistance = 1.5f;
	[Export] public float wallSnapDistance = 0.8f;
	[Export] public float rotationSpeed = 12.0f;
	[Export] public float positionSnapSpeed = 8.0f;
	[Export] public float wallAngleThreshold = 25.0f; // Maximum wall angle deviation from vertical
	[Export] public float velocityThreshold = 1.0f; // Minimum velocity for wall detection
	[Export] public float raycastSpread = 0.3f; // Horizontal spacing between raycasts
	[Export] public float raycastVerticalSpread = 0.5f; // Vertical spacing for multiple raycasts
	[Export] public int raycastCount = 3; // Number of raycasts per side (vertically distributed)

	// Debug and performance properties
	[Export] public bool enableDebugOutput = false;
	[Export] public bool enableVisualization = false;

	// Detection states
	public bool isOnWall { get; private set; } = false;
	public bool isOnLeftWall { get; private set; } = false;
	public bool isOnRightWall { get; private set; } = false;
	public Vector3 wallNormal { get; private set; } = Vector3.Zero;
	public Vector3 wallPoint { get; private set; } = Vector3.Zero;
	public Vector3 wallDirection { get; private set; } = Vector3.Zero;

	[Export]
	private RayCast3D leftRaycast;
	[Export]
	private RayCast3D rightRaycast;

	private bool isSnapping = false;
	private float targetYRotation = 0.0f;
	private float detectionCooldown = 0.0f;
	private const float DETECTION_UPDATE_INTERVAL = 0.02f; // 50 FPS detection updates

	private int frameCounter = 0;
	private const int RAYCAST_UPDATE_FREQUENCY = 2;

	// Wall detection result cache
	private WallSnapResult currentWallResult = new WallSnapResult();

	[Export] public float minWallRunSpeed = 3.0f; // Minimum speed required to maintain wall running
	[Export] public float maxWallRunTime = 55.0f; // Maximum time player can stay on wall continuously

	private float wallRunTimer = 0.0f;
	private Vector3 lastValidWallNormal = Vector3.Zero;
	private float wallStabilityTimer = 0.0f;
	private const float WALL_STABILITY_THRESHOLD = 0.1f; // Time to confirm wall before snapping

	public override void _Ready()
	{
		// Only set TargetPosition if not set in inspector (length near zero)
		if (leftRaycast != null && leftRaycast.TargetPosition.Length() < 0.1f)
			ConfigureRaycast(leftRaycast, new Vector3(-wallDetectionDistance, 0, 0));
		if (rightRaycast != null && rightRaycast.TargetPosition.Length() < 0.1f)
			ConfigureRaycast(rightRaycast, new Vector3(wallDetectionDistance, 0, 0));

		Position = Vector3.Zero; // Ensure this node is at player center
	}

	private void ConfigureRaycast(RayCast3D raycast, Vector3 targetPosition)
	{
		raycast.Enabled = true;
		raycast.TargetPosition = targetPosition;
		raycast.CollisionMask = 1; // Wall layer
		raycast.HitFromInside = false;
		raycast.HitBackFaces = false;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Always keep raycasts enabled and updated
		if (leftRaycast != null)
			leftRaycast.Enabled = true;
		if (rightRaycast != null)
			rightRaycast.Enabled = true;
	}

	public void UpdateWallDetection(float delta)
	{
		detectionCooldown -= delta;
		frameCounter++;

		// Check if raycasts are properly assigned
		if (leftRaycast == null || rightRaycast == null)
		{
			if (enableDebugOutput)
				GD.PrintErr("Raycasts not assigned in inspector!");
			return;
		}

		// Throttle detection updates for performance
		if (detectionCooldown > 0) return;
		detectionCooldown = DETECTION_UPDATE_INTERVAL;

		// Stagger raycast updates for better performance
		if (frameCounter % RAYCAST_UPDATE_FREQUENCY != 0) return;

		// Check if player is eligible for wall detection
		if (!IsEligibleForWallDetection())
		{
			ClearWallState();
			return;
		}

		// Force raycast update right before checking collision
		leftRaycast.ForceRaycastUpdate();
		rightRaycast.ForceRaycastUpdate();

		// Debug raycast states
		if (enableDebugOutput)
		{
			GD.Print($"Left raycast pos: {leftRaycast.GlobalTransform.Origin}, target: {leftRaycast.TargetPosition}, enabled: {leftRaycast.Enabled}, colliding: {leftRaycast.IsColliding()}");
			GD.Print($"Right raycast pos: {rightRaycast.GlobalTransform.Origin}, target: {rightRaycast.TargetPosition}, enabled: {rightRaycast.Enabled}, colliding: {rightRaycast.IsColliding()}");
		}

		// Perform comprehensive wall detection
		WallSnapResult leftResult = DetectWallSide(leftRaycast, true);
		WallSnapResult rightResult = DetectWallSide(rightRaycast, false);

		// Select the best wall result
		WallSnapResult bestResult = SelectBestWallResult(leftResult, rightResult);

		// Debug output
		if (enableDebugOutput && bestResult.isValid)
		{
			GD.Print($"Wall detected: {(bestResult.isLeftSide ? "Left" : "Right")} wall, Quality: {bestResult.quality:F2}, Distance: {bestResult.distance:F2}");
		}

		// Update wall state and apply snapping
		UpdateWallState(bestResult, delta);
	}

	private bool IsEligibleForWallDetection()
	{
		// Must be sprinting and not grounded
		if (!Components.Instance.Movement.isSprinting || Components.Instance.Movement.isGrounded)
		{
			if (enableDebugOutput)
				GD.Print("Not eligible: not sprinting or grounded");
			return false;
		}

		// Must have sufficient horizontal velocity
		Vector3 horizontalVelocity = new Vector3(
			Components.Instance.Movement.velocity.X,
			0,
			Components.Instance.Movement.velocity.Z
		);

		bool hasVelocity = horizontalVelocity.Length() > velocityThreshold;
		if (enableDebugOutput && !hasVelocity)
			GD.Print($"Not eligible: velocity too low ({horizontalVelocity.Length()})");

		return hasVelocity;
	}

	private WallSnapResult DetectWallSide(RayCast3D raycast, bool isLeftSide)
	{
		WallSnapResult result = new WallSnapResult();

		if (raycast == null)
		{
			result.isValid = false;
			return result;
		}

		// Force raycast update right before checking collision
		raycast.ForceRaycastUpdate();

		if (!raycast.IsColliding())
		{
			if (enableDebugOutput)
				GD.Print($"{(isLeftSide ? "Left" : "Right")} raycast not colliding");
			result.isValid = false;
			return result;
		}

		Vector3 hitPoint = raycast.GetCollisionPoint();
		Vector3 hitNormal = raycast.GetCollisionNormal();

		if (enableDebugOutput)
			GD.Print($"{(isLeftSide ? "Left" : "Right")} raycast hit at {hitPoint}, normal: {hitNormal}");

		// Validate wall surface
		if (!IsValidWallSurface(hitNormal, hitPoint))
		{
			result.isValid = false;
			return result;
		}

		// Calculate quality of this hit
		float quality = CalculateWallQuality(hitNormal, hitPoint);

		if (quality <= 0.3f) // Below minimum quality threshold
		{
			result.isValid = false;
			return result;
		}

		// Final validation: check player movement direction
		if (!IsMovingTowardWall(hitNormal))
		{
			result.isValid = false;
			return result;
		}

		result.isValid = true;
		result.isLeftSide = isLeftSide;
		result.wallNormal = hitNormal;
		result.wallPoint = hitPoint;
		result.quality = quality;
		result.distance = (Components.Instance.Player.GlobalPosition - hitPoint).Length();

		return result;
	}

	private bool IsValidWallSurface(Vector3 normal, Vector3 point)
	{
		// Check wall angle (should be close to vertical)
		float wallAngle = Mathf.RadToDeg(Mathf.Acos(Mathf.Clamp(Mathf.Abs(normal.Dot(Vector3.Up)), 0.0f, 1.0f)));
		if (wallAngle > wallAngleThreshold)
			return false;

		// Check distance to wall
		float distance = (Components.Instance.Player.GlobalPosition - point).Length();
		if (distance > wallDetectionDistance)
			return false;

		return true;
	}

	private bool IsMovingTowardWall(Vector3 wallNormal)
	{
		Vector3 horizontalVelocity = new Vector3(
			Components.Instance.Movement.velocity.X,
			0,
			Components.Instance.Movement.velocity.Z
		);

		if (horizontalVelocity.Length() < 0.5f) return true; // Allow at low speeds

		// Check if moving toward wall (negative dot product means moving toward)
		float velocityDotNormal = horizontalVelocity.Normalized().Dot(wallNormal);
		return velocityDotNormal < 0.2f; // Not moving strongly away from wall
	}

	private float CalculateWallQuality(Vector3 normal, Vector3 point)
	{
		float qualityScore = 0.0f;

		// Prefer more vertical walls
		float verticalScore = 1.0f - Mathf.Abs(normal.Dot(Vector3.Up));
		qualityScore += verticalScore * 0.4f;

		// Prefer closer walls
		float distance = (Components.Instance.Player.GlobalPosition - point).Length();
		float distanceScore = 1.0f - Mathf.Clamp(distance / wallDetectionDistance, 0.0f, 1.0f);
		qualityScore += distanceScore * 0.3f;

		// Prefer walls aligned with player movement
		Vector3 horizontalVelocity = new Vector3(
			Components.Instance.Movement.velocity.X,
			0,
			Components.Instance.Movement.velocity.Z
		);

		if (horizontalVelocity.Length() > 0.1f)
		{
			Vector3 wallParallel = new Vector3(-normal.Z, 0, normal.X).Normalized();
			float alignmentScore = Mathf.Abs(horizontalVelocity.Normalized().Dot(wallParallel));
			qualityScore += alignmentScore * 0.3f;
		}

		return Mathf.Clamp(qualityScore, 0.0f, 1.0f);
	}

	private WallSnapResult SelectBestWallResult(WallSnapResult left, WallSnapResult right)
	{
		if (!left.isValid && !right.isValid)
			return new WallSnapResult { isValid = false };

		if (left.isValid && !right.isValid)
			return left;

		if (!left.isValid && right.isValid)
			return right;

		// Both valid - choose based on quality and prefer closer walls
		float leftScore = left.quality - (left.distance * 0.1f);
		float rightScore = right.quality - (right.distance * 0.1f);

		return leftScore > rightScore ? left : right;
	}

	private void UpdateWallState(WallSnapResult result, float delta)
	{
		if (!result.isValid)
		{
			ClearWallState();
			return;
		}

		// Update wall state
		isOnWall = true;
		isOnLeftWall = result.isLeftSide;
		isOnRightWall = !result.isLeftSide;
		wallNormal = result.wallNormal;
		wallPoint = result.wallPoint;
		wallDirection = new Vector3(-wallNormal.Z, 0, wallNormal.X).Normalized();

		currentWallResult = result;

		// Apply wall snapping
		ApplyWallSnapping(result, delta);

		// Lock lateral movement
		Components.Instance.Movement.isLateralMovementLocked = true;
	}

	private void ApplyWallSnapping(WallSnapResult result, float delta)
	{
		// Calculate and apply rotation snapping
		float targetRotation = CalculateTargetRotation(result.isLeftSide);
		ApplyRotationSnapping(targetRotation, delta);

		// Apply position snapping
		ApplyPositionSnapping(result, delta);
	}

	private float CalculateTargetRotation(bool isLeftWall)
	{
		// Get player's current velocity direction for more responsive rotation
		Vector3 horizontalVelocity = new Vector3(
			Components.Instance.Movement.velocity.X,
			0,
			Components.Instance.Movement.velocity.Z
		);

		// Calculate the direction parallel to the wall
		Vector3 wallParallel = new Vector3(-wallNormal.Z, 0, wallNormal.X).Normalized();

		// If player has velocity, choose wall direction that aligns with movement
		if (horizontalVelocity.Length() > 0.5f)
		{
			Vector3 velocityDirection = horizontalVelocity.Normalized();
			float dot1 = wallParallel.Dot(velocityDirection);
			float dot2 = (-wallParallel).Dot(velocityDirection);

			// Choose the wall direction that better aligns with velocity
			if (dot1 > dot2)
			{
				return Mathf.Atan2(wallParallel.X, wallParallel.Z);
			}
			else
			{
				return Mathf.Atan2(-wallParallel.X, -wallParallel.Z);
			}
		}

		// Fallback: use wall side to determine rotation
		float baseWallRotation = Mathf.Atan2(wallNormal.X, wallNormal.Z);
		float rotationOffset = isLeftWall ? -Mathf.Pi / 2 : Mathf.Pi / 2;

		return baseWallRotation + rotationOffset;
	}

	private void ApplyRotationSnapping(float targetRotation, float delta)
	{
		// Normalize angles
		targetRotation = NormalizeAngle(targetRotation);
		float currentRotation = Components.Instance.Player.rb.Rotation.Y;
		currentRotation = NormalizeAngle(currentRotation);

		// Calculate shortest rotation path
		float rotationDiff = targetRotation - currentRotation;
		rotationDiff = NormalizeAngle(rotationDiff);

		// Apply smooth rotation
		float rotationStep = rotationSpeed * delta;

		if (Mathf.Abs(rotationDiff) < rotationStep * 2.0f)
		{
			// Close enough - snap to target
			SetPlayerYRotation(targetRotation);
			isSnapping = false;
		}
		else
		{
			// Smooth interpolation toward target
			float direction = Mathf.Sign(rotationDiff);
			float newRotation = currentRotation + direction * rotationStep;
			SetPlayerYRotation(newRotation);
			isSnapping = true;
		}
	}

	private void ApplyPositionSnapping(WallSnapResult result, float delta)
	{
		// Calculate desired position relative to wall
		Vector3 offsetFromWall = wallNormal * wallSnapDistance;
		Vector3 targetPosition = result.wallPoint + offsetFromWall;

		// Only adjust horizontal position (X and Z)
		Vector3 currentPosition = Components.Instance.Player.GlobalPosition;
		targetPosition.Y = currentPosition.Y; // Preserve Y position

		// Calculate position difference
		Vector3 positionDiff = targetPosition - currentPosition;
		positionDiff.Y = 0; // Ensure no vertical adjustment

		// Apply smooth position adjustment if difference is significant
		if (positionDiff.Length() > 0.08f)
		{
			Vector3 adjustment = positionDiff * positionSnapSpeed * delta;

			// Limit adjustment speed to prevent overshooting
			if (adjustment.Length() > positionDiff.Length())
				adjustment = positionDiff;

			// Apply position adjustment
			Vector3 newPosition = currentPosition + adjustment;
			Components.Instance.Player.GlobalPosition = newPosition;
		}
	}

	private float NormalizeAngle(float angle)
	{
		while (angle > Mathf.Pi) angle -= 2 * Mathf.Pi;
		while (angle < -Mathf.Pi) angle += 2 * Mathf.Pi;
		return angle;
	}

	private void SetPlayerYRotation(float yRotation)
	{
		Vector3 currentRotation = Components.Instance.Player.rb.Rotation;
		Components.Instance.Player.rb.Rotation = new Vector3(currentRotation.X, yRotation, currentRotation.Z);
	}

	public void ClearWallState()
	{
		isOnWall = false;
		isOnLeftWall = false;
		isOnRightWall = false;
		wallNormal = Vector3.Zero;
		wallPoint = Vector3.Zero;
		wallDirection = Vector3.Zero;
		isSnapping = false;
		currentWallResult = new WallSnapResult { isValid = false };

		// Release movement lock
		Components.Instance.Movement.isLateralMovementLocked = false;
	}

	public void ForceReset()
	{
		ClearWallState();
		detectionCooldown = 0.0f;
	}

	public Vector3 GetWallRunDirection()
	{
		if (!isOnWall) return Vector3.Zero;

		// Get player's forward direction
		Vector3 playerForward = -Components.Instance.Player.rb.Transform.Basis.Z;
		playerForward.Y = 0;
		playerForward = playerForward.Normalized();

		// Choose wall direction that best aligns with player's movement
		float dot1 = wallDirection.Dot(playerForward);
		float dot2 = (-wallDirection).Dot(playerForward);

		return dot1 > dot2 ? wallDirection : -wallDirection;
	}

	public bool IsMovingTowardCurrentWall()
	{
		if (!isOnWall) return false;

		Vector3 horizontalVelocity = new Vector3(
			Components.Instance.Movement.velocity.X,
			0,
			Components.Instance.Movement.velocity.Z
		);
		if (horizontalVelocity.Length() < 0.1f) return true;

		float dotProduct = horizontalVelocity.Normalized().Dot(-wallNormal);
		return dotProduct > -0.5f; // Not moving strongly away
	}

	public override void _ExitTree()
	{
		ClearWallState();
	}
}

// Helper struct for wall detection results
public struct WallSnapResult
{
	public bool isValid;
	public bool isLeftSide;
	public Vector3 wallNormal;
	public Vector3 wallPoint;
	public float distance;
	public float quality;
}
