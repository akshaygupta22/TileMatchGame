using Godot;
using TileMatchGame.Data;

namespace TileMatchGame.Gameplay;

public partial class Piece : Node2D
{
    private static readonly Color SelectedTint = new Color(1.3f, 1.3f, 0.7f);
    private static readonly Color InvalidTint = new Color(1.6f, 0.4f, 0.4f);
    private static readonly Color NormalTint = Colors.White;
    private static readonly Vector2 SelectedScale = new Vector2(1.15f, 1.15f);

    private static readonly System.Collections.Generic.Dictionary<PieceType, Color> AccentColors = new()
    {
        { PieceType.Red, new Color(0.85f, 0.2f, 0.2f) },
        { PieceType.Blue, new Color(0.2f, 0.4f, 0.85f) },
        { PieceType.Green, new Color(0.2f, 0.75f, 0.3f) },
        { PieceType.Yellow, new Color(0.9f, 0.85f, 0.15f) },
        { PieceType.Purple, new Color(0.6f, 0.25f, 0.8f) },
        { PieceType.Orange, new Color(0.95f, 0.55f, 0.1f) }
    };

    private Sprite2D _sprite;
    private CpuParticles2D _clearParticles;
    private Label _specialIcon;
    private static Texture2D _dotTexture;

    public PieceType Type { get; private set; }
    public TileInfo Data { get; private set; }
    public Vector2I GridPosition { get; set; }
    public SpecialPieceKind SpecialKind { get; private set; } = SpecialPieceKind.None;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _clearParticles = GetNodeOrNull<CpuParticles2D>("ClearParticles");
        _specialIcon = GetNodeOrNull<Label>("SpecialIcon");
        if (_clearParticles != null)
        {
            _clearParticles.Texture = GetDotTexture();
        }
    }

    public void Setup(PieceType type, TileInfo data)
    {
        Type = type;
        Data = data;

        _sprite ??= GetNode<Sprite2D>("Sprite2D");
        _sprite.Texture = data.SpriteTexture;
        SetSpecialKind(SpecialPieceKind.None);
    }

    /// <summary>Marks this piece as a special power-up (or clears it back to normal) and updates its icon overlay.</summary>
    public void SetSpecialKind(SpecialPieceKind kind)
    {
        SpecialKind = kind;

        _specialIcon ??= GetNodeOrNull<Label>("SpecialIcon");
        if (_specialIcon == null)
        {
            return;
        }

        _specialIcon.Text = kind switch
        {
            SpecialPieceKind.LineClearRow => "\u2194",
            SpecialPieceKind.LineClearCol => "\u2195",
            SpecialPieceKind.ColorBomb => "\u2605",
            _ => ""
        };
        _specialIcon.Visible = kind != SpecialPieceKind.None;
    }

    /// <summary>
    /// Detaches the clear-particle burst from this piece (which is about to be
    /// freed), reparents it to the given host so it can finish playing, and
    /// frees it once emission completes.
    /// </summary>
    public void PlayClearEffect(Node2D host)
    {
        if (_clearParticles == null || host == null)
        {
            return;
        }

        var globalPos = GlobalPosition;
        _clearParticles.GetParent()?.RemoveChild(_clearParticles);
        host.AddChild(_clearParticles);
        _clearParticles.GlobalPosition = globalPos;
        _clearParticles.Color = AccentColors.TryGetValue(Type, out var c) ? c : Colors.White;
        _clearParticles.Emitting = true;
        _clearParticles.Finished += () => _clearParticles.QueueFree();
    }

    private static Texture2D GetDotTexture()
    {
        if (_dotTexture != null)
        {
            return _dotTexture;
        }

        const int size = 8;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        var center = new Vector2(size / 2f, size / 2f);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = new Vector2(x + 0.5f, y + 0.5f).DistanceTo(center);
                image.SetPixel(x, y, dist <= size / 2f ? Colors.White : Colors.Transparent);
            }
        }

        _dotTexture = ImageTexture.CreateFromImage(image);
        return _dotTexture;
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
