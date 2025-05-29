using Godot;
using System;
using System.Collections.Generic;
public partial class GameUI : Control
{
    public static GameUI Instance { get; private set; }

    [Export] private Control[] animatableObjects;
    [Export] private float animationSpeed = 0.15f;
    [Export] private float bounceIntensity = 1.2f;
    [Export] private float glowIntensity = 0.3f;
    [Export] private float maxFallDistance = 100.0f;
    [Export] private float transitionSpeed = 0.3f;

    private Vector2[] originalPositions;
    private Vector2[] originalScales;
    private Color[] originalColors;
    private Vector2[] currentPositions;
    private Vector2[] targetPositions;
    private Dictionary<string, Tween> activeTweens = new Dictionary<string, Tween>();
    private RayCast3D groundDetectionRay;

    private string previousState = "";

    public override void _Ready()
    {
        Instance = this;

        int count = animatableObjects.Length;
        originalPositions = new Vector2[count];
        originalScales = new Vector2[count];
        originalColors = new Color[count];
        currentPositions = new Vector2[count];
        targetPositions = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            originalPositions[i] = animatableObjects[i].Position;
            originalScales[i] = animatableObjects[i].Scale;
            originalColors[i] = animatableObjects[i].Modulate;
            currentPositions[i] = originalPositions[i];
            targetPositions[i] = originalPositions[i];
        }

