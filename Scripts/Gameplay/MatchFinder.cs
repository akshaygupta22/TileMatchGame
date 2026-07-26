using System.Collections.Generic;
using Godot;
using TileMatchGame.Data;

namespace TileMatchGame.Gameplay;

/// <summary>A special piece that should be spawned at Position instead of being cleared.</summary>
public readonly struct SpecialSpawn
{
    public Vector2I Position { get; }
    public SpecialPieceKind Kind { get; }

    public SpecialSpawn(Vector2I position, SpecialPieceKind kind)
    {
        Position = position;
        Kind = kind;
    }
}

/// <summary>Result of scanning the board for matches: matched cells plus any special spawns.</summary>
public readonly struct MatchScanResult
{
    public List<Vector2I> MatchedCells { get; }
    public List<SpecialSpawn> SpecialSpawns { get; }

    public MatchScanResult(List<Vector2I> matchedCells, List<SpecialSpawn> specialSpawns)
    {
        MatchedCells = matchedCells;
        SpecialSpawns = specialSpawns;
    }
}

/// <summary>
/// Static helpers for finding 3+ matches on the board and detecting when a
/// run of 4+ should spawn a special piece (line-clear or color-bomb).
/// </summary>
public static class MatchFinder
{
    /// <summary>
    /// Scans rows and columns for runs of 3+ identical PieceType tiles.
    /// Returns the unique set of matched grid coordinates.
    /// </summary>
    public static List<Vector2I> CheckBoardMatches(Piece[,] grid, int width, int height)
    {
        return ScanBoard(grid, width, height, null, null).MatchedCells;
    }

    /// <summary>
    /// Scans rows and columns for runs of 3+ identical PieceType tiles, and
    /// additionally reports where special pieces should spawn for runs of 4+
    /// (line-clear) or 5+ (color-bomb). preferredA/preferredB (if given, e.g.
    /// the two just-swapped cells) are preferred as the special's spawn cell
    /// when they fall within the matched run; otherwise the run's middle cell
    /// is used.
    /// </summary>
    public static MatchScanResult ScanBoard(Piece[,] grid, int width, int height, Vector2I? preferredA, Vector2I? preferredB)
    {
        var matched = new HashSet<Vector2I>();
        var spawns = new List<SpecialSpawn>();

        // Horizontal runs
        for (int y = 0; y < height; y++)
        {
            int runStart = 0;
            for (int x = 1; x <= width; x++)
            {
                bool sameAsPrev = x < width &&
                    grid[x, y] != null && grid[x - 1, y] != null &&
                    grid[x, y].Type == grid[x - 1, y].Type;

                if (!sameAsPrev)
                {
                    int runLength = x - runStart;
                    if (runLength >= 3)
                    {
                        var cells = new List<Vector2I>();
                        for (int i = runStart; i < x; i++)
                        {
                            var cell = new Vector2I(i, y);
                            matched.Add(cell);
                            cells.Add(cell);
                        }

                        if (runLength >= 4)
                        {
                            var kind = runLength >= 5 ? SpecialPieceKind.ColorBomb : SpecialPieceKind.LineClearRow;
                            spawns.Add(new SpecialSpawn(PickSpawnPosition(cells, preferredA, preferredB), kind));
                        }
                    }
                    runStart = x;
                }
            }
        }

        // Vertical runs
        for (int x = 0; x < width; x++)
        {
            int runStart = 0;
            for (int y = 1; y <= height; y++)
            {
                bool sameAsPrev = y < height &&
                    grid[x, y] != null && grid[x, y - 1] != null &&
                    grid[x, y].Type == grid[x, y - 1].Type;

                if (!sameAsPrev)
                {
                    int runLength = y - runStart;
                    if (runLength >= 3)
                    {
                        var cells = new List<Vector2I>();
                        for (int i = runStart; i < y; i++)
                        {
                            var cell = new Vector2I(x, i);
                            matched.Add(cell);
                            cells.Add(cell);
                        }

                        if (runLength >= 4)
                        {
                            var kind = runLength >= 5 ? SpecialPieceKind.ColorBomb : SpecialPieceKind.LineClearCol;
                            spawns.Add(new SpecialSpawn(PickSpawnPosition(cells, preferredA, preferredB), kind));
                        }
                    }
                    runStart = y;
                }
            }
        }

        return new MatchScanResult(new List<Vector2I>(matched), spawns);
    }

    private static Vector2I PickSpawnPosition(List<Vector2I> cells, Vector2I? preferredA, Vector2I? preferredB)
    {
        if (preferredA.HasValue && cells.Contains(preferredA.Value))
        {
            return preferredA.Value;
        }

        if (preferredB.HasValue && cells.Contains(preferredB.Value))
        {
            return preferredB.Value;
        }

        return cells[cells.Count / 2];
    }

    /// <summary>
    /// Brute-force scan (board is tiny, ~8x8, so this is cheap): true if any
    /// adjacent swap on the board would create a 3+ match. Used by Endless
    /// mode to detect a stuck board.
    /// </summary>
    public static bool HasAnyPossibleMove(Piece[,] grid, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null)
                {
                    continue;
                }

                if (x + 1 < width && WouldSwapMatch(grid, width, height, new Vector2I(x, y), new Vector2I(x + 1, y)))
                {
                    return true;
                }

                if (y + 1 < height && WouldSwapMatch(grid, width, height, new Vector2I(x, y), new Vector2I(x, y + 1)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool WouldSwapMatch(Piece[,] grid, int width, int height, Vector2I a, Vector2I b)
    {
        if (grid[a.X, a.Y] == null || grid[b.X, b.Y] == null)
        {
            return false;
        }

        (grid[a.X, a.Y], grid[b.X, b.Y]) = (grid[b.X, b.Y], grid[a.X, a.Y]);
        bool hasMatch = ScanBoard(grid, width, height, null, null).MatchedCells.Count > 0;
        (grid[a.X, a.Y], grid[b.X, b.Y]) = (grid[b.X, b.Y], grid[a.X, a.Y]);

        return hasMatch;
    }
}

