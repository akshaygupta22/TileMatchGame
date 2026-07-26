using Godot;
using TileMatchGame.Data;
using TileMatchGame.Managers;

namespace TileMatchGame.UI;

/// <summary>
/// Main menu: lets the player pick a GameMode, resets GameManager state for
/// a fresh run, and transitions to the Main gameplay scene.
/// </summary>
public partial class MainMenuUI : Control
{
    public override void _Ready()
    {
        var gameManager = GetNode<GameManager>("/root/GameManager");

        GetNode<Button>("%ClassicButton").Pressed += () => StartGame(gameManager, GameMode.Classic);
        GetNode<Button>("%TimeAttackButton").Pressed += () => StartGame(gameManager, GameMode.TimeAttack);
        GetNode<Button>("%EndlessButton").Pressed += () => StartGame(gameManager, GameMode.Endless);
    }

    private void StartGame(GameManager gameManager, GameMode mode)
    {
        gameManager.StartNewGame(mode);
        GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
    }
}
