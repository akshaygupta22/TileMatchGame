using Godot;
using TileMatchGame.Data;

namespace TileMatchGame.Gameplay;

public partial class Piece : Node2D
{
    private static readonly Color SelectedTint = new Color(1.3f, 1.3f, 0.7f);
    private static readonly Color InvalidTint = new Color(1.6f, 0.4f, 0.4f);
    private static readonly Color NormalTint = Colors.White;
    private static readonly Vector2 SelectedScale = new Vector2(1.15f, 1.15f);

    private Sprite2D _sprite;

    public PieceType Type { get; private set; }
    public TileInfo Data { get; private set; }
    public Vector2I GridPosition { get; set; }

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");
    }

    public void Setup(PieceType type, TileInfo data)
    {
        Type = type;
        Data = data;

        _sprite ??= GetNode<Sprite2D>("Sprite2D");
        _sprite.Texture = data.SpriteTexture;
    }

    /// <summary>Toggles the selection highlight (tint + slight scale up).</summary>
    public void SetSelected(bool selected)
    {
        Modulate = selected ? SelectedTint : NormalTint;
        Scale = selected ? SelectedScale : Vector2.One;
    }

    /// <summary>Briefly flashes red to indicate an invalid swap attempt.</summary>
    public void FlashInvalid()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", InvalidTint, 0.08);
        tween.TweenProperty(this, "modulate", NormalTint, 0.08);
    }
}
