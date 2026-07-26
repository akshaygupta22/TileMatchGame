using Godot;

namespace TileMatchGame.Data;

[GlobalClass]
public partial class TileInfo : Resource
{
    [Export] public string TileType { get; set; } = "";
    [Export] public Texture2D SpriteTexture { get; set; }
    [Export] public int ScoreValue { get; set; } = 10;
    [Export] public bool IsSpecialBomb { get; set; } = false;
}
