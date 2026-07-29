using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TileMatchGame.Data;

namespace TileMatchGame.Managers;

public sealed class SaveStore
{
    private const string DefaultFileName = "save_data.json";
    private string _storagePath;

    public SaveData SaveData { get; private set; } = new();

    public SaveStore(string storagePath = null)
    {
        _storagePath = storagePath;
    }

    public void SetStoragePath(string path)
    {
        _storagePath = path;
    }

    public void Load()
    {
        var targetPath = ResolveStoragePath();
        if (!File.Exists(targetPath))
        {
            SaveData = new SaveData();
            return;
        }

        try
        {
            var json = File.ReadAllText(targetPath);
            var loaded = JsonSerializer.Deserialize<SaveData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            SaveData = loaded ?? new SaveData();
            EnsureDefaults();
        }
        catch (Exception)
        {
            SaveData = new SaveData();
            Save();
        }
    }

    public void Save()
    {
        var targetPath = ResolveStoragePath();
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(SaveData, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(targetPath, json);
    }

    public void RecordRunResult(GameMode mode, int score, int tier, bool won, int movesUsed, float timeRemaining)
    {
        EnsureDefaults();
        var key = mode.ToString();
        if (!SaveData.HighScores.ContainsKey(key))
        {
            SaveData.HighScores[key] = 0;
        }

        if (score > SaveData.HighScores[key])
        {
            SaveData.HighScores[key] = score;
        }

        SaveData.HighestTier = Math.Max(SaveData.HighestTier, tier);
        SaveData.TotalGamesPlayed++;
        if (won)
        {
            SaveData.TotalWins++;
        }

        SaveData.TutorialSeen = true;
        Save();
    }

    public int GetHighScore(GameMode mode)
    {
        EnsureDefaults();
        var key = mode.ToString();
        return SaveData.HighScores.TryGetValue(key, out var value) ? value : 0;
    }

    public void SetSetting(string key, object value)
    {
        EnsureDefaults();
        SaveData.Settings[key] = value;
        Save();
    }

    public object GetSetting(string key, object fallback = null)
    {
        EnsureDefaults();
        return SaveData.Settings.TryGetValue(key, out var value) ? value : fallback;
    }

    private void EnsureDefaults()
    {
        SaveData.HighScores ??= new Dictionary<string, int>();
        SaveData.Unlocks ??= new Dictionary<string, bool>();
        SaveData.Settings ??= new Dictionary<string, object>();
    }

    private string ResolveStoragePath()
    {
        if (!string.IsNullOrWhiteSpace(_storagePath))
        {
            return _storagePath;
        }

        return DefaultFileName;
    }
}

public sealed class SaveData
{
    public int Version { get; set; } = 1;
    public Dictionary<string, int> HighScores { get; set; } = new();
    public int HighestTier { get; set; } = 1;
    public int TotalGamesPlayed { get; set; }
    public int TotalWins { get; set; }
    public bool TutorialSeen { get; set; }
    public Dictionary<string, bool> Unlocks { get; set; } = new();
    public Dictionary<string, object> Settings { get; set; } = new();
}
