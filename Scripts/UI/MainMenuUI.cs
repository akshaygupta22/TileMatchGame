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
    private GameManager _gameManager;
    private SaveManager _saveManager;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _saveManager = GetNodeOrNull<SaveManager>("/root/SaveManager");

        GetNode<Button>("%ClassicButton").Pressed += () => StartGame(GameMode.Classic);
        GetNode<Button>("%TimeAttackButton").Pressed += () => StartGame(GameMode.TimeAttack);
        GetNode<Button>("%EndlessButton").Pressed += () => StartGame(GameMode.Endless);
        GetNode<Button>("%ResetProgressButton").Pressed += ResetProgress;

        _saveManager?.Load();
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var summary = GetNodeOrNull<Label>("%SummaryLabel");
        if (summary == null)
        {
            return;
        }

        summary.Text = BuildSummaryText();
    }

    private string BuildSummaryText()
    {
        if (_saveManager == null)
        {
            return "No local progress yet.\nPlay a run to create your first save.";
        }

        var classic = _saveManager.GetHighScore(GameMode.Classic);
        var timeAttack = _saveManager.GetHighScore(GameMode.TimeAttack);
        var endless = _saveManager.GetHighScore(GameMode.Endless);
        var gamesPlayed = _saveManager.SaveData.TotalGamesPlayed;
        var wins = _saveManager.SaveData.TotalWins;
        var highestTier = _saveManager.SaveData.HighestTier;
        var tutorialSeen = _saveManager.SaveData.TutorialSeen ? "Tutorial ready" : "Tutorial pending";

        return $"Best scores\nClassic: {classic}\nTime Attack: {timeAttack}\nEndless: {endless}\n\nRuns: {gamesPlayed} | Wins: {wins}\nHighest tier: {highestTier}\n{tutorialSeen}";
    }

    private void ResetProgress()
    {
        if (_saveManager == null)
        {
            return;
        }

        _saveManager.ResetProgress();
        UpdateSummary();
    }

    private void StartGame(GameMode mode)
    {
        _gameManager.StartNewGame(mode);
        GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
    }
}
