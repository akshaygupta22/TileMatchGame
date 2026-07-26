# TileMatchGame

A match-3 tile puzzle game built with **Godot 4.7** and **C# (.NET 8)**.

## Gameplay

Swap adjacent tiles on an 8x8 grid to form matches of 3 or more same-colored
pieces. Matches are cleared, scored, and the board cascades with gravity and
refills from the top. Reach the target score before you run out of moves to
win.

- **Board:** 8x8 grid, 5 piece colors (Red, Blue, Green, Yellow, Purple)
- **Goal:** Reach a target score (default 1000) within a limited number of
  moves (default 20)
- **Feedback:** Selected tiles are highlighted, invalid swaps flash red and
  revert, and a status bar shows one-line messages for each action

## Project Structure

```
Scenes/
  Main.tscn       Entry scene (run/main_scene)
  Board.tscn      Grid/board container
  Piece.tscn      Single tile piece
  UI.tscn         Score/status/moves UI
Scripts/
  Data/           PieceType enum, TileData resource
  Gameplay/       GridManager, GridInputHandler, MatchFinder, Piece
  Managers/       GameManager (autoload: score, moves, game-over state)
  UI/             ScoreUI
  Utils/          GridUtils, TileDatabase (autoload)
```

Key autoloads (script-only, no scenes): `TileDatabase`, `GameManager`.

## Requirements

- [Godot 4.7](https://godotengine.org/) (.NET/C# build)
- [.NET 8 SDK](https://dotnet.microsoft.com/download)

## Building & Running

Open the project in the Godot editor and press **Run**, or build the C#
project directly from the repo root:

```powershell
dotnet build
```

To play, open `project.godot` in Godot and run the project (F5).

## Controls

Click a tile, then click an adjacent tile to swap. Swaps that don't produce a
match are automatically reverted.