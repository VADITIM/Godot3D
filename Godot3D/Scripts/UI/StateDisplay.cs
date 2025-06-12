using Godot;

public partial class StateDisplay : Label
{
    [Export] private string prefix = "";
    [Export] private bool enableColorTransitions = true;
    [Export] private float colorTransitionSpeed = 0.2f; private string lastState = "";
    private Tween colorTween; public override void _Ready()
    {
        // Subscribe to StateMachine's state change events
        if (Components.Instance?.StateMachine != null)
        {
            Components.Instance.StateMachine.StateChanged += OnStateChanged;

            // Initialize with current state if available
            if (!string.IsNullOrEmpty(Components.Instance.StateMachine.CurrentState))
            {
                OnStateChanged(Components.Instance.StateMachine.CurrentState);
            }
        }
        else
        {
            // Wait for StateMachine to be ready
            GetTree().CreateTimer(0.1f).Timeout += () => _Ready();
        }
    }
    public override void _ExitTree()
    {
        // Unsubscribe from StateMachine events to prevent memory leaks
        if (Components.Instance?.StateMachine != null)
        {
            Components.Instance.StateMachine.StateChanged -= OnStateChanged;
        }
    }

    private void OnStateChanged(string newState)
    {
        // Only update if state has changed
        if (newState != lastState)
        {
            lastState = newState;
            Text = $"{prefix}{newState}";            // Trigger smooth color transition
            if (enableColorTransitions && Components.Instance?.StateMachine != null)
            {
                AnimateColorTransition(newState);
            }
        }
    }
    private void AnimateColorTransition(string state)
    {
        if (Components.Instance?.StateMachine == null) return;

        Color targetColor = Components.Instance.StateMachine.GetStateColor(state);

        // Kill existing color tween
        colorTween?.Kill();

        // Get current color or use white as default
        Color currentColor = GetThemeColor("font_color", "Label");
        if (currentColor == Colors.Black) // Default theme color, use white instead
            currentColor = Colors.White;

        // Create smooth color transition
        colorTween = CreateTween();
        colorTween.TweenMethod(Callable.From<Color>(SetFontColor),
            currentColor, targetColor, colorTransitionSpeed)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);
    }

    private void SetFontColor(Color color)
    {
        AddThemeColorOverride("font_color", color);
    }
}
