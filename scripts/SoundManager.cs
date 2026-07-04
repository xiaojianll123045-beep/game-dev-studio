using Godot;

/// <summary>全局音效管理</summary>
public partial class SoundManager : Node
{
    private AudioStreamPlayer _clickPlayer;
    private AudioStreamPlayer _hoverPlayer;
    private AudioStreamPlayer _bgmPlayer;
    private AudioStreamPlayer _menuPlayer;
    private AudioStream[] _bgmTracks = new AudioStream[2];
    private int _bgmIndex = 0;

    public override void _Ready()
    {
        _clickPlayer = AddPlayer("Click", "switch6.wav", false);
        _hoverPlayer = AddPlayer("Hover", "rollover1.wav", false);
        _menuPlayer = AddPlayer("Menu", "bgm_加载.wav", true);
        // BGM 用 AudioStreamMP3 直接从文件加载（不需 import）
        _bgmPlayer = new AudioStreamPlayer { Name = "BgmPlayer", VolumeDb = 0f };
        AddChild(_bgmPlayer);
        _bgmTracks[0] = ResourceLoader.Load<AudioStream>("res://assets/sounds/Casa Bossa Nova.mp3");
        _bgmTracks[1] = ResourceLoader.Load<AudioStream>("res://assets/sounds/Thinking Music.mp3");
        GD.Print($"Loaded BGM0={_bgmTracks[0] != null} BGM1={_bgmTracks[1] != null}");
        if (_bgmTracks[0] != null) _bgmPlayer.Stream = _bgmTracks[0];
        _bgmPlayer.Finished += _SwapBgm;
    }

    private AudioStream AddPlayerLoad(string path)
    {
        if (!FileAccess.FileExists(path)) { GD.Print($"WAV not found: {path}"); return null; }
        var res = ResourceLoader.Load<AudioStream>(path);
        if (res != null) { GD.Print($"Loaded WAV: {path}"); return res; }
        // fallback: manual parse
        using var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (f == null) return null;
        var len = (int)f.GetLength();
        var buf = f.GetBuffer(len);
        if (len < 44) return null;
        int sr = buf[24] | (buf[25] << 8) | (buf[26] << 16) | (buf[27] << 24);
        int ch = buf[22] | (buf[23] << 8);
        int bits = buf[34] | (buf[35] << 8);
        int pos = 12, dSize = 0;
        while (pos < len - 8) { int csz = buf[pos+4] | (buf[pos+5] << 8) | (buf[pos+6] << 16) | (buf[pos+7] << 24); if (buf[pos]=='d' && buf[pos+1]=='a' && buf[pos+2]=='t' && buf[pos+3]=='a') { dSize = csz; break; } pos += 8 + csz; }
        pos += 8; if (dSize <= 0 || pos >= len) return null;
        int pcmLen = Mathf.Min(dSize, len - pos);
        var pcm = new byte[pcmLen]; System.Buffer.BlockCopy(buf, pos, pcm, 0, pcmLen);
        return new AudioStreamWav { Data = pcm, LoopMode = AudioStreamWav.LoopModeEnum.Disabled, Format = bits == 8 ? AudioStreamWav.FormatEnum.Format8Bits : AudioStreamWav.FormatEnum.Format16Bits, MixRate = sr, Stereo = ch >= 2 };
    }

    private void _SwapBgm()
    {
        _bgmIndex = 1 - _bgmIndex;
        if (_bgmTracks[_bgmIndex] != null)
        {
            _bgmPlayer.Stream = _bgmTracks[_bgmIndex];
            _bgmPlayer.Play();
        }
    }

    private AudioStreamPlayer AddPlayer(string name, string file, bool loop)
    {
        var p = new AudioStreamPlayer { Name = $"{name}Player", VolumeDb = 0f };
        AddChild(p);
        if (loop) p.Finished += () => p.Play();

        var path = $"res://assets/sounds/{file}";
        GD.Print($"SFX: loading {file}...");

        // 方案A: ResourceLoader
        var res = ResourceLoader.Load<AudioStream>(path);
        if (res != null)
        {
            GD.Print($"SFX: {file} ResourceLoader type={res.GetType().Name}");
            p.Stream = res;
            return p;
        }

        // 方案B: 手动解析 WAV（兜底 + 确保 LoopMode 可控）
        if (!FileAccess.FileExists(path)) { GD.Print($"SFX: {file} not found"); return p; }
        var f = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (f == null) return p;
        var len = (int)f.GetLength();
        var buf = f.GetBuffer(len);
        f.Close();
        if (len < 44) return p;

        int sr = buf[24] | (buf[25] << 8) | (buf[26] << 16) | (buf[27] << 24);
        int ch = buf[22] | (buf[23] << 8);
        int bits = buf[34] | (buf[35] << 8);
        int pos = 12, dSize = 0;
        while (pos < len - 8)
        {
            int csz = buf[pos+4] | (buf[pos+5] << 8) | (buf[pos+6] << 16) | (buf[pos+7] << 24);
            if (buf[pos]=='d' && buf[pos+1]=='a' && buf[pos+2]=='t' && buf[pos+3]=='a') { dSize = csz; break; }
            pos += 8 + csz;
        }
        pos += 8;
        if (dSize <= 0 || pos >= len) return p;
        int pcmLen = Mathf.Min(dSize, len - pos);
        var pcm = new byte[pcmLen];
        System.Buffer.BlockCopy(buf, pos, pcm, 0, pcmLen);

        var wav = new AudioStreamWav
        {
            Data = pcm, LoopMode = AudioStreamWav.LoopModeEnum.Disabled,
            Format = bits == 8 ? AudioStreamWav.FormatEnum.Format8Bits : AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = sr, Stereo = ch >= 2
        };
        p.Stream = wav;
        GD.Print($"SFX: {file} direct OK ({pcmLen} bytes)");
        return p;
    }

    public void RefreshVolume()
    {
        float sv = GlobalSettings.SoundVolume / 100f * 80f - 80f; // 0→-80dB, 100→0dB
        _clickPlayer.VolumeDb = GlobalSettings.SoundEnabled ? sv : -80f;
        _hoverPlayer.VolumeDb = GlobalSettings.SoundEnabled ? sv : -80f;
        float mv = GlobalSettings.MusicVolume / 100f * 80f - 80f;
        _bgmPlayer.VolumeDb = (GlobalSettings.MusicEnabled && GlobalSettings.SoundEnabled) ? mv : -80f;
        _menuPlayer.VolumeDb = (GlobalSettings.MusicEnabled && GlobalSettings.SoundEnabled) ? mv : -80f;
    }

    public void PlayGameBgm()
    {
        RefreshVolume();
        _menuPlayer?.Stop(); _bgmPlayer?.Stop(); _bgmPlayer?.Play();
    }

    public void PlayMenuBgm()
    {
        RefreshVolume();
        _bgmPlayer?.Stop(); _menuPlayer?.Stop(); _menuPlayer?.Play();
    }

    public void StopBgm() { _bgmPlayer?.Stop(); _menuPlayer?.Stop(); }
    public void PlayClick() { RefreshVolume(); if (GlobalSettings.SoundEnabled) _clickPlayer?.Play(); }
    public void PlayHover() { RefreshVolume(); if (GlobalSettings.SoundEnabled) _hoverPlayer?.Play(); }
}
