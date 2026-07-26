using Godot;
using TileMatchGame.Managers;

namespace TileMatchGame.UI;

/// <summary>
/// Binds GameManager events to Score/Moves labels and a win/lose popup.
/// Expects child nodes marked "Access as Unique Name": %ScoreLabel,
/// %MovesLabel, %WinLosePopup, %ResultLabel, %StatusLabel.
/// </summary>
public partial class ScoreUI : Control
{
    private Label _scoreLabel;
    private Label _movesLabel;
    private Panel _winLosePopup;
    private Label _resultLabel;
    private Label _statusLabel;
    private GameManager _gameManager;
    private SceneTreeTimer _statusClearTimer;

    public override void _Ready()
    {
        _scoreLabel = GetNode<Label>("%ScoreLabel");
        _movesLabel = GetNode<Label>("%MovesLabel");
        _winLosePopup = GetNode<Panel>("%WinLosePopup");
        _resultLabel = GetNode<Label>("%ResultLabel");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _winLosePopup.Visible = false;
        _statusLabel.Text = "Click a tile, then click an adjacent tile to swap.";

        _gameManager = GetNode<GameManager>("/root/GameManager");
        _gameManager.OnScoreUpdated += HandleScoreUpdated;
        _gameManager.OnMovesUpdated += HandleMovesUpdated;
        _gameManager.OnGameOver += HandleGameOver;
        _gameManager.OnStatusMessage += HandleStatusMessage;

        HandleScoreUpdated(_gameManager.CurrentScore);
        HandleMovesUpdated(_gameManager.MovesRemaining);
    }

    public override void _ExitTree()
    {
        if (_gameManager == null)
        {
            return;
        }

        _gameManager.OnScoreUpdated -= HandleScoreUpdated;
        _gameManager.OnMovesUpdated -= HandleMovesUpdated;
        _gameManager.OnGameOver -= HandleGameOver;
        _gameManager.OnStatusMessage -= HandleStatusMessage;
    }

    private void HandleScoreUpdated(int score)
    {
        _scoreLabel.Text = $"Score: {score} / {_gameManager.TargetScore}";
    }

    private void HandleMovesUpdated(int moves)
    {
        _movesLabel.Text = $"Moves: {moves}";
    }

    private void HandleGameOver(bool won)
    {
        _resultLabel.Text = won ? "You Win!" : "Game Over";
        _winLosePopup.Visible = true;
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
}
