using System;
using System.Collections.Generic;
using UnityEngine;

public enum BallType { Cue, Red, Blue, Gold }

public enum GameStateId { None, Placing, Aiming, Charging, BallsMoving, Evaluating, TurnEnd, PhaseEnd, MatchEnd }

/// <summary>Geometria fixa da mesa hexagonal (metros). Caçapa N fica no vértice N (1 = direita, anti-horário).</summary>
public static class TableGeometry
{
    public const float R = 1.5f;                       // raio do hexágono (vértice) = lado
    public static readonly float Apothem = R * 0.8660254f;
    public const float BallRadius = 0.04f;

    public static Vector3 Vertex(int pocketIndex1to6)
    {
        float a = (pocketIndex1to6 - 1) * 60f * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a) * R, 0f, Mathf.Sin(a) * R);
    }

    /// <summary>Ponto dentro da área jogável (longe das bordas e das caçapas).</summary>
    public static bool Inside(Vector3 p, float margin)
    {
        float a = Apothem - margin;
        for (int k = 0; k < 6; k++)
        {
            float ang = (k * 60f + 30f) * Mathf.Deg2Rad;
            if (p.x * Mathf.Cos(ang) + p.z * Mathf.Sin(ang) > a) return false;
        }
        for (int i = 1; i <= 6; i++)
        {
            Vector3 v = Vertex(i);
            float dx = p.x - v.x, dz = p.z - v.z;
            if (dx * dx + dz * dz < 0.22f * 0.22f) return false;
        }
        return true;
    }
}

public static class GameRules
{
    // Jogador 1 = azul, Jogador 2 = vermelho
    public static BallType OwnType(int player) { return player == 0 ? BallType.Blue : BallType.Red; }
    public static BallType OppType(int player) { return player == 0 ? BallType.Red : BallType.Blue; }
    public static string PlayerName(int p) { return "Jogador " + (p + 1); }
    public static Color PlayerColor(int p) { return p == 0 ? Palette.Ciano : Palette.HotPink; }
}

public static class GameSession
{
    public static int Mode = 0; // 0 = 2 jogadores locais (único modo do GDD)
}

/// <summary>Eventos do jogo (Observer). UI e áudio só escutam, sem mexer nas regras.</summary>
public static class GameEvents
{
    public static event Action<BallController, int> OnBallPocketed;
    public static event Action<string> OnFoul;
    public static event Action<string> OnMessage;
    public static event Action<int> OnTurnEnded;
    public static event Action<int> OnPhaseStarted;
    public static event Action<int, int> OnPhaseEnded;   // vencedor, índice da fase
    public static event Action<int> OnMatchEnded;        // vencedor
    public static event Action<GameStateId> OnStateChanged;
    public static event Action<float> OnPowerChanged;
    public static event Action<float> OnShot;
    public static event Action<float> OnBallHit;
    public static event Action OnCushionHit;
    public static event Action OnHudDirty;
    public static event Action<bool> OnPauseChanged;

    public static void RaiseBallPocketed(BallController b, int p) { if (OnBallPocketed != null) OnBallPocketed(b, p); }
    public static void RaiseFoul(string s) { if (OnFoul != null) OnFoul(s); }
    public static void RaiseMessage(string s) { if (OnMessage != null) OnMessage(s); }
    public static void RaiseTurnEnded(int next) { if (OnTurnEnded != null) OnTurnEnded(next); }
    public static void RaisePhaseStarted(int idx) { if (OnPhaseStarted != null) OnPhaseStarted(idx); }
    public static void RaisePhaseEnded(int winner, int idx) { if (OnPhaseEnded != null) OnPhaseEnded(winner, idx); }
    public static void RaiseMatchEnded(int winner) { if (OnMatchEnded != null) OnMatchEnded(winner); }
    public static void RaiseStateChanged(GameStateId s) { if (OnStateChanged != null) OnStateChanged(s); }
    public static void RaisePower(float v) { if (OnPowerChanged != null) OnPowerChanged(v); }
    public static void RaiseShot(float v) { if (OnShot != null) OnShot(v); }
    public static void RaiseBallHit(float v) { if (OnBallHit != null) OnBallHit(v); }
    public static void RaiseCushionHit() { if (OnCushionHit != null) OnCushionHit(); }
    public static void RaiseHud() { if (OnHudDirty != null) OnHudDirty(); }
    public static void RaisePause(bool p) { if (OnPauseChanged != null) OnPauseChanged(p); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearAll()
    {
        OnBallPocketed = null; OnFoul = null; OnMessage = null; OnTurnEnded = null; OnPhaseStarted = null;
        OnPhaseEnded = null; OnMatchEnded = null; OnStateChanged = null; OnPowerChanged = null; OnShot = null;
        OnBallHit = null; OnCushionHit = null; OnHudDirty = null; OnPauseChanged = null;
    }
}

/// <summary>Configurações salvas em PlayerPrefs.</summary>
public static class GameSettings
{
    public static float Master { get { return PlayerPrefs.GetFloat("vp_master", 0.9f); } set { PlayerPrefs.SetFloat("vp_master", value); } }
    public static float Music { get { return PlayerPrefs.GetFloat("vp_music", 0.5f); } set { PlayerPrefs.SetFloat("vp_music", value); } }
    public static float Sfx { get { return PlayerPrefs.GetFloat("vp_sfx", 0.9f); } set { PlayerPrefs.SetFloat("vp_sfx", value); } }
    public static int Quality { get { return PlayerPrefs.GetInt("vp_quality", QualitySettings.GetQualityLevel()); } set { PlayerPrefs.SetInt("vp_quality", value); } }
    public static bool Fullscreen { get { return PlayerPrefs.GetInt("vp_full", Screen.fullScreen ? 1 : 0) == 1; } set { PlayerPrefs.SetInt("vp_full", value ? 1 : 0); } }
    public static bool VSync { get { return PlayerPrefs.GetInt("vp_vsync", 1) == 1; } set { PlayerPrefs.SetInt("vp_vsync", value ? 1 : 0); } }
    public static bool AimLine { get { return PlayerPrefs.GetInt("vp_aimline", 1) == 1; } set { PlayerPrefs.SetInt("vp_aimline", value ? 1 : 0); } }
    public static int ResW { get { return PlayerPrefs.GetInt("vp_resw", 0); } set { PlayerPrefs.SetInt("vp_resw", value); } }
    public static int ResH { get { return PlayerPrefs.GetInt("vp_resh", 0); } set { PlayerPrefs.SetInt("vp_resh", value); } }

    static List<Vector2Int> _res;
    public static List<Vector2Int> Resolutions()
    {
        if (_res != null) return _res;
        _res = new List<Vector2Int>();
        foreach (var r in Screen.resolutions)
        {
            var v = new Vector2Int(r.width, r.height);
            if (v.x < 800) continue;
            if (!_res.Contains(v)) _res.Add(v);
        }
        if (_res.Count == 0) { _res.Add(new Vector2Int(1280, 720)); _res.Add(new Vector2Int(1920, 1080)); }
        return _res;
    }

    public static void ApplyAudio() { AudioListener.volume = Master; }

    public static void ApplyAll()
    {
        ApplyAudio();
        QualitySettings.SetQualityLevel(Mathf.Clamp(Quality, 0, QualitySettings.names.Length - 1), true);
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        if (ResW > 0 && ResH > 0) Screen.SetResolution(ResW, ResH, Fullscreen);
        else Screen.fullScreen = Fullscreen;
    }
}
