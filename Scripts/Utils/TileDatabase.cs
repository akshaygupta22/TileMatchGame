using System;
using System.Collections.Generic;
using Godot;
using TileMatchGame.Data;

namespace TileMatchGame.Utils;

/// <summary>
/// Autoload singleton that builds TileData resources with runtime-generated
/// placeholder textures (solid colored squares), one per PieceType.
/// </summary>
public partial class TileDatabase : Node
{
    private const int TextureSize = 64;

    private readonly Dictionary<PieceType, TileInfo> _tileData = new();

    private static readonly Dictionary<PieceType, Color> PlaceholderColors = new()
    {
        { PieceType.Red, new Color(0.85f, 0.2f, 0.2f) },
        { PieceType.Blue, new Color(0.2f, 0.4f, 0.85f) },
        { PieceType.Green, new Color(0.2f, 0.75f, 0.3f) },
        { PieceType.Yellow, new Color(0.9f, 0.85f, 0.15f) },
        { PieceType.Purple, new Color(0.6f, 0.25f, 0.8f) }
    };

    public override void _Ready()
    {
        foreach (PieceType type in Enum.GetValues<PieceType>())
        {
            _tileData[type] = BuildTileData(type);
        }
    }

    public TileInfo GetTileData(PieceType type) => _tileData[type];

    private static TileInfo BuildTileData(PieceType type)
    {
        var image = Image.CreateEmpty(TextureSize, TextureSize, false, Image.Format.Rgba8);
        image.Fill(PlaceholderColors[type]);
        var texture = ImageTexture.CreateFromImage(image);

        return new TileInfo
        {
            TileType = type.ToString(),
            SpriteTexture = texture,
            ScoreValue = 10,
            IsSpecialBomb = false
        };
    }
}
