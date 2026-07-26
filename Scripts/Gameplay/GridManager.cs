using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using TileMatchGame.Data;
using TileMatchGame.Managers;
using TileMatchGame.Utils;

namespace TileMatchGame.Gameplay;

/// <summary>
/// Owns the board state (Piece[,] grid), handles tile swapping with tween
/// animations, match resolution, gravity and refill cascades.
/// </summary>
public partial class GridManager : Node2D
{
    [Export] public int Width { get; set; } = 8;
    [Export] public int Height { get; set; } = 8;
    [Export] public int TileSize { get; set; } = 64;
    [Export] public Vector2 GridOffset { get; set; } = Vector2.Zero;
    [Export] public PackedScene PieceScene { get; set; }
    [Export] public NodePath InputHandlerPath { get; set; }

    private const float SwapDuration = 0.15f;

    private Piece[,] _grid;
    private GridInputHandler _inputHandler;
    private TileDatabase _tileDatabase;
    private GameManager _gameManager;
    private Vector2I? _selectedTile;
    private bool _isBusy;
    private readonly Random _random = new();

    public override void _Ready()
    {
        _tileDatabase = GetNode<TileDatabase>("/root/TileDatabase");
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _grid = new Piece[Width, Height];

        _inputHandler = InputHandlerPath != null && !InputHandlerPath.IsEmpty
            ? GetNode<GridInputHandler>(InputHandlerPath)
            : GetNodeOrNull<GridInputHandler>("GridInputHandler");

        if (_inputHandler != null)
        {
            _inputHandler.Configure(TileSize, GridOffset);
            _inputHandler.OnTileSelected += HandleTileSelected;
        }

        GenerateInitialBoard();
    }

    private void GenerateInitialBoard()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                PieceType type;
                do
                {
                    type = RandomPieceType();
                } while (WouldCreateMatchAt(x, y, type));

