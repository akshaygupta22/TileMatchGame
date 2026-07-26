using System;
using Godot;

namespace TileMatchGame.Managers;

/// <summary>
/// Autoload singleton tracking score, target score and moves remaining.
/// </summary>
public partial class GameManager : Node
{
    [Export] public int TargetScore { get; set; } = 1000;
    [Export] public int StartingMoves { get; set; } = 20;

    public int CurrentScore { get; private set; }
    public int MovesRemaining { get; private set; }

    public event Action<int> OnScoreUpdated;
    public event Action<int> OnMovesUpdated;
    public event Action<bool> OnGameOver;
    public event Action<string> OnStatusMessage;

    private bool _gameOver;

    /// <summary>Broadcasts a short human-readable status/feedback message to the UI.</summary>
    public void PostStatus(string message)
    {
        OnStatusMessage?.Invoke(message);
    }

    public override void _Ready()
    {
        MovesRemaining = StartingMoves;
    }

    public void AddScore(int amount)
    {
        if (_gameOver || amount == 0)
        {
            return;
        }

        CurrentScore += amount;
        OnScoreUpdated?.Invoke(CurrentScore);

        if (CurrentScore >= TargetScore)
        {
            EndGame(won: true);
        }
    }

    public void UseMove()
    {
        if (_gameOver)
        {
            return;
        }

        MovesRemaining = Math.Max(0, MovesRemaining - 1);
        OnMovesUpdated?.Invoke(MovesRemaining);

        if (MovesRemaining <= 0 && CurrentScore < TargetScore)
        {
            EndGame(won: false);
        }
    }

    private void EndGame(bool won)
    {
        _gameOver = true;
        OnGameOver?.Invoke(won);
    }
}
