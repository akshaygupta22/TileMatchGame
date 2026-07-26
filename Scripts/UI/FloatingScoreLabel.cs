using Godot;

namespace TileMatchGame.UI;

/// <summary>
/// Small floating "+N" text spawned at a match location; tweens upward and
/// fades out, then frees itself. Spawned dynamically by ScoreUI.
/// </summary>
public partial class FloatingScoreLabel : Label
{
    public void Play(string text, Color color)
    {
        Text = text;
        Modulate = color;

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(this, "position", Position + new Vector2(0, -40), 0.6)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "modulate:a", 0.0, 0.6).SetDelay(0.1);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}
