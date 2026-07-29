using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace TileMatchGame.Managers;

public sealed class GameBalanceData
{
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;
    public int BaseTargetScore { get; set; } = 1000;
    public int StartingMoves { get; set; } = 20;
    public float TimeAttackSeconds { get; set; } = 90f;
    public float TierScoreStep { get; set; } = 500f;
    public float TierTimeBonusSeconds { get; set; } = 10f;
    public float TierTargetMultiplier { get; set; } = 1.5f;

    public static GameBalanceData LoadFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new GameBalanceData();
        }

        try
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<GameBalanceData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return data ?? new GameBalanceData();
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Failed to load balance data from {path}: {ex.Message}");
            return new GameBalanceData();
        }
    }

    public void SaveToFile(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
