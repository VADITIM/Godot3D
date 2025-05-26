using Godot;
using System;
using System.Collections.Generic;

public partial class GameUI : Control
{
    public static GameUI Instance { get; private set; }

    [Export] private Control[] animatableObjects; // Array of UI objects to animate
    [Export] private float animationSpeed = 0.15f;
    [Export] private float bounceIntensity = 1.2f;
    [Export] private float glowIntensity = 0.3f;
    [Export] private float maxFallDistance = 100.0f; // Maximum distance for fall effect scaling
    [Export] private float transitionSpeed = 0.3f; // Speed for smooth state transitions

    private Vector2[] originalPositions;
    private Vector2[] originalScales;
    private Color[] originalColors;
    private Vector2[] currentPositions; // Track current animated positions
    private Vector2[] targetPositions; // Where we want each object to be
    private Dictionary<string, Tween> activeTweens = new Dictionary<string, Tween>();
    private string currentState = "";
    private string previousState = "";
    private RayCast3D groundDetectionRay;

    public override void _Ready()
    {
        Instance = this;

        if (animatableObjects != null && animatableObjects.Length > 0)
        {
            // Initialize arrays for each object's properties
            int count = animatableObjects.Length;
            originalPositions = new Vector2[count];
            originalScales = new Vector2[count];
            originalColors = new Color[count];
            currentPositions = new Vector2[count];
            targetPositions = new Vector2[count];

            // Store original properties for each object
            for (int i = 0; i < count; i++)
            {
                if (animatableObjects[i] != null)
                {
                    originalPositions[i] = animatableObjects[i].Position;
                    originalScales[i] = animatableObjects[i].Scale;
                    originalColors[i] = animatableObjects[i].Modulate;
                    currentPositions[i] = originalPositions[i];
                    targetPositions[i] = originalPositions[i];
                }
            }
        }

        // Create ground detection raycast
        SetupGroundDetection();
    }

    private void SetupGroundDetection()
    {
        // Create a raycast for ground distance detection
        groundDetectionRay = new RayCast3D();
        groundDetectionRay.Enabled = true;
        groundDetectionRay.TargetPosition = new Vector3(0, -maxFallDistance, 0);
        groundDetectionRay.CollisionMask = 1; // Adjust collision mask as needed

        // Add to player or find player and add there
        if (Components.Instance?.Player != null)
        {
            Components.Instance.Player.AddChild(groundDetectionRay);
        }
    }

    private float GetDistanceToGround()
    {
        if (groundDetectionRay == null)
            return maxFallDistance;

        // Force update the raycast
        groundDetectionRay.ForceRaycastUpdate();

        if (!groundDetectionRay.IsColliding())
            return maxFallDistance;

        Vector3 hitPoint = groundDetectionRay.GetCollisionPoint();
        Vector3 rayOrigin = groundDetectionRay.GlobalTransform.Origin;
        return rayOrigin.DistanceTo(hitPoint);
    }

    private float GetNormalizedDistanceToGround()
    {
        float distance = GetDistanceToGround();
        return Mathf.Clamp(distance / maxFallDistance, 0.0f, 1.0f);
    }

    // Enhanced animation methods with smooth state transitions
    public void OnStateChanged(string newState)
    {
        if (currentState == newState) return;

        previousState = currentState;
        currentState = newState;

        // Update current positions to where the objects actually are before transitioning
        if (animatableObjects != null)
        {
            for (int i = 0; i < animatableObjects.Length; i++)
            {
                if (animatableObjects[i] != null)
                {
                    currentPositions[i] = animatableObjects[i].Position;
                }
            }
        }

        switch (newState)
        {
            case "Wall Running":
                WallRunAnimation();
                break;
            case "Wall Jumping":
                WallJumpAnimation();
                break;
            case "Jumping":
                JumpAnimation();
                break;
            case "Falling":
                FallAnimation();
                break;
            case "Sprinting":
                SprintAnimation();
                break;
            case "Running":
                RunAnimation();
                break;
            case "Walking":
                WalkAnimation();
                break;
            case "Grounded":
            case "Idle":
                IdleAnimation();
                break;
            default:
                ResetToIdle();
                break;
        }
    }

    private void WallRunAnimation()
    {
        StopAllTweens();

        if (animatableObjects == null) return;

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (animatableObjects[i] == null) continue;

            // Calculate target position for wall running (slight offset from original)
            targetPositions[i] = originalPositions[i] + new Vector2(0, -10);

            // Smooth transition to wall run position from current position
            var positionTween = CreateSmoothTween($"position_{i}");
            positionTween.TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

            // Wait for position transition, then start vibration
            if (i == 0) // Only start vibration once for the first object
                positionTween.TweenCallback(Callable.From(StartWallRunVibration));

            // Intense glow effect with rapid pulsing
            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.Cyan * 1.4f, animationSpeed);

