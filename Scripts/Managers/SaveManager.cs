using System;
using Godot;
using TileMatchGame.Data;

namespace TileMatchGame.Managers;

public partial class SaveManager : Node
{
    private SaveStore _store = new();
    public SaveData SaveData => _store.SaveData;

    public void SetStoragePath(string path)
    {
        _store.SetStoragePath(path);
    }

    public void Load()
    {
        try
        {
            _store.Load();
        }
        catch (Exception ex)
        {
            GD.PushWarning($"Save data corrupt, resetting: {ex.Message}");
            _store = new SaveStore();
            _store.Save();
        }
    }

    public void Save()
    {
        _store.Save();
    }

    public void RecordRunResult(GameMode mode, int score, int tier, bool won, int movesUsed, float timeRemaining)
    {
        _store.RecordRunResult(mode, score, tier, won, movesUsed, timeRemaining);
    }

    public int GetHighScore(GameMode mode)
    {
        return _store.GetHighScore(mode);
    }

    public void SetSetting(string key, object value)
    {
        _store.SetSetting(key, value);
    }

    public object GetSetting(string key, object fallback = null)
    {
        return _store.GetSetting(key, fallback);
    }

    public void ResetProgress()
    {
        _store.ResetProgress();
    }
}
