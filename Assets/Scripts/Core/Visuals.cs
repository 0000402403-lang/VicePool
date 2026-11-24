using System.Collections.Generic;
using UnityEngine;

/// <summary>Paleta oficial do Vice Pool (hex do GDD) + extras para brilho neon.</summary>
public static class Palette
{
    // Cores do GDD
    public static readonly Color RosaNeon = H("F58AB8");
    public static readonly Color RosaPastel = H("F5A0C4");
    public static readonly Color Roxo = H("7D5BC7");
    public static readonly Color Turquesa = H("42C6D9");
    public static readonly Color Laranja = H("FF8B52");
    public static readonly Color Dourado = H("F7D66D");

    // Extras (versoes mais saturadas para emissao/bloom)
    public static readonly Color HotPink = H("FF2E93");
    public static readonly Color Magenta = H("D61F9B");
    public static readonly Color Ciano = H("22D3EE");
    public static readonly Color Violeta = H("B04CFF");
    public static readonly Color Fundo = H("0A0714");
    public static readonly Color Painel = H("170C2B");
    public static readonly Color BolaVermelha = H("F0213F");
    public static readonly Color BolaAzul = H("2459FF");

    public static Color H(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString("#" + hex, out c);
        return c;
    }

    public static Color A(Color c, float a) { return new Color(c.r, c.g, c.b, a); }
}

/// <summary>Fabrica de materiais URP (criados em codigo).</summary>
public static class Mats
{
    static Shader _lit, _unlit, _sprite;

    public static Shader LitShader
    {
        get
        {
            if (_lit == null) { _lit = Shader.Find("Universal Render Pipeline/Lit"); if (_lit == null) _lit = Shader.Find("Standard"); }
            return _lit;
        }
    }
    public static Shader UnlitShader
    {
        get
        {
            if (_unlit == null) { _unlit = Shader.Find("Universal Render Pipeline/Unlit"); if (_unlit == null) _unlit = Shader.Find("Unlit/Texture"); }
            return _unlit;
        }
    }
    public static Shader SpriteShader
    {
        get { if (_sprite == null) _sprite = Shader.Find("Sprites/Default"); return _sprite; }
    }

    public static Material Lit(Color c, float smooth = 0.5f, float metal = 0f)
    {
        var m = new Material(LitShader);
        m.SetColor("_BaseColor", c);
        m.SetColor("_Color", c);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", metal);
        return m;
    }

    public static void Emit(Material m, Color c, float intensity)
    {
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * intensity);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
    }

    /// <summary>Material neon: base escura + emissao forte (pega o Bloom).</summary>
    public static Material Glow(Color c, float intensity = 3f)
    {
        var m = Lit(new Color(c.r * 0.2f, c.g * 0.2f, c.b * 0.2f, 1f), 0.3f, 0f);
        Emit(m, c, intensity);
        return m;
    }

    public static Material UnlitTex(Texture tex, Color tint)
    {
        var m = new Material(UnlitShader);
        m.SetColor("_BaseColor", tint);
        m.SetColor("_Color", tint);
        m.SetTexture("_BaseMap", tex);
        m.mainTexture = tex;
        return m;
    }

    public static Material SpriteMat(Texture tex, Color tint)
    {
        var m = new Material(SpriteShader);
        m.mainTexture = tex;
        m.color = tint;
        return m;
    }

    public static Material LitTex(Texture baseT, Texture emisT, Color tint, float emisIntensity, float smooth)
    {
        var m = Lit(tint, smooth, 0f);
        if (baseT != null) { m.SetTexture("_BaseMap", baseT); m.mainTexture = baseT; }
        if (emisT != null)
        {
            m.SetTexture("_EmissionMap", emisT);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.white * emisIntensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        return m;
    }
}

/// <summary>Texturas procedurais (ceu, sol, janelas, grade neon, sprites de UI).</summary>
public static class Tex
{
    public struct Stop
    {
        public float t; public Color c;
        public Stop(float t, Color c) { this.t = t; this.c = c; }
    }

