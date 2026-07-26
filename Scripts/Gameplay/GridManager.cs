using System;
using System.Collections.Generic;
using System.Linq;
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
        var values = TileDatabase.GetActiveTypes(_gameManager?.CurrentTier ?? 1);
        return values[_random.Next(values.Count)];
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
    /// Swaps two adjacent tiles, tweening both into place. If either tile is a
    /// special piece, its effect triggers unconditionally. Otherwise, if the
    /// swap does not create a match, both tiles are tweened back to their
    /// original positions.
    /// </summary>
    public async Task SwapTiles(Vector2I posA, Vector2I posB)
    {
        var pieceA = _grid[posA.X, posA.Y];
        var pieceB = _grid[posB.X, posB.Y];

        if (pieceA == null || pieceB == null)
        {
            return;
        }

        _gameManager?.PostSwapSound();
        await AnimateSwap(pieceA, pieceB, posB, posA);

        _grid[posA.X, posA.Y] = pieceB;
        _grid[posB.X, posB.Y] = pieceA;
        pieceA.GridPosition = posB;
        pieceB.GridPosition = posA;

        if (pieceA.SpecialKind != SpecialPieceKind.None || pieceB.SpecialKind != SpecialPieceKind.None)
        {
            _gameManager?.UseMove();
            _gameManager?.PostStatus("Special piece activated!");
            await TriggerSpecialEffectsAsync(pieceA, pieceB);
            return;
        }

        var scan = MatchFinder.ScanBoard(_grid, Width, Height, posA, posB);

        if (scan.MatchedCells.Count == 0)
        {
            // No match: flash red and reverse the swap.
            pieceA.FlashInvalid();
            pieceB.FlashInvalid();
            _gameManager?.PostStatus("No match — swap reverted.");
            _gameManager?.PostInvalidSound();
            await AnimateSwap(pieceA, pieceB, posA, posB);
            _grid[posA.X, posA.Y] = pieceA;
            _grid[posB.X, posB.Y] = pieceB;
            pieceA.GridPosition = posA;
            pieceB.GridPosition = posB;
            return;
        }

        _gameManager?.UseMove();
        _gameManager?.PostStatus("Match found!");
        await ResolveMatchesAsync(scan.MatchedCells, scan.SpecialSpawns);
    }

    /// <summary>
    /// Resolves the effect(s) of one or two special pieces involved in a swap:
    /// clears the affected row/column/color, awards a bonus-scored clear, then
    /// cascades normally afterward.
    /// </summary>
    private async Task TriggerSpecialEffectsAsync(Piece pieceA, Piece pieceB)
    {
        var cells = new HashSet<Vector2I>();

        if (pieceA.SpecialKind != SpecialPieceKind.None)
        {
            cells.UnionWith(GetSpecialEffectCells(pieceA.GridPosition, pieceA.SpecialKind, pieceB.Type));
        }

        if (pieceB.SpecialKind != SpecialPieceKind.None)
        {
            cells.UnionWith(GetSpecialEffectCells(pieceB.GridPosition, pieceB.SpecialKind, pieceA.Type));
        }

        await ShakeBoardAsync();
        ClearMatchedPieces(cells.ToList(), chainIndex: 1, scoreMultiplier: 2, specialSpawns: null);
        await ApplyGravityAndRefillAsync();
    }

    /// <summary>Returns every cell a special piece's effect should clear.</summary>
    private HashSet<Vector2I> GetSpecialEffectCells(Vector2I pos, SpecialPieceKind kind, PieceType partnerType)
    {
        var cells = new HashSet<Vector2I> { pos };

        switch (kind)
        {
            case SpecialPieceKind.LineClearRow:
                for (int x = 0; x < Width; x++)
                {
                    cells.Add(new Vector2I(x, pos.Y));
                }
                break;
            case SpecialPieceKind.LineClearCol:
                for (int y = 0; y < Height; y++)
                {
                    cells.Add(new Vector2I(pos.X, y));
                }
                break;
            case SpecialPieceKind.ColorBomb:
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (_grid[x, y] != null && _grid[x, y].Type == partnerType)
                        {
                            cells.Add(new Vector2I(x, y));
                        }
                    }
                }
                break;
        }

        return cells;
    }

    /// <summary>Briefly jitters the board's position to sell big combos/effects.</summary>
    private async Task ShakeBoardAsync()
    {
        var origin = Position;
        var tween = CreateTween();
        for (int i = 0; i < 4; i++)
        {
            var offset = new Vector2(_random.Next(-6, 7), _random.Next(-6, 7));
            tween.TweenProperty(this, "position", origin + offset, 0.04);
        }
        tween.TweenProperty(this, "position", origin, 0.04);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task AnimateSwap(Piece pieceA, Piece pieceB, Vector2I targetForA, Vector2I targetForB)
    {
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(pieceA, "position", GridUtils.GridToLocal(targetForA, TileSize, GridOffset), SwapDuration);
        tween.TweenProperty(pieceB, "position", GridUtils.GridToLocal(targetForB, TileSize, GridOffset), SwapDuration);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task ResolveMatchesAsync(List<Vector2I> matches, List<SpecialSpawn> specialSpawns)
    {
        ClearMatchedPieces(matches, chainIndex: 1, scoreMultiplier: 1, specialSpawns: specialSpawns);
        await ApplyGravityAndRefillAsync();
    }

    private void ClearMatchedPieces(List<Vector2I> cellsToClear, int chainIndex, int scoreMultiplier = 1, List<SpecialSpawn> specialSpawns = null)
    {
        int scoreGained = 0;
        Vector2 centroid = Vector2.Zero;
        int count = 0;
        var spawnPositions = specialSpawns != null
            ? new HashSet<Vector2I>(specialSpawns.Select(s => s.Position))
            : null;

        foreach (var pos in cellsToClear)
        {
            var piece = _grid[pos.X, pos.Y];
            if (piece == null)
            {
                continue;
            }

            centroid += piece.Position;
            count++;

            if (spawnPositions != null && spawnPositions.Contains(pos))
            {
                // This cell becomes a special piece instead of being cleared.
                continue;
            }

            scoreGained += (piece.Data?.ScoreValue ?? 10) * scoreMultiplier;
            _grid[pos.X, pos.Y] = null;
            piece.PlayClearEffect(this);
            piece.QueueFree();
        }

        if (specialSpawns != null)
        {
            foreach (var spawn in specialSpawns)
            {
                _grid[spawn.Position.X, spawn.Position.Y]?.SetSpecialKind(spawn.Kind);
            }
        }

        if (scoreGained > 0)
        {
            _gameManager?.AddScore(scoreGained);
            _gameManager?.PostStatus($"+{scoreGained} points!");
            _gameManager?.PostScorePopup(centroid / Math.Max(1, count), scoreGained);

            if (chainIndex > 1)
            {
                _gameManager?.PostCombo(chainIndex);
            }
        }
    }

    /// <summary>
    /// Shifts remaining pieces down into empty slots, spawns new pieces above
    /// the board for empty top slots, and loops until no new matches exist.
    /// Tracks cascade chain depth for combo feedback and shakes the board on
    /// long chains.
    /// </summary>
    public async Task ApplyGravityAndRefillAsync()
    {
        bool cascaded = true;
        int chainIndex = 1;

        while (cascaded)
        {
            await CollapseAndRefillColumnsAsync();

            var scan = MatchFinder.ScanBoard(_grid, Width, Height, null, null);
            if (scan.MatchedCells.Count > 0)
            {
                chainIndex++;
                ClearMatchedPieces(scan.MatchedCells, chainIndex, scoreMultiplier: 1, specialSpawns: scan.SpecialSpawns);
                cascaded = true;
            }
            else
            {
                cascaded = false;
            }
        }

        if (chainIndex >= 3)
        {
            await ShakeBoardAsync();
        }

        EnsureBoardIsPlayable();
    }

    /// <summary>
    /// Safety net: if no swap on the board could produce a match, reshuffle
    /// tile types in place (up to a few attempts). If still stuck afterward,
    /// reports the board as stuck (ends the current game).
    /// </summary>
    private void EnsureBoardIsPlayable()
    {
        if (MatchFinder.HasAnyPossibleMove(_grid, Width, Height))
        {
            return;
        }

        for (int attempt = 0; attempt < 20 && !MatchFinder.HasAnyPossibleMove(_grid, Width, Height); attempt++)
        {
            ReshuffleBoard();
        }

        if (!MatchFinder.HasAnyPossibleMove(_grid, Width, Height))
        {
            _gameManager?.ReportBoardStuck();
        }
        else
        {
            _gameManager?.PostStatus("Board reshuffled \u2014 no moves were available.");
        }
    }

    private void ReshuffleBoard()
    {
        var types = new List<PieceType>();
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (_grid[x, y] != null)
                {
                    types.Add(_grid[x, y].Type);
                }
            }
        }

        // Fisher-Yates shuffle.
        for (int i = types.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (types[i], types[j]) = (types[j], types[i]);
        }

        int idx = 0;
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (_grid[x, y] == null)
                {
                    continue;
                }

                var type = types[idx++];
                _grid[x, y].Setup(type, _tileDatabase.GetTileData(type));
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
