using Godot;

namespace TileMatchGame.Utils;

/// <summary>
/// Converts between grid coordinates and local screen pixel positions.
/// </summary>
public static class GridUtils
{
    /// <summary>
    /// Returns the CENTER of the given cell in local space. Sprite2D nodes are
    /// centered on their Position by default, so pieces must be placed at cell
    /// centers (not the top-left corner) or every tile renders half a tile
    /// off from the click bucket that LocalToGrid computes for it.
    /// </summary>
    public static Vector2 GridToLocal(Vector2I gridPos, int tileSize, Vector2 gridOffset)
    {
        var half = tileSize / 2f;
        return gridOffset + new Vector2(gridPos.X * tileSize + half, gridPos.Y * tileSize + half);
    }

    public static Vector2I LocalToGrid(Vector2 localPos, int tileSize, Vector2 gridOffset)
    {
        var relative = localPos - gridOffset;
        return new Vector2I(
            Mathf.FloorToInt(relative.X / tileSize),
            Mathf.FloorToInt(relative.Y / tileSize)
        );
    }
}
