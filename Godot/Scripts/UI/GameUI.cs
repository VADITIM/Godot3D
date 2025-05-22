using Godot;
using System;

public partial class GameUI : Control
{
    public static GameUI Instance { get; private set; }

    [Export] private Label myLabel;
    private Vector2 originalPosition;

    public override void _Ready()
    {
        Instance = this;
        originalPosition = myLabel.Position;
    }

    public void MoveLabel()
    {
        var tween = CreateTween();
        tween.TweenProperty(myLabel, "position", 
            new Vector2(originalPosition.X, originalPosition.Y - 50), 0.2)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);
    }

    public void BounceLabel()
    {
        var tween = CreateTween();
        tween.TweenProperty(myLabel, "position",
            new Vector2(originalPosition.X, originalPosition.Y - 10), 3.4)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);

        
    }

    public void SnapLabel()
    {
        var tween = CreateTween();

        // Bounce downward
        tween.TweenProperty(myLabel, "position",
            new Vector2(originalPosition.X, originalPosition.Y), 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Bounce);

    }

    
    public void ScaleBounce()
    {
        if (myLabel == null) return;

        Vector2 originalScale = myLabel.Scale;
        var tween = CreateTween();

        tween.TweenProperty(myLabel, "scale", originalScale * 1.3f, 0.2)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quad);

        tween.TweenProperty(myLabel, "scale", originalScale, 0.5)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Bounce);
    }
}
