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
        var saveManager = GetNodeOrNull<SaveManager>("/root/SaveManager");

        GetNode<Button>("%ClassicButton").Pressed += () => StartGame(gameManager, GameMode.Classic);
        GetNode<Button>("%TimeAttackButton").Pressed += () => StartGame(gameManager, GameMode.TimeAttack);
        GetNode<Button>("%EndlessButton").Pressed += () => StartGame(gameManager, GameMode.Endless);

        saveManager?.Load();
        AddHighScoreSummary(saveManager);
    }

    private void AddHighScoreSummary(SaveManager saveManager)
    {
        var panel = new PanelContainer
        {
            Name = "HighScoreSummary",
            CustomMinimumSize = new Vector2(280f, 120f)
        };

        panel.AnchorLeft = 0.5f;
        panel.AnchorRight = 0.5f;
        panel.AnchorTop = 1.0f;
        panel.AnchorBottom = 1.0f;
        panel.Position = new Vector2(-140f, -140f);

        var margin = new MarginContainer
        {
            CustomMinimumSize = panel.CustomMinimumSize,
            ThemeTypeVariation = "MarginContainer"
        };
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 12);

        var content = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };

        var title = new Label
        {
            Text = "Local Best Scores",
            HorizontalAlignment = HorizontalAlignment.Center,
            CustomMinimumSize = new Vector2(0f, 24f)
        };

        var body = new Label
        {
            Text = BuildHighScoreText(saveManager),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.Word,
            Modulate = new Color(0.95f, 0.95f, 0.95f)
        };

        content.AddChild(title);
        content.AddChild(body);
        margin.AddChild(content);
        panel.AddChild(margin);
        AddChild(panel);
    }

    private string BuildHighScoreText(SaveManager saveManager)
    {
        if (saveManager == null)
        {
            return "No local progress yet.";
        }

        var classic = saveManager.GetHighScore(GameMode.Classic);
        var timeAttack = saveManager.GetHighScore(GameMode.TimeAttack);
        var endless = saveManager.GetHighScore(GameMode.Endless);
        return $"Classic: {classic}\nTime Attack: {timeAttack}\nEndless: {endless}";
    }

    private void StartGame(GameManager gameManager, GameMode mode)
    {
        gameManager.StartNewGame(mode);
        GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
    }
}
