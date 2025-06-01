using Godot;
using System;
using System.Collections.Generic;

public partial class UIAnimations : Node
{
    public static UIAnimations Instance { get; private set; }

    [Export] private Control[] animatableObjects;
    [Export] private float animationSpeed = 0.15f;
    [Export] private float bounceIntensity = 1.2f;
    [Export] private float transitionSpeed = 0.3f;
    private Vector2[] originalPositions;
    private Vector2[] originalScales;
    private Color[] originalColors;
    private Vector2[] currentPositions;
    private Vector2[] targetPositions;
    private Dictionary<string, Tween> activeTweens = new Dictionary<string, Tween>();
    private string previousState = "";

    public bool IsFalling() => isFalling;
    private bool isFalling = false;
    private float fallDuration = 3.0f;


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
            targetPositions[i] = animatableObjects[i].Position; // Update target to current position
        }
    }

    // --------------------------------------------------------------------------------------------------------------------------------
    // --------------------------------------------------------------------------------------------------------------------------------
    // --------------------------------------------------------------------------------------------------------------------------------
    #region WALL RUNNING

    public void WallRunAnimation()
    {
        StopAllTweens();

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            targetPositions[i] = originalPositions[i] + new Vector2(0, -25);

            var positionTween = CreateSmoothTween($"position_{i}");
            positionTween.TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

            PopUp();

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

            shakeTween.TweenMethod(Callable.From<float>((_) => ShakeFromTargetWithDynamicIntensity(objectIndex)),
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
    #endregion
    // --------------------------------------------------------------------------------------------------------------------------------
    // --------------------------------------------------------------------------------------------------------------------------------
    // --------------------------------------------------------------------------------------------------------------------------------
    #region JUMPING - FALLING

    public void JumpAnimation()
    {
        StopAllTweens();

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            targetPositions[i] = originalPositions[i] + new Vector2(0, -40);

            // PopUp();

            var posTween = CreateSmoothTween($"position_{i}");
            posTween.Parallel().TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.3f, 0.13f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
            posTween.Parallel().TweenProperty(animatableObjects[i], "scale", originalScales[i], 0.13f)
                .SetDelay(.13f);

            posTween.Parallel().TweenProperty(animatableObjects[i], "position", targetPositions[i], 0.8f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Spring);

            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.Parallel().TweenProperty(animatableObjects[i], "modulate", Colors.LimeGreen * 1.3f, animationSpeed);
        }
    }

    public void PopUp()
    {
        for (int i = 0; i < animatableObjects.Length; i++)
        {
            var pop = CreateSmoothTween($"popUp_{i}");
            pop.Parallel().TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.35f, 0.06f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
            pop.Parallel().TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.15f, 0.06f)
                .SetDelay(.06f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
            pop.Parallel().TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.25f, 0.06f)
                .SetDelay(.12f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
            pop.Parallel().TweenProperty(animatableObjects[i], "scale", originalScales[i], 0.06f)
                .SetDelay(.18f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
        }
    }

    public void FallAnimation()
    {
        StopAllTweens();
        float fallDuration = 3f;

        UpdateCurrentPositions();

        isFalling = true;

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            Vector2 fallTargetPosition = currentPositions[i] + new Vector2(0, 80);


            var fallTween = CreateSmoothTween($"fall_{i}");
            // fallTween.TweenProperty(animatableObjects[i], "$1", $2[1], $3, $5)
            // .SetEase(Tween.EasyType.$6).SetTrans(Tween.TransitionType.$7);
            fallTween.Parallel().TweenProperty(animatableObjects[i], "scale", originalScales[i] * new Vector2(.9f, .9f), .03f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            fallTween.Parallel().TweenProperty(animatableObjects[i], "scale", originalScales[i] * new Vector2(1.1f, 1.1f), .03f)
                .SetDelay(.03f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

            fallTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * new Vector2(.8f, .8f), fallDuration * 2.5f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            fallTween.Parallel().TweenProperty(animatableObjects[i], "position", fallTargetPosition, fallDuration)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            fallTween.Parallel().TweenProperty(animatableObjects[i], "modulate", Colors.OrangeRed * 1.3f, 0.3f);
        }

        // Timer to track fall completion
        var fallTimerTween = CreateSmoothTween("fallTimer");
        fallTimerTween.TweenInterval(fallDuration);
        fallTimerTween.TweenCallback(Callable.From(OnFallAnimationComplete));
    }

    private void OnFallAnimationComplete()
    {
        isFalling = false;
        // Animation naturally completed - objects stay in stretched position
    }

    // Apple-style bounce back when landing from fall
    public void ElasticAnimation(string newState)
    {
        if (!isFalling) return;

        isFalling = false;
        StopAllTweens();

        // Apple-style bounce back to original position with elastic effect
        for (int i = 0; i < animatableObjects.Length; i++)
        {
            var bounceTween = CreateSmoothTween($"fallBounce_{i}");

            // Quick compression
            bounceTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * .85f, 0.1f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);

            // Elastic expansion


            // Settle to original scale
            bounceTween.TweenProperty(animatableObjects[i], "scale", originalScales[i], .25f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Bounce);

            // Bounce position back to original
            var bouncePosTween = CreateSmoothTween($"fallBouncePos_{i}");
            bouncePosTween.TweenProperty(animatableObjects[i], "position", originalPositions[i], 0.5f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Bounce);

            // Flash color effect
            var bounceColorTween = CreateSmoothTween($"fallBounceColor_{i}");
            bounceColorTween.TweenProperty(animatableObjects[i], "modulate", Colors.White * 1.8f, 0.08f);
            bounceColorTween.TweenProperty(animatableObjects[i], "modulate", originalColors[i], 0.42f);
        }
    }
    #endregion
    // --------------------------------------------------------------------------------------------------------------------------------
    // --------------------------------------------------------------------------------------------------------------------------------
    // --------------------------------------------------------------------------------------------------------------------------------
    #region MOVING

    public void MoveAnimation()
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


    public void SprintAnimation()
    {
        StopAllTweens();

        for (int i = 0; i < animatableObjects.Length; i++)
        {
            targetPositions[i] = originalPositions[i] + new Vector2(5, -5);
            var posTween = CreateSmoothTween($"position_{i}");
            posTween.TweenProperty(animatableObjects[i], "position", targetPositions[i], transitionSpeed);

            var colorTween = CreateSmoothTween($"color_{i}");
            colorTween.TweenProperty(animatableObjects[i], "modulate", Colors.Purple * 1.3f, animationSpeed);

            var pulseTween = CreateSmoothTween($"pulse_{i}");
            pulseTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.2f, 0.3f);
            pulseTween.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.12f, 0.3f);

            var pulseLoop = CreateSmoothTween($"pulseLoop_{i}");
            pulseLoop.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.15f, 0.25f)
                .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
            pulseLoop.TweenProperty(animatableObjects[i], "scale", originalScales[i] * 1.12f, 0.25f)
                .SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Sine);
            pulseLoop.SetLoops();
        }
    }

    #endregion
    // --------------------------------------------------------------------------------------------------------------------------------
    // --------------------------------------------------------------------------------------------------------------------------------
    // --------------------------------------------------------------------------------------------------------------------------------
    #region IDLE

    public void IdleAnimation()
    {
        StopAllTweens();

        for (int i = 0; i < animatableObjects.Length; i++)
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

        // Start breathing effect after initial animation
        var breathingTween = CreateSmoothTween("startBreathing");
        breathingTween.TweenCallback(Callable.From(StartIdleBreathing)).SetDelay(transitionSpeed * 1.5f);
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
    #endregion
    // --------------------------------------------------------------------------------------------------------------------------------
    // --------------------------------------------------------------------------------------------------------------------------------
    // --------------------------------------------------------------------------------------------------------------------------------
    #region SMOOTH TWEEN CREATION

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

    // --------------------------------------------------------------------------------------------------------------------------------

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

    private void ShakeHeavyFromTarget(int index, Vector2 offset)
    {
        var randomOffset = new Vector2(
            (float)(GD.Randf() - 1f) * offset.X,
            (float)(GD.Randf() - 1f) * offset.Y
        );
        animatableObjects[index].Position = targetPositions[index] + randomOffset;
    }
    #endregion
    // --------------------------------------------------------------------------------------------------------------------------------

    public void MoveLabel()
    {
        if (Components.Instance?.StateMachine != null)
            Components.Instance.StateMachine.TriggerState("Jumping");
    }

    public void ScaleBounce()
    {
        if (Components.Instance?.StateMachine != null)
            Components.Instance.StateMachine.TriggerState("Sprinting");
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

    private void ShakeFromTargetWithDynamicIntensity(int index)
    {
        // Calculate intensity based on current speed (0 to 300) mapped to (0 to 10)
        float currentSpeed = Components.Instance.Movement.currentSpeed;
        float normalizedSpeed = Mathf.Clamp(currentSpeed / 300f, 0f, 1f); // 0 to 1
        float intensity = normalizedSpeed * 10f; // 0 to 10

        // Scale shake offset based on intensity
        Vector2 shakeOffset = new Vector2(intensity, intensity);

        var randomOffset = new Vector2(
            (float)(GD.Randf() - 0.5f) * shakeOffset.X,
            (float)(GD.Randf() - 0.5f) * shakeOffset.Y
        );
        animatableObjects[index].Position = targetPositions[index] + randomOffset;
    }
}