        SetupGroundDetection();
    }

    private void SetupGroundDetection()
    {
        groundDetectionRay = new RayCast3D();
        groundDetectionRay.Enabled = true;
        groundDetectionRay.TargetPosition = new Vector3(0, -maxFallDistance, 0);
        groundDetectionRay.CollisionMask = 1;
        Components.Instance.Player.AddChild(groundDetectionRay);
    }

    private float GetDistanceToGround()
    {
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

    public void UpdatePreviousState(string newPreviousState)
    {
        previousState = newPreviousState;
    }

    public void UpdateCurrentPositions()
    {
        for (int i = 0; i < animatableObjects.Length; i++)
        {
            currentPositions[i] = animatableObjects[i].Position;
        }
    }

    public void WallRunAnimation()
    {
        StopAllTweens();

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            targetPositions[i] = originalPositions[i] + new Vector2(0, -10);

            var positionTween = CreateSmoothTween($"position_{i}");
            positionTween.TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

            if (i == 0)
                positionTween.TweenCallback(Callable.From(StartWallRunVibration));

            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.Cyan * 1.4f, animationSpeed);

            var scaleTween = CreateSmoothTween($"scale_{i}");
            scaleTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.15f, animationSpeed);
        }
    }

    private void StartWallRunVibration()
    {
        for (int i = 0; i < animatableObjects.Length; i++)
        {
            var shakeTween = CreateSmoothTween($"shake_{i}");
            int objectIndex = i;
            shakeTween.TweenMethod(Callable.From<float>((_) => ShakeFromTarget(objectIndex, new Vector2(6, 2))),
                0.0f, 1.0f, 0.1f);
            shakeTween.SetLoops();
        }
    }

    public void WallJumpAnimation()
    {
        StopAllTweens();

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            var burstTween = CreateSmoothTween($"burst_{i}");
            burstTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.5f, 0.1f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
            burstTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.1f, 0.2f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Elastic);

            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.Orange * 1.6f, 0.1f);
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.Orange * 1.2f, 0.3f);

            var posTween = CreateSmoothTween($"position_{i}");
            posTween.TweenProperty(animatableObjects[i], "position", originalPositions[i] + new Vector2(0, -30), 0.15f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
            posTween.TweenProperty(animatableObjects[i], "position", originalPositions[i], 0.4f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Bounce);
        }
    }

    public void JumpAnimation()
    {
        StopAllTweens();

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            targetPositions[i] = originalPositions[i] + new Vector2(0, -20);
            var posTween = CreateSmoothTween($"position_{i}");
            posTween.TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

            var scaleTween = CreateSmoothTween($"scale_{i}");
            scaleTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.1f, animationSpeed);

            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.LimeGreen * 1.3f, animationSpeed);
        }
    }

    public void FallAnimation()
    {
        StopAllTweens();
        StartDynamicFallAnimation();

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.OrangeRed * 1.2f, animationSpeed);
        }
    }

    private void StartDynamicFallAnimation()
    {
        var fallTween = CreateSmoothTween("dynamicFall");
        fallTween.TweenMethod(Callable.From<float>(UpdateFallPosition), 0.0f, 1.0f, 0.05f);
        fallTween.SetLoops();
    }

    private void UpdateFallPosition(float _)
    {
        if (Components.Instance.StateMachine.CurrentState != "Falling") return;

        float distanceToGround = GetDistanceToGround();
        float normalizedDistance = GetNormalizedDistanceToGround();
        float fallIntensity = 1.0f - normalizedDistance;

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            float fallOffset = 5 + (normalizedDistance * 60);
            Vector2 newTargetPosition = originalPositions[i] + new Vector2(0, fallOffset);

            float animSpeed = 0.8f - (fallIntensity * 0.6f);
            animSpeed = Mathf.Max(animSpeed, 0.1f);

            var posTween = CreateSmoothTween($"fallPosition_{i}");
            posTween.TweenProperty(animatableObjects[i], "position", newTargetPosition, animSpeed)
                .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);

            float scaleReduction = 0.85f + (normalizedDistance * 0.1f);
            var scaleTween = CreateSmoothTween($"fallScale_{i}");
            scaleTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * scaleReduction, animSpeed);

            Color fallColor = Colors.OrangeRed * (1.1f + fallIntensity * 0.7f);
            var colorTween = CreateSmoothTween($"fallColor_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", fallColor, animSpeed);
        }
    }

    public void SprintAnimation()
    {
        StopAllTweens();

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            targetPositions[i] = originalPositions[i] + new Vector2(5, -5);
            var posTween = CreateSmoothTween($"position_{i}");
            posTween.TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed);

            var pulseTween = CreateSmoothTween($"pulse_{i}");
            pulseTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.08f, 0.3f);
            pulseTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.12f, 0.3f);
            pulseTween.SetLoops();

            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.Purple * 1.3f, animationSpeed);
        }
    }

    public void RunAnimation()
    {
        StopAllTweens();

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            targetPositions[i] = originalPositions[i] + new Vector2(2, -2);
            var posTween = CreateSmoothTween($"position_{i}");
            posTween.TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed);

            var bounceTween = CreateSmoothTween($"bounce_{i}");
            bounceTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.05f, 0.4f);
            bounceTween.TweenProperty(animatableObjects[i], "scale", originalScales[i], 0.4f);
            bounceTween.SetLoops();

            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.DeepSkyBlue * 1.2f, animationSpeed);
        }
    }

    public void WalkAnimation()
    {
        StopAllTweens();

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            targetPositions[i] = originalPositions[i] + new Vector2(0, 2);

            var initialPosTween = CreateSmoothTween($"initialPos_{i}");
            initialPosTween.TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed);

            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.LightBlue * 1.1f, animationSpeed);
        }

        var firstObjectTween = CreateSmoothTween("initialPos_0");
        firstObjectTween.TweenCallback(Callable.From(StartWalkSway));
    }

    private void StartWalkSway()
    {
        for (int i = 0; i < animatableObjects.Length; i++)
        {
            var swayTween = CreateSmoothTween($"sway_{i}");
            swayTween.TweenProperty(animatableObjects[i], "position", targetPositions[i] + new Vector2(2, 0), 0.8f);
            swayTween.TweenProperty(animatableObjects[i], "position", targetPositions[i] + new Vector2(-2, 0), 0.8f);
            swayTween.SetLoops();
        }
    }

    public void IdleAnimation()
    {
        StopAllTweens();

        bool justLanded = previousState == "Falling";

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            if (justLanded)
            {
                var landingTween = CreateSmoothTween($"landing_{i}");
                landingTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 0.8f, 0.1f)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
                landingTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.3f, 0.2f)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Elastic);
                landingTween.TweenProperty(animatableObjects[i], "scale", originalScales[i], 0.3f)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Bounce);

                var posTween = CreateSmoothTween($"landingPosition_{i}");
                posTween.TweenProperty(animatableObjects[i], "position", originalPositions[i] + new Vector2(0, -15), 0.15f)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
                posTween.TweenProperty(animatableObjects[i], "position", originalPositions[i], 0.4f)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Bounce);

                var colorFlash = CreateSmoothTween($"landingColor_{i}");
                colorFlash.TweenProperty(animatableObjects[i], "modulate", Colors.White * 1.5f, 0.1f);
                colorFlash.TweenProperty(animatableObjects[i], "modulate", originalColors[i], 0.4f);
            }
            else
            {
                targetPositions[i] = originalPositions[i];
                var posTween = CreateSmoothTween($"position_{i}");
                posTween.TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed * 1.5f)
                    .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Elastic);

                var colorTween = CreateSmoothTween($"color_{i}");
                colorTween.TweenProperty(animatableObjects[i], "modulate", originalColors[i], animationSpeed * 2);

                var scaleTween = CreateSmoothTween($"scale_{i}");
                scaleTween.TweenProperty(animatableObjects[i], "scale", originalScales[i], animationSpeed);
            }
        }

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
        for (int i = 0; i < animatableObjects.Length; i++)
        {
            var breatheTween = CreateSmoothTween($"breathe_{i}");
            breatheTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.02f, 2.0f)
                .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
            breatheTween.TweenProperty(animatableObjects[i], "scale", originalScales[i], 2.0f)
                .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
            breatheTween.SetLoops();
        }
    }

    public void ResetToIdle()
    {
        StopAllTweens();

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            targetPositions[i] = originalPositions[i];
            var resetTween = CreateSmoothTween($"reset_{i}");
            resetTween.TweenProperty(animatableObjects[i], "scale", originalScales[i], transitionSpeed);
            resetTween.Parallel().TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed);
            resetTween.Parallel().TweenProperty(animatableObjects[i], "modulate", originalColors[i], transitionSpeed);
        }
    }

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
        animatableObjects[index].Modulate = color;
    }

    private void SetAllObjectsColor(Color color)
    {
        for (int i = 0; i < animatableObjects.Length; i++)
        {
            animatableObjects[i].Modulate = color;
        }
    }

    private void ShakeObject(int index, Vector2 offset)
    {
        var randomOffset = new Vector2(
            (float)(GD.Randf() - 0.5f) * offset.X,
            (float)(GD.Randf() - 0.5f) * offset.Y
        );
        animatableObjects[index].Position = originalPositions[index] + randomOffset;
    }

    private void ShakeFromTarget(int index, Vector2 offset)
    {
        var randomOffset = new Vector2(
            (float)(GD.Randf() - 0.5f) * offset.X,
            (float)(GD.Randf() - 0.5f) * offset.Y
        );
        animatableObjects[index].Position = targetPositions[index] + randomOffset;
    }

    public void MoveLabel()
    {
        if (Components.Instance?.StateMachine != null)
            Components.Instance.StateMachine.TriggerState("Jumping");
    }

    public void BounceLabel()
    {
        if (Components.Instance?.StateMachine != null)
            Components.Instance.StateMachine.TriggerState("Falling");
    }

    public void SnapLabel()
    {
        ResetToIdle();
    }

    public void ScaleBounce()
    {
        if (Components.Instance?.StateMachine != null)
            Components.Instance.StateMachine.TriggerState("Sprinting");
    }
}
