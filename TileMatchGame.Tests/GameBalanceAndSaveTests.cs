using System;
using System.IO;
using TileMatchGame.Data;
using TileMatchGame.Managers;
using Xunit;

namespace TileMatchGame.Tests;

public class GameBalanceAndSaveTests
{
    [Fact]
    public void LoadFromFile_ParsesVersionedBalanceData()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"tilematch-balance-{Guid.NewGuid():N}.json");
        File.WriteAllText(tempPath, """
        {
          "version": 2,
          "baseTargetScore": 2000,
          "startingMoves": 25,
          "timeAttackSeconds": 120,
          "tierScoreStep": 750,
          "tierTimeBonusSeconds": 15,
          "tierTargetMultiplier": 1.75
        }
        """);

        var balance = GameBalanceData.LoadFromFile(tempPath);

        Assert.Equal(2, balance.Version);
        Assert.Equal(2000, balance.BaseTargetScore);
        Assert.Equal(25, balance.StartingMoves);
        Assert.Equal(120f, balance.TimeAttackSeconds);
        Assert.Equal(750f, balance.TierScoreStep);
        Assert.Equal(15f, balance.TierTimeBonusSeconds);
        Assert.Equal(1.75f, balance.TierTargetMultiplier);
    }

    [Fact]
    public void RecordRunResult_UpdatesHighScoreAndTier()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"tilematch-save-{Guid.NewGuid():N}.json");
        var saveStore = new SaveStore(tempPath);

        saveStore.RecordRunResult(GameMode.Classic, score: 1800, tier: 4, won: true, movesUsed: 10, timeRemaining: 45f);

        Assert.Equal(1800, saveStore.GetHighScore(GameMode.Classic));
        Assert.Equal(4, saveStore.SaveData.HighestTier);
        Assert.True(saveStore.SaveData.TutorialSeen);
    }

    [Fact]
    public void Load_RecoversFromCorruptFile()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"tilematch-corrupt-{Guid.NewGuid():N}.json");
        File.WriteAllText(tempPath, "{not-json");

        var saveStore = new SaveStore(tempPath);
        saveStore.Load();

        Assert.NotNull(saveStore.SaveData);
        Assert.Equal(0, saveStore.GetHighScore(GameMode.TimeAttack));
        Assert.False(saveStore.SaveData.TutorialSeen);
    }

    [Fact]
    public void ResetProgress_ClearsSavedStatsAndTutorialState()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"tilematch-reset-{Guid.NewGuid():N}.json");
        var saveStore = new SaveStore(tempPath);

        saveStore.RecordRunResult(GameMode.Classic, 2500, 6, true, 12, 15f);
        saveStore.SetSetting("sound_enabled", false);

        saveStore.ResetProgress();

        Assert.Equal(0, saveStore.GetHighScore(GameMode.Classic));
        Assert.Equal(1, saveStore.SaveData.HighestTier);
        Assert.False(saveStore.SaveData.TutorialSeen);
        Assert.Empty(saveStore.SaveData.Settings);
    }
}
