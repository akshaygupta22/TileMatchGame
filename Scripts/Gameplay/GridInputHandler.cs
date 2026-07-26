using System;
using Godot;
using TileMatchGame.Utils;

namespace TileMatchGame.Gameplay;

/// <summary>
/// Detects mouse clicks / touch presses on the board and reports the
/// grid coordinate that was targeted. TileSize/GridOffset are pushed in by
/// GridManager (via Configure) so the two never fall out of sync.
/// </summary>
public partial class GridInputHandler : Node2D
{
    private int _tileSize = 64;
    private Vector2 _gridOffset = Vector2.Zero;

    public event Action<Vector2I> OnTileSelected;

    public void Configure(int tileSize, Vector2 gridOffset)
    {
        _tileSize = tileSize;
        _gridOffset = gridOffset;
    }

    public override void _Input(InputEvent @event)
    {
        Vector2? screenPos = null;

        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed &&
            mouseButton.ButtonIndex == MouseButton.Left)
        {
            screenPos = mouseButton.Position;
        }
        else if (@event is InputEventScreenTouch screenTouch && screenTouch.Pressed)
        {
            screenPos = screenTouch.Position;
        }

        if (screenPos.HasValue)
        {
            // Convert viewport/screen space to canvas (world) space before
            // converting to local, so camera/canvas transforms don't cause drift.
            var canvasTransform = GetViewport().GetCanvasTransform();
            var globalPos = canvasTransform.AffineInverse() * screenPos.Value;
            var localPos = ToLocal(globalPos);
            var gridPos = GridUtils.LocalToGrid(localPos, _tileSize, _gridOffset);
            OnTileSelected?.Invoke(gridPos);
        }
    }
}