            // Scale pulse
            var scaleTween = CreateSmoothTween($"scale_{i}");
            scaleTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.15f, animationSpeed);
        }
    }

    private void StartWallRunVibration()
    {
        if (animatableObjects == null) return;

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (animatableObjects[i] == null) continue;

            // Start continuous shake effect for wall running intensity
            var shakeTween = CreateSmoothTween($"shake_{i}");
            int objectIndex = i; // Capture the index for the closure
            shakeTween.TweenMethod(Callable.From<float>((_) => ShakeFromTarget(objectIndex, new Vector2(6, 2))),
                0.0f, 1.0f, 0.1f);
            shakeTween.SetLoops();
        }
    }

    private void WallJumpAnimation()
    {
        StopAllTweens();

        if (animatableObjects == null) return;

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (animatableObjects[i] == null) continue;

            // Explosive burst effect
            var burstTween = CreateSmoothTween($"burst_{i}");
            burstTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.5f, 0.1f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
            burstTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.1f, 0.2f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Elastic);

            // Color flash
            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.Orange * 1.6f, 0.1f);
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.Orange * 1.2f, 0.3f);

            // Position bounce
            var posTween = CreateSmoothTween($"position_{i}");
            posTween.TweenProperty(animatableObjects[i], "position", originalPositions[i] + new Vector2(0, -30), 0.15f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
            posTween.TweenProperty(animatableObjects[i], "position", originalPositions[i], 0.4f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Bounce);
        }
    }

    private void JumpAnimation()
    {
        StopAllTweens();

        if (animatableObjects == null) return;

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (animatableObjects[i] == null) continue;

            // Smooth upward motion with anticipation
            targetPositions[i] = originalPositions[i] + new Vector2(0, -20);
            var posTween = CreateSmoothTween($"position_{i}");
            posTween.TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

            // Gentle scale increase
            var scaleTween = CreateSmoothTween($"scale_{i}");
            scaleTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.1f, animationSpeed);

            // Bright color
            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.LimeGreen * 1.3f, animationSpeed);
        }
    }

    private void FallAnimation()
    {
        StopAllTweens();

        // Start continuous fall animation that responds to ground distance
        StartDynamicFallAnimation();

        if (animatableObjects == null) return;

        // Color changes based on initial fall state
        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (animatableObjects[i] == null) continue;

            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.OrangeRed * 1.2f, animationSpeed);
        }
    }

    private void StartDynamicFallAnimation()
    {
        // Create a continuous tween that updates based on ground distance
        var fallTween = CreateSmoothTween("dynamicFall");
        fallTween.TweenMethod(Callable.From<float>(UpdateFallPosition), 0.0f, 1.0f, 0.05f);
        fallTween.SetLoops();
    }

    private void UpdateFallPosition(float _)
    {
        if (animatableObjects == null || currentState != "Falling") return;

        // Get current distance to ground
        float distanceToGround = GetDistanceToGround();
        float normalizedDistance = GetNormalizedDistanceToGround();

        // Calculate fall intensity (higher when closer to ground)
        float fallIntensity = 1.0f - normalizedDistance; // 0 = far from ground, 1 = close to ground

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (animatableObjects[i] == null) continue;

            // Calculate target position based on distance - higher means less offset
            float fallOffset = 5 + (normalizedDistance * 60); // 5-65 pixel range (more distance = more dramatic effect)
            Vector2 newTargetPosition = originalPositions[i] + new Vector2(0, fallOffset);

            // Calculate animation speed based on distance (slower when higher, faster when closer)
            float animSpeed = 0.8f - (fallIntensity * 0.6f); // 0.2-0.8 second range (closer = faster)
            animSpeed = Mathf.Max(animSpeed, 0.1f); // Minimum animation speed

            // Smoothly tween to the new position
            var posTween = CreateSmoothTween($"fallPosition_{i}");
            posTween.TweenProperty(animatableObjects[i], "position", newTargetPosition, animSpeed)
                .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);

            // Scale effect based on height (smaller when higher)
            float scaleReduction = 0.85f + (normalizedDistance * 0.1f); // 0.85-0.95 scale range
            var scaleTween = CreateSmoothTween($"fallScale_{i}");
            scaleTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * scaleReduction, animSpeed);

            // Color intensity based on fall proximity (more intense when closer to ground)
            Color fallColor = Colors.OrangeRed * (1.1f + fallIntensity * 0.7f);
            var colorTween = CreateSmoothTween($"fallColor_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", fallColor, animSpeed);
        }

        // Debug info (optional - remove in production)
        // GD.Print($"Distance to ground: {distanceToGround:F1}, Normalized: {normalizedDistance:F2}, Fall Intensity: {fallIntensity:F2}");
    }

    private void PrepareLandingBounce()
    {
        if (animatableObjects == null) return;

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (animatableObjects[i] == null) continue;

            // This method prepares the UI for the bounce when landing
            // We'll trigger this when the player is about to hit the ground
            var prepareTween = CreateSmoothTween($"prepareLanding_{i}");
            prepareTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 0.7f, 0.1f)
                .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Back);
        }
    }

    private void SprintAnimation()
    {
        StopAllTweens();

        if (animatableObjects == null) return;

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (animatableObjects[i] == null) continue;

            // Smooth transition to sprint position
            targetPositions[i] = originalPositions[i] + new Vector2(5, -5);
            var posTween = CreateSmoothTween($"position_{i}");
            posTween.TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed);

            // Fast pulse effect
            var pulseTween = CreateSmoothTween($"pulse_{i}");
            pulseTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.08f, 0.3f);
            pulseTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.12f, 0.3f);
            pulseTween.SetLoops();

            // Vibrant color
            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.Purple * 1.3f, animationSpeed);
        }
    }

    private void RunAnimation()
    {
        StopAllTweens();

        if (animatableObjects == null) return;

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (animatableObjects[i] == null) continue;

            // Smooth transition to run position
            targetPositions[i] = originalPositions[i] + new Vector2(2, -2);
            var posTween = CreateSmoothTween($"position_{i}");
            posTween.TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed);

            // Moderate bounce
            var bounceTween = CreateSmoothTween($"bounce_{i}");
            bounceTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.05f, 0.4f);
            bounceTween.TweenProperty(animatableObjects[i], "scale", originalScales[i], 0.4f);
            bounceTween.SetLoops();

            // Dynamic color
            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.DeepSkyBlue * 1.2f, animationSpeed);
        }
    }

    private void WalkAnimation()
    {
        StopAllTweens();

        if (animatableObjects == null) return;

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (animatableObjects[i] == null) continue;

            // Start gentle sway around a slightly offset position
            targetPositions[i] = originalPositions[i] + new Vector2(0, 2);

            // First move to base walk position
            var initialPosTween = CreateSmoothTween($"initialPos_{i}");
            initialPosTween.TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed);

            // Calm color
            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.LightBlue * 1.1f, animationSpeed);
        }

        // Then start sway animation (only need to call once)
        var firstObjectTween = CreateSmoothTween("initialPos_0");
        firstObjectTween.TweenCallback(Callable.From(StartWalkSway));
    }

    private void StartWalkSway()
    {
        if (animatableObjects == null) return;

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (animatableObjects[i] == null) continue;

            // Gentle sway around the target position
            var swayTween = CreateSmoothTween($"sway_{i}");
            swayTween.TweenProperty(animatableObjects[i], "position", targetPositions[i] + new Vector2(2, 0), 0.8f);
            swayTween.TweenProperty(animatableObjects[i], "position", targetPositions[i] + new Vector2(-2, 0), 0.8f);
            swayTween.SetLoops();
        }
    }

    private void IdleAnimation()
    {
        StopAllTweens();

        if (animatableObjects == null) return;

        // Check if we just came from falling state to trigger landing bounce
        bool justLanded = previousState == "Falling";

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (animatableObjects[i] == null) continue;

            if (justLanded)
            {
                // Landing bounce sequence
                var landingTween = CreateSmoothTween($"landing_{i}");

                // First: Quick compress (impact)
                landingTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 0.8f, 0.1f)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);

                // Second: Bounce up bigger than normal
                landingTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.3f, 0.2f)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Elastic);

                // Third: Settle to normal size
                landingTween.TweenProperty(animatableObjects[i], "scale", originalScales[i], 0.3f)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Bounce);

                // Position bounce back to original
                var posTween = CreateSmoothTween($"landingPosition_{i}");
                posTween.TweenProperty(animatableObjects[i], "position", originalPositions[i] + new Vector2(0, -15), 0.15f)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
                posTween.TweenProperty(animatableObjects[i], "position", originalPositions[i], 0.4f)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Bounce);

                // Color flash on landing
                var colorFlash = CreateSmoothTween($"landingColor_{i}");
                colorFlash.TweenProperty(animatableObjects[i], "modulate", Colors.White * 1.5f, 0.1f);
                colorFlash.TweenProperty(animatableObjects[i], "modulate", originalColors[i], 0.4f);
            }
            else
            {
                // Normal transition to idle
                // Smooth return to original position
                targetPositions[i] = originalPositions[i];
                var posTween = CreateSmoothTween($"position_{i}");
                posTween.TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed * 1.5f)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Elastic);

                // Return to original color and scale
                var colorTween = CreateSmoothTween($"color_{i}");
                colorTween.TweenProperty(animatableObjects[i], "modulate", originalColors[i], animationSpeed * 2);

                var scaleTween = CreateSmoothTween($"scale_{i}");
                scaleTween.TweenProperty(animatableObjects[i], "scale", originalScales[i], animationSpeed);
            }
        }

        // After landing/transition animation, start normal idle breathing (only need to call once)
        if (justLanded)
        {
            var firstLandingTween = CreateSmoothTween("landing_0");
            firstLandingTween.TweenCallback(Callable.From(StartIdleBreathing));
        }
        else
        {
            var firstPosTween = CreateSmoothTween("position_0");
            firstPosTween.TweenCallback(Callable.From(StartIdleBreathing));
        }
    }

    private void StartIdleBreathing()
    {
        if (animatableObjects == null) return;

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (animatableObjects[i] == null) continue;

            // Gentle breathing effect for idle state
            var breatheTween = CreateSmoothTween($"breathe_{i}");
            breatheTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.02f, 2.0f)
                .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
            breatheTween.TweenProperty(animatableObjects[i], "scale", originalScales[i], 2.0f)
                .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
            breatheTween.SetLoops();
        }
    }

    private void ResetToIdle()
    {
        StopAllTweens();

        if (animatableObjects == null) return;

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (animatableObjects[i] == null) continue;

            targetPositions[i] = originalPositions[i];
            var resetTween = CreateSmoothTween($"reset_{i}");
            resetTween.TweenProperty(animatableObjects[i], "scale", originalScales[i], transitionSpeed);
            resetTween.Parallel().TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed);
            resetTween.Parallel().TweenProperty(animatableObjects[i], "modulate", originalColors[i], transitionSpeed);
        }
    }

    // Utility methods
    private Tween CreateSmoothTween(string name)
    {
        if (activeTweens.ContainsKey(name))
        {
            activeTweens[name]?.Kill();
        }

        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetTrans(Tween.TransitionType.Cubic);
        activeTweens[name] = tween;

        return tween;
    }

    private void StopAllTweens()
    {
        foreach (var tween in activeTweens.Values)
        {
            tween?.Kill();
        }
        activeTweens.Clear();
    }

    private void SetObjectColor(int index, Color color)
    {
        if (animatableObjects != null && index >= 0 && index < animatableObjects.Length && animatableObjects[index] != null)
            animatableObjects[index].Modulate = color;
    }

    private void SetAllObjectsColor(Color color)
    {
        if (animatableObjects == null) return;

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (animatableObjects[i] != null)
                animatableObjects[i].Modulate = color;
        }
    }

    private void ShakeObject(int index, Vector2 offset)
    {
        if (animatableObjects == null || index < 0 || index >= animatableObjects.Length || animatableObjects[index] == null)
            return;

        var randomOffset = new Vector2(
            (float)(GD.Randf() - 0.5f) * offset.X,
            (float)(GD.Randf() - 0.5f) * offset.Y
        );
        animatableObjects[index].Position = originalPositions[index] + randomOffset;
    }

    private void ShakeFromTarget(int index, Vector2 offset)
    {
        if (animatableObjects == null || index < 0 || index >= animatableObjects.Length || animatableObjects[index] == null)
            return;

        var randomOffset = new Vector2(
            (float)(GD.Randf() - 0.5f) * offset.X,
            (float)(GD.Randf() - 0.5f) * offset.Y
        );
        animatableObjects[index].Position = targetPositions[index] + randomOffset;
    }

    // Legacy methods for backward compatibility (simplified)
    public void MoveLabel()
    {
        OnStateChanged("Jumping");
    }

    public void BounceLabel()
    {
        OnStateChanged("Falling");
    }

    public void SnapLabel()
    {
        ResetToIdle();
    }

    public void ScaleBounce()
    {
        OnStateChanged("Sprinting");
    }
}
