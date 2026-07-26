using Godot;
using TileMatchGame.Managers;

namespace TileMatchGame.Managers;

/// <summary>
/// Autoload singleton that procedurally synthesizes short SFX tones (no
/// external audio files needed) and plays them on gameplay events.
/// </summary>
public partial class AudioManager : Node
{
    private const int MixRate = 22050;
    private const int PoolSize = 4;

    private AudioStreamPlayer[] _playerPool;
    private int _poolIndex;

    private AudioStreamWav _swapSfx;
    private AudioStreamWav _matchSfx;
    private AudioStreamWav _invalidSfx;
    private AudioStreamWav _winSfx;
    private AudioStreamWav _loseSfx;
    private AudioStreamWav _tierUpSfx;

    public override void _Ready()
    {
        _playerPool = new AudioStreamPlayer[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            _playerPool[i] = new AudioStreamPlayer();
            AddChild(_playerPool[i]);
        }

        _swapSfx = GenerateTone(400, 600, 0.08f, 0.35f);
        _matchSfx = GenerateTone(700, 950, 0.12f, 0.4f);
        _invalidSfx = GenerateTone(220, 150, 0.15f, 0.35f);
        _winSfx = GenerateTone(500, 950, 0.5f, 0.45f);
        _loseSfx = GenerateTone(320, 120, 0.6f, 0.4f);
        _tierUpSfx = GenerateTone(600, 1300, 0.3f, 0.4f);

        var gameManager = GetNode<GameManager>("/root/GameManager");
        gameManager.OnSwapSound += () => Play(_swapSfx);
        gameManager.OnInvalidSound += () => Play(_invalidSfx);
        gameManager.OnScoreUpdated += _ => Play(_matchSfx);
        gameManager.OnTierUpdated += _ => Play(_tierUpSfx);
        gameManager.OnGameOver += won => Play(won ? _winSfx : _loseSfx);
    }

    private void Play(AudioStreamWav stream)
    {
        if (stream == null)
        {
            return;
        }

        var player = _playerPool[_poolIndex];
        _poolIndex = (_poolIndex + 1) % PoolSize;
        player.Stream = stream;
        player.Play();
    }

    /// <summary>Synthesizes a short mono PCM16 tone with a linear frequency sweep and fade-out envelope (avoids clicks).</summary>
    private static AudioStreamWav GenerateTone(float startFreq, float endFreq, float duration, float volume)
    {
        int sampleCount = (int)(MixRate * duration);
        var data = new byte[sampleCount * 2];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)MixRate;
            float progress = i / (float)sampleCount;
            float freq = Mathf.Lerp(startFreq, endFreq, progress);
            float envelope = 1.0f - progress;
            float sample = Mathf.Sin(2f * Mathf.Pi * freq * t) * volume * envelope;
            short pcm = (short)Mathf.Clamp(sample * 32767f, -32768f, 32767f);
            data[i * 2] = (byte)(pcm & 0xFF);
            data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
        }

        return new AudioStreamWav
        {
            Data = data,
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = MixRate,
            Stereo = false,
            LoopMode = AudioStreamWav.LoopModeEnum.Disabled
        };
    }
}
