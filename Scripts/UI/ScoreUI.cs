using Godot;
using TileMatchGame.Data;
using TileMatchGame.Managers;

namespace TileMatchGame.UI;

/// <summary>
/// Binds GameManager events to Score/Moves/Time/Tier labels and a win/lose
/// popup. Expects child nodes marked "Access as Unique Name": %ScoreLabel,
/// %MovesLabel, %TimeLabel, %TierLabel, %ComboLabel, %WinLosePopup,
/// %ResultLabel, %FinalScoreLabel, %TierReachedLabel, %BackToMenuButton,
/// %StatusLabel, %FloatingScoreLayer.
/// </summary>
public partial class ScoreUI : Control
{
    private static readonly PackedScene FloatingScoreScene =
        GD.Load<PackedScene>("res://Scenes/FloatingScore.tscn");

    private Label _scoreLabel;
    private Label _movesLabel;
    private Label _timeLabel;
    private Label _tierLabel;
    private Label _comboLabel;
    private Panel _winLosePopup;
    private Label _resultLabel;
    private Label _finalScoreLabel;
    private Label _tierReachedLabel;
    private Button _backToMenuButton;
    private Label _statusLabel;
    private Node2D _floatingScoreLayer;
    private GameManager _gameManager;
    private SceneTreeTimer _statusClearTimer;
    private SceneTreeTimer _comboClearTimer;

    public override void _Ready()
    {
        _scoreLabel = GetNode<Label>("%ScoreLabel");
        _movesLabel = GetNode<Label>("%MovesLabel");
        _timeLabel = GetNode<Label>("%TimeLabel");
        _tierLabel = GetNode<Label>("%TierLabel");
        _comboLabel = GetNode<Label>("%ComboLabel");
        _winLosePopup = GetNode<Panel>("%WinLosePopup");
        _resultLabel = GetNode<Label>("%ResultLabel");
        _finalScoreLabel = GetNode<Label>("%FinalScoreLabel");
        _tierReachedLabel = GetNode<Label>("%TierReachedLabel");
        _backToMenuButton = GetNode<Button>("%BackToMenuButton");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _floatingScoreLayer = GetNode<Node2D>("%FloatingScoreLayer");
        _winLosePopup.Visible = false;
        _winLosePopup.Modulate = new Color(1, 1, 1, 0);
        _statusLabel.Text = "Click a tile, then click an adjacent tile to swap.";
        _comboLabel.Text = "";
        _backToMenuButton.Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");

        _gameManager = GetNode<GameManager>("/root/GameManager");

        bool isTimeAttack = _gameManager.SelectedMode == GameMode.TimeAttack;
        bool isEndless = _gameManager.SelectedMode == GameMode.Endless;
        _movesLabel.Visible = !isTimeAttack && !isEndless;
        _timeLabel.Visible = isTimeAttack;

        _gameManager.OnScoreUpdated += HandleScoreUpdated;
        _gameManager.OnMovesUpdated += HandleMovesUpdated;
        _gameManager.OnTimeUpdated += HandleTimeUpdated;
        _gameManager.OnTierUpdated += HandleTierUpdated;
        _gameManager.OnGameOver += HandleGameOver;
        _gameManager.OnStatusMessage += HandleStatusMessage;
        _gameManager.OnScorePopup += HandleScorePopup;
        _gameManager.OnComboUpdated += HandleComboUpdated;

        HandleScoreUpdated(_gameManager.CurrentScore);
        HandleMovesUpdated(_gameManager.MovesRemaining);
        HandleTimeUpdated(_gameManager.TimeRemaining);
        HandleTierUpdated(_gameManager.CurrentTier);
    }

    public override void _ExitTree()
    {
        if (_gameManager == null)
        {
            return;
        }

        _gameManager.OnScoreUpdated -= HandleScoreUpdated;
        _gameManager.OnMovesUpdated -= HandleMovesUpdated;
        _gameManager.OnTimeUpdated -= HandleTimeUpdated;
        _gameManager.OnTierUpdated -= HandleTierUpdated;
        _gameManager.OnGameOver -= HandleGameOver;
        _gameManager.OnStatusMessage -= HandleStatusMessage;
        _gameManager.OnScorePopup -= HandleScorePopup;
        _gameManager.OnComboUpdated -= HandleComboUpdated;
    }

    private void HandleScoreUpdated(int score)
    {
        _scoreLabel.Text = _gameManager.SelectedMode == GameMode.Classic
            ? $"Score: {score} / {_gameManager.TargetScore}"
            : $"Score: {score}";
    }

    private void HandleMovesUpdated(int moves)
    {
        _movesLabel.Text = $"Moves: {moves}";
    }

    private void HandleTimeUpdated(float seconds)
    {
        _timeLabel.Text = $"Time: {Mathf.CeilToInt(seconds)}s";
    }

    private void HandleTierUpdated(int tier)
    {
        _tierLabel.Text = $"Tier: {tier}";
    }

    private void HandleGameOver(bool won)
    {
        _resultLabel.Text = won ? "You Win!" : "Game Over";
        _finalScoreLabel.Text = $"Final Score: {_gameManager.CurrentScore}";
        _tierReachedLabel.Text = $"Tier Reached: {_gameManager.CurrentTier}";

        _winLosePopup.Visible = true;
        _winLosePopup.Scale = new Vector2(0.7f, 0.7f);
        _winLosePopup.Modulate = new Color(1, 1, 1, 0);

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(_winLosePopup, "scale", Vector2.One, 0.3)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_winLosePopup, "modulate", Colors.White, 0.25);
    }

    private void HandleStatusMessage(string message)
    {
        _statusLabel.Text = message;

        _statusClearTimer = GetTree().CreateTimer(2.0);
        var timerRef = _statusClearTimer;
        timerRef.Timeout += () =>
        {
            if (_statusClearTimer == timerRef)
            {
                _statusLabel.Text = "";
            }
        };
    }

    private void HandleScorePopup(Vector2 worldPosition, int amount)
    {
        if (FloatingScoreScene == null)
        {
            return;
        }

        var label = FloatingScoreScene.Instantiate<FloatingScoreLabel>();
        _floatingScoreLayer.AddChild(label);
        label.Position = worldPosition;
        label.Play($"+{amount}", new Color(1.0f, 0.85f, 0.2f));
    }

    private void HandleComboUpdated(int chainLength)
    {
        _comboLabel.Text = $"Combo x{chainLength}!";

        _comboClearTimer = GetTree().CreateTimer(1.2);
        var timerRef = _comboClearTimer;
        timerRef.Timeout += () =>
        {
            if (_comboClearTimer == timerRef)
            {
                _comboLabel.Text = "";
            }
        };
    }
}

