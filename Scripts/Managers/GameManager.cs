using System;
using Godot;
using TileMatchGame.Data;

namespace TileMatchGame.Managers;

/// <summary>
/// Autoload singleton tracking score, moves/time, and difficulty tier for the
/// currently selected GameMode. Persists across scene changes (Main Menu ->
/// Main), so SelectedMode/score/etc. are reset explicitly via StartNewGame()
/// rather than relying on _Ready (which only runs once at engine start).
/// </summary>
public partial class GameManager : Node
{
    private const string BalancePath = "res://Config/game_balance.json";

    public GameBalanceData BalanceData { get; private set; } = new();
    public int BaseTargetScore { get; private set; }
    public int StartingMoves { get; private set; }
    public float TimeAttackSeconds { get; private set; }
    public float TierScoreStep { get; private set; }
    public float TierTimeBonusSeconds { get; private set; }
    public float TierTargetMultiplier { get; private set; }

    public GameMode SelectedMode { get; private set; } = GameMode.Classic;
    public int CurrentScore { get; private set; }
    public int MovesRemaining { get; private set; }
    public int CurrentTier { get; private set; } = 1;
    public int TargetScore { get; private set; }
    public float TimeRemaining { get; private set; }

    public event Action<int> OnScoreUpdated;
    public event Action<int> OnMovesUpdated;
    public event Action<float> OnTimeUpdated;
    public event Action<int> OnTierUpdated;
    public event Action<bool> OnGameOver;
    public event Action<string> OnStatusMessage;
    public event Action<Vector2, int> OnScorePopup;
    public event Action<int> OnComboUpdated;
    public event Action OnSwapSound;
    public event Action OnInvalidSound;

    private SaveManager _saveManager;
    private bool _gameOver;
    private int _tierScoreBaseline;

    public override void _Ready()
    {
        LoadBalanceData();
        _saveManager = GetNodeOrNull<SaveManager>("/root/SaveManager");
        _saveManager?.Load();
        StartNewGame(SelectedMode);
    }

    public override void _Process(double delta)
    {
        if (_gameOver || SelectedMode != GameMode.TimeAttack)
        {
            return;
        }

        TimeRemaining = Mathf.Max(0f, TimeRemaining - (float)delta);
        OnTimeUpdated?.Invoke(TimeRemaining);

        if (TimeRemaining <= 0f)
        {
            EndGame(won: false);
        }
    }

    private void LoadBalanceData()
    {
        var resolvedPath = ProjectSettings.GlobalizePath(BalancePath);
        BalanceData = GameBalanceData.LoadFromFile(resolvedPath);
        BaseTargetScore = BalanceData.BaseTargetScore;
        StartingMoves = BalanceData.StartingMoves;
        TimeAttackSeconds = BalanceData.TimeAttackSeconds;
        TierScoreStep = BalanceData.TierScoreStep;
        TierTimeBonusSeconds = BalanceData.TierTimeBonusSeconds;
        TierTargetMultiplier = BalanceData.TierTargetMultiplier;
    }

    /// <summary>Resets all run state for a fresh playthrough of the given mode. Call before changing to the Main scene.</summary>
    public void StartNewGame(GameMode mode)
    {
        SelectedMode = mode;
        CurrentScore = 0;
        CurrentTier = 1;
        _tierScoreBaseline = 0;
        TargetScore = BaseTargetScore;
        MovesRemaining = StartingMoves;
        TimeRemaining = TimeAttackSeconds;
        _gameOver = false;
    }

    /// <summary>Broadcasts a short human-readable status/feedback message to the UI.</summary>
    public void PostStatus(string message)
    {
        OnStatusMessage?.Invoke(message);
    }

    /// <summary>Broadcasts a floating "+N" popup to spawn at a world position.</summary>
    public void PostScorePopup(Vector2 worldPosition, int amount)
    {
        OnScorePopup?.Invoke(worldPosition, amount);
    }

    /// <summary>Broadcasts the current cascade chain length (2+ means a combo).</summary>
    public void PostCombo(int chainLength)
    {
        OnComboUpdated?.Invoke(chainLength);
    }

    /// <summary>Broadcasts that a tile swap was attempted (for the swap SFX).</summary>
    public void PostSwapSound()
    {
        OnSwapSound?.Invoke();
    }

    /// <summary>Broadcasts that a swap was reverted (for the invalid-swap SFX).</summary>
    public void PostInvalidSound()
    {
        OnInvalidSound?.Invoke();
    }

    public void AddScore(int amount)
    {
        if (_gameOver || amount == 0)
        {
            return;
        }

        CurrentScore += amount;
        OnScoreUpdated?.Invoke(CurrentScore);
        CheckTierUp();
    }

    private void CheckTierUp()
    {
        if (SelectedMode == GameMode.Classic)
        {
            if (CurrentScore < TargetScore)
            {
                return;
            }

            CurrentTier++;
            TargetScore = Mathf.RoundToInt(TargetScore * TierTargetMultiplier);
            OnTierUpdated?.Invoke(CurrentTier);
            PostStatus($"Tier {CurrentTier} reached! New target: {TargetScore}");
            return;
        }

        // TimeAttack / Endless: tier up every TierScoreStep points scored.
        if (CurrentScore - _tierScoreBaseline < TierScoreStep)
        {
            return;
        }

        _tierScoreBaseline = CurrentScore;
        CurrentTier++;
        OnTierUpdated?.Invoke(CurrentTier);

        if (SelectedMode == GameMode.TimeAttack)
        {
            TimeRemaining += TierTimeBonusSeconds;
            OnTimeUpdated?.Invoke(TimeRemaining);
            PostStatus($"Tier {CurrentTier}! +{TierTimeBonusSeconds:0}s bonus.");
        }
        else
        {
            PostStatus($"Tier {CurrentTier}! Board getting harder.");
        }
    }

    public void UseMove()
    {
        if (_gameOver || SelectedMode != GameMode.Classic)
        {
            // Time Attack and Endless have no move limit (time/board-stuck end them instead).
            return;
        }

        MovesRemaining = Math.Max(0, MovesRemaining - 1);
        OnMovesUpdated?.Invoke(MovesRemaining);

        if (MovesRemaining <= 0)
        {
            EndGame(won: false);
        }
    }

    /// <summary>Called by GridManager when a board scan finds zero possible moves (after a reshuffle attempt).</summary>
    public void ReportBoardStuck()
    {
        if (_gameOver)
        {
            return;
        }

        PostStatus("No moves left on the board!");
        EndGame(won: false);
    }

    private void EndGame(bool won)
    {
        _gameOver = true;
        _saveManager?.RecordRunResult(SelectedMode, CurrentScore, CurrentTier, won, StartingMoves - MovesRemaining, TimeRemaining);
        OnGameOver?.Invoke(won);
    }
}

