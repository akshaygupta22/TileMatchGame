using System.Collections.Generic;
using Godot;

namespace TileMatchGame.Gameplay;

/// <summary>
/// Static helpers for finding 3+ matches on the board.
/// </summary>
public static class MatchFinder
{
    /// <summary>
    /// Scans rows and columns for runs of 3+ identical PieceType tiles.
    /// Returns the unique set of matched grid coordinates.
    /// </summary>
    public static List<Vector2I> CheckBoardMatches(Piece[,] grid, int width, int height)
    {
        var matched = new HashSet<Vector2I>();

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
                        for (int i = runStart; i < x; i++)
                        {
                            matched.Add(new Vector2I(i, y));
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
                        for (int i = runStart; i < y; i++)
                        {
                            matched.Add(new Vector2I(x, i));
                        }
                    }
                    runStart = y;
                }
            }
        }

        return new List<Vector2I>(matched);
    }

    /// <summary>
    /// Checks the run length (horizontal or vertical, whichever is longer)
    /// through the given position. Returns true if it is a 4-or-5-in-a-row match.
    /// </summary>
    public static bool IsBigMatch(Piece[,] grid, int width, int height, Vector2I pos, out int runLength)
    {
        runLength = 0;
        var piece = grid[pos.X, pos.Y];
        if (piece == null)
        {
            return false;
        }

        var type = piece.Type;

        int horizontal = 1;
        for (int x = pos.X + 1; x < width && grid[x, pos.Y] != null && grid[x, pos.Y].Type == type; x++) horizontal++;
        for (int x = pos.X - 1; x >= 0 && grid[x, pos.Y] != null && grid[x, pos.Y].Type == type; x--) horizontal++;

        int vertical = 1;
        for (int y = pos.Y + 1; y < height && grid[pos.X, y] != null && grid[pos.X, y].Type == type; y++) vertical++;
        for (int y = pos.Y - 1; y >= 0 && grid[pos.X, y] != null && grid[pos.X, y].Type == type; y--) vertical++;

        runLength = Mathf.Max(horizontal, vertical);
        return runLength >= 4;
    }
}
