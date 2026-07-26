# TileMatchGame

A match-3 tile puzzle game built with **Godot 4.7** and **C# (.NET 8)**.

## Gameplay

Swap adjacent tiles on an 8x8 grid to form matches of 3 or more same-colored
fruit pieces. Matches are cleared, scored, and the board cascades with
gravity and refills from the top. Pick a game mode from the main menu:

- **Classic:** Reach the target score before you run out of moves. Hitting
  the target advances you to the next tier (higher target, same moves) —
  keep going until you run out of moves.
- **Time Attack:** No move limit — race the clock. Crossing a score
  threshold advances the tier and grants a small time bonus.
- **Endless:** No move limit or timer — play until the board has no possible
  matches left (auto-reshuffles once before ending). Score thresholds
  advance the tier.

Each tier unlocks a harder board (an extra fruit color appears from tier 3
onward). Matching 4 in a row creates a line-clear special piece; matching 5+
creates a color-bomb. Swap a special piece with any neighbor to trigger its
effect.

- **Board:** 8x8 grid, 5 base fruit colors (unlocks a 6th at tier 3)
- **Feedback:** Selected tiles are highlighted, invalid swaps flash red and
  revert, floating score popups and a combo counter appear on cascades, the
  board shakes on big chains, and short synthesized SFX play on
  swap/match/invalid/tier-up/win/lose

## Project Structure

```
Assets/
  Sprites/Fruits/ Fruit tile art
  UI/             GameTheme.tres (theme resource)
Scenes/
  MainMenu.tscn   Entry scene (run/main_scene) — mode select
  Main.tscn       Gameplay scene (Board + UI)
  Board.tscn      Grid/board container
  Piece.tscn      Single tile piece
  FloatingScore.tscn  Floating "+N" popup
  UI.tscn         Score/status/moves/time/tier/combo UI + win-lose popup
Scripts/
  Data/           PieceType, GameMode, SpecialPieceKind enums, TileData resource
  Gameplay/       GridManager, GridInputHandler, MatchFinder, Piece
  Managers/       GameManager (autoload: mode/score/tier/game-over state),
                  AudioManager (autoload: procedural SFX)
  UI/             ScoreUI, MainMenuUI, FloatingScoreLabel
  Utils/          GridUtils, TileDatabase (autoload)
```

Key autoloads (script-only, no scenes): `TileDatabase`, `GameManager`,
`AudioManager`.

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
match are automatically reverted (unless one of the tiles is a special piece,
in which case the swap always triggers its effect).

## Credits

Fruit tile art by [JennPixel](https://jennpixel.itch.io/).
