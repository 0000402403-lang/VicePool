using UnityEngine;

/// <summary>Sons e música sintetizados em código (sem arquivos). Só escuta eventos do jogo.</summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    AudioSource music, sfx;
    AudioClip cHit, cCushion, cPocket, cFoul, cClick, cWin, cShot, cMusic;
    const int SR = 44100;

    public static void Ensure()
    {
        if (Instance != null) return;
        var go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        go.AddComponent<AudioManager>();
    }

    public static void Click() { if (Instance != null) Instance.Play(Instance.cClick, 0.8f); }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (GetComponent<AudioListener>() == null && FindFirstObjectByType<AudioListener>() == null) { }

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.loop = true;

        cClick = Blip(0.06f, 1300f, 1300f, 0.55f, 40f, 0f);
        cHit = Blip(0.10f, 1900f, 1500f, 0.5f, 45f, 0.5f);
        cCushion = Blip(0.14f, 260f, 200f, 0.6f, 28f, 0.25f);
        cPocket = Blip(0.42f, 620f, 150f, 0.7f, 7f, 0.1f);
        cFoul = Buzz(0.45f, 110f);
        cShot = Blip(0.09f, 340f, 240f, 0.7f, 50f, 0.6f);
        cWin = Arp(new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.16f);
        cMusic = BuildMusic();
        music.clip = cMusic;
        music.Play();

        GameEvents.OnBallHit += OnHit;
        GameEvents.OnCushionHit += OnCushion;
        GameEvents.OnBallPocketed += OnPocket;
        GameEvents.OnFoul += OnFoul;
        GameEvents.OnShot += OnShot;
        GameEvents.OnPhaseEnded += OnPhaseEnd;
        GameEvents.OnMatchEnded += OnMatchEnd;
        GameSettings.ApplyAudio();
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        GameEvents.OnBallHit -= OnHit;
        GameEvents.OnCushionHit -= OnCushion;
        GameEvents.OnBallPocketed -= OnPocket;
        GameEvents.OnFoul -= OnFoul;
        GameEvents.OnShot -= OnShot;
        GameEvents.OnPhaseEnded -= OnPhaseEnd;
        GameEvents.OnMatchEnded -= OnMatchEnd;
        Instance = null;
    }

    void Update()
    {
        music.volume = GameSettings.Music * 0.55f;
        if (Time.timeScale == 0f) music.volume *= 0.5f;
    }

    void Play(AudioClip c, float vol) { if (c != null) sfx.PlayOneShot(c, Mathf.Clamp01(vol) * GameSettings.Sfx); }

    void OnHit(float v) { Play(cHit, Mathf.Clamp(v / 5f, 0.12f, 1f)); }
    void OnCushion() { Play(cCushion, 0.55f); }
    void OnPocket(BallController b, int p) { Play(cPocket, 0.9f); }
    void OnFoul(string s) { Play(cFoul, 0.7f); }
    void OnShot(float p) { Play(cShot, 0.4f + 0.6f * p); }
    void OnPhaseEnd(int w, int i) { Play(cWin, 0.9f); }
    void OnMatchEnd(int w) { Play(cWin, 1f); }

    // ---------------- síntese ----------------
    static AudioClip Make(string name, float[] data)
    {
        var c = AudioClip.Create(name, data.Length, 1, SR, false);
        c.SetData(data, 0);
        return c;
    }

    static AudioClip Blip(float len, float f0, float f1, float amp, float decay, float noise)
    {
        int n = (int)(len * SR);
        var d = new float[n];
        var rnd = new System.Random(7);
        float ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float k = i / (float)n;
            float f = Mathf.Lerp(f0, f1, k);
            ph += 2f * Mathf.PI * f / SR;
            float env = Mathf.Exp(-t * decay);
            float s = Mathf.Sin(ph) * (1f - noise) + ((float)rnd.NextDouble() * 2f - 1f) * noise;
            d[i] = s * env * amp;
        }
        return Make("blip", d);
    }

    static AudioClip Buzz(float len, float f)
    {
        int n = (int)(len * SR);
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float sq = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t)) * 0.5f + Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * 1.5f * t)) * 0.25f;
            d[i] = sq * Mathf.Exp(-t * 5f) * 0.35f;
        }
        return Make("buzz", d);
    }

    static AudioClip Arp(float[] notes, float step)
    {
        int per = (int)(step * SR);
        var d = new float[per * (notes.Length + 3)];
        for (int k = 0; k < notes.Length; k++)
            for (int i = 0; i < per * 4 && k * per + i < d.Length; i++)
            {
                float t = i / (float)SR;
                d[k * per + i] += Mathf.Sin(2f * Mathf.PI * notes[k] * t) * Mathf.Exp(-t * 6f) * 0.35f;
            }
        return Make("arp", d);
    }

    static AudioClip BuildMusic()
    {
        const float bpm = 108f;
        float beat = 60f / bpm;
        float eighth = beat / 2f;
        int eighths = 64;                       // 8 compassos
        int n = (int)(eighths * eighth * SR);
        var d = new float[n];

        float[] roots = { 110f, 87.31f, 130.81f, 98f };                      // Am F C G
        float[][] chords = {
            new[] { 440f, 523.25f, 659.25f },
            new[] { 349.23f, 440f, 523.25f },
            new[] { 523.25f, 659.25f, 783.99f },
            new[] { 392f, 493.88f, 587.33f } };

        for (int e = 0; e < eighths; e++)
        {
            int bar = (e / 8) % 4;
            int start = (int)(e * eighth * SR);
            int len = (int)(eighth * SR);
            float root = roots[bar];
            var ch = chords[bar];
            float arpF = ch[(e % 3 == 0) ? 0 : (e % 3 == 1 ? 1 : 2)] * ((e % 2 == 0) ? 1f : 2f);
            for (int i = 0; i < len && start + i < n; i++)
            {
                float t = i / (float)SR;
                // baixo (serra suave)
                float ph = (root * t) % 1f;
                float bass = (ph * 2f - 1f) * Mathf.Exp(-t * 6f) * 0.35f;
                // arpejo (triângulo)
                float ap = (arpF * t) % 1f;
                float tri = (Mathf.Abs(ap * 4f - 2f) - 1f) * Mathf.Exp(-t * 7f) * 0.20f;
                d[start + i] += bass + tri;
            }
            // pad suave no começo de cada compasso
            if (e % 8 == 0)
            {
                int plen = (int)(8 * eighth * SR);
                for (int i = 0; i < plen && start + i < n; i++)
                {
                    float t = i / (float)SR;
                    float env = Mathf.Min(1f, t * 3f) * Mathf.Min(1f, (plen - i) / (float)SR * 3f);
                    float s = 0f;
                    foreach (var f in ch) s += Mathf.Sin(2f * Mathf.PI * f * 0.5f * t);
                    d[start + i] += s * env * 0.05f;
                }
            }
        }
        // filtro passa-baixa simples + fade nas pontas para o loop não estalar
        float y = 0f;
        for (int i = 0; i < n; i++)
        {
            y += (d[i] - y) * 0.25f;
            float edge = Mathf.Min(1f, Mathf.Min(i, n - i) / 400f);
            d[i] = Mathf.Clamp(y, -1f, 1f) * edge;
        }
        return Make("music", d);
    }
}