                _grid[x, y] = SpawnPiece(new Vector2I(x, y), type);
            }
        }
    }

    private bool WouldCreateMatchAt(int x, int y, PieceType type)
    {
        if (x >= 2 && _grid[x - 1, y] != null && _grid[x - 2, y] != null &&
            _grid[x - 1, y].Type == type && _grid[x - 2, y].Type == type)
        {
            return true;
        }

        if (y >= 2 && _grid[x, y - 1] != null && _grid[x, y - 2] != null &&
            _grid[x, y - 1].Type == type && _grid[x, y - 2].Type == type)
        {
            return true;
        }

        return false;
    }

    private PieceType RandomPieceType()
    {
        var values = Enum.GetValues<PieceType>();
        return values[_random.Next(values.Length)];
    }

    private Piece SpawnPiece(Vector2I gridPos, PieceType type)
    {
        var piece = PieceScene.Instantiate<Piece>();
        AddChild(piece);
        piece.Setup(type, _tileDatabase.GetTileData(type));
        piece.GridPosition = gridPos;
        piece.Position = GridUtils.GridToLocal(gridPos, TileSize, GridOffset);
        return piece;
    }

    private void HandleTileSelected(Vector2I gridPos)
    {
        if (_isBusy)
        {
            _gameManager?.PostStatus("Hold on, board is still moving...");
            return;
        }

        if (!IsInBounds(gridPos) || _grid[gridPos.X, gridPos.Y] == null)
        {
            return;
        }

        if (_selectedTile == null)
        {
            SelectTile(gridPos);
            _gameManager?.PostStatus("Tile selected — click an adjacent tile to swap.");
            return;
        }

        var first = _selectedTile.Value;

        if (first == gridPos)
        {
            DeselectCurrent();
            _gameManager?.PostStatus("Deselected.");
            return;
        }

        if (IsAdjacent(first, gridPos))
        {
            DeselectCurrent();
            _ = TrySwapAsync(first, gridPos);
        }
        else
        {
            DeselectCurrent();
            SelectTile(gridPos);
            _gameManager?.PostStatus("Not adjacent — selected this tile instead.");
        }
    }

    private void SelectTile(Vector2I pos)
    {
        _selectedTile = pos;
        _grid[pos.X, pos.Y]?.SetSelected(true);
    }

    private void DeselectCurrent()
    {
        if (_selectedTile is { } pos)
        {
            _grid[pos.X, pos.Y]?.SetSelected(false);
        }
        _selectedTile = null;
    }

    private static bool IsAdjacent(Vector2I a, Vector2I b)
    {
        var diff = (a - b).Abs();
        return diff == Vector2I.Right || diff == Vector2I.Down;
    }

    private bool IsInBounds(Vector2I pos)
    {
        return pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;
    }

    private async Task TrySwapAsync(Vector2I posA, Vector2I posB)
    {
        _isBusy = true;
        try
        {
            await SwapTiles(posA, posB);
        }
        catch (Exception ex)
        {
            GD.PushError($"GridManager swap failed: {ex}");
        }
        finally
        {
            _isBusy = false;
        }
    }

    /// <summary>
    /// Swaps two adjacent tiles, tweening both into place. If the swap does not
    /// create a match, they are tweened back to their original positions.
    /// </summary>
    public async Task SwapTiles(Vector2I posA, Vector2I posB)
    {
        var pieceA = _grid[posA.X, posA.Y];
        var pieceB = _grid[posB.X, posB.Y];

        if (pieceA == null || pieceB == null)
        {
            return;
        }

        await AnimateSwap(pieceA, pieceB, posB, posA);

        _grid[posA.X, posA.Y] = pieceB;
        _grid[posB.X, posB.Y] = pieceA;
        pieceA.GridPosition = posB;
        pieceB.GridPosition = posA;

        var matches = MatchFinder.CheckBoardMatches(_grid, Width, Height);

        if (matches.Count == 0)
        {
            // No match: flash red and reverse the swap.
            pieceA.FlashInvalid();
            pieceB.FlashInvalid();
            _gameManager?.PostStatus("No match — swap reverted.");
            await AnimateSwap(pieceA, pieceB, posA, posB);
            _grid[posA.X, posA.Y] = pieceA;
            _grid[posB.X, posB.Y] = pieceB;
            pieceA.GridPosition = posA;
            pieceB.GridPosition = posB;
            return;
        }

        _gameManager?.UseMove();
        _gameManager?.PostStatus("Match found!");
        await ResolveMatchesAsync(matches);
    }

    private async Task AnimateSwap(Piece pieceA, Piece pieceB, Vector2I targetForA, Vector2I targetForB)
    {
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(pieceA, "position", GridUtils.GridToLocal(targetForA, TileSize, GridOffset), SwapDuration);
        tween.TweenProperty(pieceB, "position", GridUtils.GridToLocal(targetForB, TileSize, GridOffset), SwapDuration);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task ResolveMatchesAsync(List<Vector2I> matches)
    {
        ClearMatchedPieces(matches);
        await ApplyGravityAndRefillAsync();
    }

    private void ClearMatchedPieces(List<Vector2I> matches)
    {
        int scoreGained = 0;

        foreach (var pos in matches)
        {
            var piece = _grid[pos.X, pos.Y];
            if (piece == null)
            {
                continue;
            }

            scoreGained += piece.Data?.ScoreValue ?? 10;
            _grid[pos.X, pos.Y] = null;
            piece.QueueFree();
        }

        if (scoreGained > 0)
        {
            _gameManager?.AddScore(scoreGained);
            _gameManager?.PostStatus($"+{scoreGained} points!");
        }
    }

    /// <summary>
    /// Shifts remaining pieces down into empty slots, spawns new pieces above
    /// the board for empty top slots, and loops until no new matches exist.
    /// </summary>
    public async Task ApplyGravityAndRefillAsync()
    {
        bool cascaded = true;

        while (cascaded)
        {
            await CollapseAndRefillColumnsAsync();

            var newMatches = MatchFinder.CheckBoardMatches(_grid, Width, Height);
            if (newMatches.Count > 0)
            {
                ClearMatchedPieces(newMatches);
                cascaded = true;
            }
            else
            {
                cascaded = false;
            }
        }
    }

    private async Task CollapseAndRefillColumnsAsync()
    {
        var tween = CreateTween();
        tween.SetParallel(true);
        bool anyTween = false;

        for (int x = 0; x < Width; x++)
        {
            int writeY = Height - 1;

            for (int y = Height - 1; y >= 0; y--)
            {
                if (_grid[x, y] == null)
                {
                    continue;
                }

                var piece = _grid[x, y];

                if (writeY != y)
                {
                    _grid[x, writeY] = piece;
                    _grid[x, y] = null;
                    piece.GridPosition = new Vector2I(x, writeY);
                    tween.TweenProperty(piece, "position", GridUtils.GridToLocal(piece.GridPosition, TileSize, GridOffset), SwapDuration);
                    anyTween = true;
                }

                writeY--;
            }

            for (int y = writeY; y >= 0; y--)
            {
                var type = RandomPieceType();
                var spawnPos = new Vector2I(x, y);
                var piece = SpawnPiece(spawnPos, type);
                piece.Position = GridUtils.GridToLocal(new Vector2I(x, y - Height), TileSize, GridOffset);
                _grid[x, y] = piece;
                tween.TweenProperty(piece, "position", GridUtils.GridToLocal(spawnPos, TileSize, GridOffset), SwapDuration);
                anyTween = true;
            }
        }

        if (anyTween)
        {
            await ToSignal(tween, Tween.SignalName.Finished);
        }
        else
        {
            tween.Kill();
        }
    }
}