    public static Color Eval(Stop[] s, float t)
    {
        if (t <= s[0].t) return s[0].c;
        for (int i = 1; i < s.Length; i++)
        {
            if (t <= s[i].t)
            {
                float k = (t - s[i - 1].t) / Mathf.Max(0.0001f, s[i].t - s[i - 1].t);
                return Color.Lerp(s[i - 1].c, s[i].c, k);
            }
        }
        return s[s.Length - 1].c;
    }

    public static Texture2D Sky()
    {
        var st = new[] {
            new Stop(0f, Palette.H("0c0418")),
            new Stop(0.47f, Palette.H("1a0a30")),
            new Stop(0.5f, Palette.H("ff7a4a")),
            new Stop(0.53f, Palette.H("ff4f9a")),
            new Stop(0.60f, Palette.H("c21f9a")),
            new Stop(0.75f, Palette.H("6a1fa8")),
            new Stop(0.90f, Palette.H("2a0e5a")),
            new Stop(1f, Palette.H("120838")) };
        int h = 512;
        var tex = new Texture2D(4, h, TextureFormat.RGBA32, false);
        var px = new Color32[4 * h];
        for (int y = 0; y < h; y++)
        {
            Color c = Eval(st, y / (h - 1f));
            for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
        }
        tex.SetPixels32(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply(false);
        return tex;
    }

    /// <summary>Sol retro com listras (estilo Vice City).</summary>
    public static Texture2D Sun()
    {
        int s = 256;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true);
        var px = new Color32[s * s];
        var st = new[] { new Stop(0f, Palette.HotPink), new Stop(0.45f, Palette.Laranja), new Stop(1f, Palette.Dourado) };
        for (int y = 0; y < s; y++)
        {
            float v = y / (s - 1f);
            for (int x = 0; x < s; x++)
            {
                float u = x / (s - 1f);
                float dx = u - 0.5f, dy = v - 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / 0.5f;
                float a = 1f - Mathf.SmoothStep(0.97f, 1f, r);
                if (v < 0.55f)
                {
                    float yy = v / 0.55f;
                    float f = (yy * 7f) % 1f;
                    float cut = Mathf.Lerp(0.55f, 0.04f, yy);
                    if (f < cut) a = 0f;
                }
                Color c = Eval(st, v);
                c.a = a;
                px[y * s + x] = c;
            }
        }
        tex.SetPixels32(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply(true);
        return tex;
    }

    public static Texture2D Radial(int s, Color c, float power = 2f)
    {
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var px = new Color32[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s - 0.5f, dy = (y + 0.5f) / s - 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / 0.5f;
                float a = Mathf.Pow(Mathf.Clamp01(1f - r), power);
                var cc = c; cc.a = a * c.a;
                px[y * s + x] = cc;
            }
        tex.SetPixels32(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply(false);
        return tex;
    }

    /// <summary>Anel (para destacar cacapa e bola fantasma).</summary>
    public static Texture2D RingTex(int s, float inner, float outer)
    {
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true);
        var px = new Color32[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s - 0.5f, dy = (y + 0.5f) / s - 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / 0.5f;
                float a = Mathf.SmoothStep(inner - 0.04f, inner, r) * (1f - Mathf.SmoothStep(outer, outer + 0.04f, r));
                px[y * s + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels32(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply(true);
        return tex;
    }

    public static Texture2D DiscTex(int s)
    {
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, true);
        var px = new Color32[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s - 0.5f, dy = (y + 0.5f) / s - 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / 0.5f;
                px[y * s + x] = new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0.94f, 1f, r));
            }
        tex.SetPixels32(px);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply(true);
        return tex;
    }

    /// <summary>Janelas de predios: textura base (parede) e emissiva (janelas acesas).</summary>
    public static void Windows(int seed, out Texture2D baseT, out Texture2D emisT)
    {
        int w = 128, h = 256, cell = 16;
        var rnd = new System.Random(seed);
        baseT = new Texture2D(w, h, TextureFormat.RGBA32, true);
        emisT = new Texture2D(w, h, TextureFormat.RGBA32, true);
        var bp = new Color32[w * h];
        var ep = new Color32[w * h];
        Color wall = Palette.H("1d0f3a");
        Color win = Palette.H("34205c");
        Color[] lights = { Palette.Ciano, Palette.HotPink, Palette.Laranja, Palette.Dourado, Palette.Violeta };
        for (int cy = 0; cy < h / cell; cy++)
            for (int cx = 0; cx < w / cell; cx++)
            {
                bool lit = rnd.NextDouble() < 0.38;
                Color lc = lights[rnd.Next(lights.Length)];
                for (int y = 0; y < cell; y++)
                    for (int x = 0; x < cell; x++)
                    {
                        int px = cx * cell + x, py = cy * cell + y;
                        bool inWin = x >= 3 && x < 13 && y >= 3 && y < 12;
                        Color b = wall, e = Color.black;
                        if (inWin)
                        {
                            if (lit) { b = lc * 0.55f; e = lc; }
                            else b = win;
                        }
                        b.a = 1f; e.a = 1f;
                        bp[py * w + px] = b;
                        ep[py * w + px] = e;
                    }
            }
        baseT.SetPixels32(bp); emisT.SetPixels32(ep);
        baseT.wrapMode = TextureWrapMode.Repeat; emisT.wrapMode = TextureWrapMode.Repeat;
        baseT.Apply(true); emisT.Apply(true);
    }

    /// <summary>Grade neon (chao).</summary>
    public static void Grid(int s, int line, Color lineCol, Color bg, out Texture2D baseT, out Texture2D emisT)
    {
        baseT = new Texture2D(s, s, TextureFormat.RGBA32, true);
        emisT = new Texture2D(s, s, TextureFormat.RGBA32, true);
        var bp = new Color32[s * s];
        var ep = new Color32[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                bool isLine = x < line || y < line;
                bool faint = (x % (s / 2) < 1) || (y % (s / 2) < 1);
                Color b = bg, e = Color.black;
                if (isLine) { b = Color.Lerp(bg, lineCol, 0.7f); e = lineCol; }
                else if (faint) { b = Color.Lerp(bg, lineCol, 0.15f); e = lineCol * 0.18f; }
                b.a = 1f; e.a = 1f;
                bp[y * s + x] = b; ep[y * s + x] = e;
            }
        baseT.SetPixels32(bp); emisT.SetPixels32(ep);
        baseT.wrapMode = TextureWrapMode.Repeat; emisT.wrapMode = TextureWrapMode.Repeat;
        baseT.anisoLevel = 8; emisT.anisoLevel = 8;
        baseT.Apply(true); emisT.Apply(true);
    }

    // ---------- Sprites de UI ----------
    static Sprite _rounded, _circle, _white;
    static readonly Dictionary<string, Sprite> _grads = new Dictionary<string, Sprite>();

    public static Sprite Rounded()
    {
        if (_rounded != null) return _rounded;
        int size = 64, radius = 24;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Min(x + 0.5f, size - x - 0.5f);
                float cy = Mathf.Min(y + 0.5f, size - y - 0.5f);
                float a = 1f;
                if (cx < radius && cy < radius)
                {
                    float dx = radius - cx, dy = radius - cy;
                    a = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                }
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels32(px);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply(false);
        _rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return _rounded;
    }

    public static Sprite Circle()
    {
        if (_circle != null) return _circle;
        var t = DiscTex(128);
        t.filterMode = FilterMode.Bilinear;
        _circle = Sprite.Create(t, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100f);
        return _circle;
    }

    public static Sprite White()
    {
        if (_white != null) return _white;
        var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color32[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        t.SetPixels32(px); t.Apply(false);
        _white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        return _white;
    }

    /// <summary>Gradiente vertical (baixo -> cima) como sprite, para a barra de forca.</summary>
    public static Sprite VerticalGradient(string key, Color bottom, Color top)
    {
        Sprite s;
        if (_grads.TryGetValue(key, out s) && s != null) return s;
        int h = 128;
        var t = new Texture2D(4, h, TextureFormat.RGBA32, false);
        var px = new Color32[4 * h];
        for (int y = 0; y < h; y++)
        {
            Color c = Color.Lerp(bottom, top, y / (h - 1f));
            for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
        }
        t.SetPixels32(px);
        t.wrapMode = TextureWrapMode.Clamp;
        t.Apply(false);
        s = Sprite.Create(t, new Rect(0, 0, 4, h), new Vector2(0.5f, 0.5f), 100f);
        _grads[key] = s;
        return s;
    }
}
