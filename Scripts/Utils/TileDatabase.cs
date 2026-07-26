using System;
using System.Collections.Generic;
using Godot;
using TileMatchGame.Data;

namespace TileMatchGame.Utils;

/// <summary>
/// Autoload singleton that builds TileData resources. Loads real fruit sprite
/// textures from Assets/Sprites/Fruits; falls back to a runtime-generated
/// solid colored square if a sprite file is missing.
/// </summary>
public partial class TileDatabase : Node
{
    private const int FallbackTextureSize = 64;

    /// <summary>Tier at which the bonus 6th color (Orange) unlocks.</summary>
    private const int OrangeUnlockTier = 3;

    private readonly Dictionary<PieceType, TileInfo> _tileData = new();

    // Note: PieceType.Blue has no matching fruit sprite in the art pack, so it
    // uses Kiwi as the closest cool-toned look-alike.
    private static readonly Dictionary<PieceType, string> SpritePaths = new()
    {
        { PieceType.Red, "res://Assets/Sprites/Fruits/Strawberry.png" },
        { PieceType.Blue, "res://Assets/Sprites/Fruits/Kiwi.png" },
        { PieceType.Green, "res://Assets/Sprites/Fruits/Apple.png" },
        { PieceType.Yellow, "res://Assets/Sprites/Fruits/Lemon.png" },
        { PieceType.Purple, "res://Assets/Sprites/Fruits/Grape.png" },
        { PieceType.Orange, "res://Assets/Sprites/Fruits/Orange.png" }
    };

    private static readonly Dictionary<PieceType, Color> FallbackColors = new()
    {
        { PieceType.Red, new Color(0.85f, 0.2f, 0.2f) },
        { PieceType.Blue, new Color(0.2f, 0.4f, 0.85f) },
        { PieceType.Green, new Color(0.2f, 0.75f, 0.3f) },
        { PieceType.Yellow, new Color(0.9f, 0.85f, 0.15f) },
        { PieceType.Purple, new Color(0.6f, 0.25f, 0.8f) },
        { PieceType.Orange, new Color(0.95f, 0.55f, 0.1f) }
    };

    public override void _Ready()
    {
        foreach (PieceType type in Enum.GetValues<PieceType>())
        {
            _tileData[type] = BuildTileData(type);
        }
    }

    public TileInfo GetTileData(PieceType type) => _tileData[type];

    /// <summary>
    /// Returns the subset of PieceType values unlocked at the given tier.
    /// Tier 1 starts with the 5 base colors; Orange unlocks at tier 3+.
    /// </summary>
    public static IReadOnlyList<PieceType> GetActiveTypes(int tier)
    {
        var types = new List<PieceType>
        {
            PieceType.Red, PieceType.Blue, PieceType.Green, PieceType.Yellow, PieceType.Purple
        };

        if (tier >= OrangeUnlockTier)
        {
            types.Add(PieceType.Orange);
        }

        return types;
    }

    private static TileInfo BuildTileData(PieceType type)
    {
        var texture = LoadSpriteTexture(type) ?? GenerateFallbackTexture(type);

        return new TileInfo
        {
            TileType = type.ToString(),
            SpriteTexture = texture,
            ScoreValue = 10,
            IsSpecialBomb = false
        };
    }

    private static Texture2D LoadSpriteTexture(PieceType type)
    {
        if (!SpritePaths.TryGetValue(type, out var path) || !ResourceLoader.Exists(path))
        {
            return null;
        }

        return GD.Load<Texture2D>(path);
    }

    private static Texture2D GenerateFallbackTexture(PieceType type)
    {
        var image = Image.CreateEmpty(FallbackTextureSize, FallbackTextureSize, false, Image.Format.Rgba8);
        image.Fill(FallbackColors[type]);
        return ImageTexture.CreateFromImage(image);
    }
}
