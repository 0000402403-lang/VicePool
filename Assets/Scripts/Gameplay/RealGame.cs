using UnityEngine;

/// <summary>Materiais mais realistas para a mesa de sinuca: feltro com fibra, bolas lacadas, verniz nas bordas.</summary>
public static class RealGame
{
    static Texture2D _feltTex;

    static Texture2D FeltTex()
    {
        if (_feltTex != null) return _feltTex;
        const int S = 256;
        var t = new Texture2D(S, S, TextureFormat.RGBA32, true);
        var rnd = new System.Random(7);
        var px = new Color32[S * S];
        var raw = new float[S * S];
        for (int i = 0; i < raw.Length; i++) raw[i] = (float)rnd.NextDouble();
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                // média com vizinhos (fibras suaves) + grão fino
                float a = raw[y * S + x];
                float b = raw[((y + 1) % S) * S + x];
                float c = raw[y * S + (x + 1) % S];
                float v = 0.80f + 0.20f * (a * 0.5f + b * 0.25f + c * 0.25f);
                byte g = (byte)(Mathf.Clamp01(v) * 255f);
                px[y * S + x] = new Color32(g, g, g, 255);
            }
        t.SetPixels32(px);
        t.wrapMode = TextureWrapMode.Repeat;
        t.anisoLevel = 8;
        t.Apply(true);
        _feltTex = t;
        return t;
    }

    public static Material FeltMaterial(Color tint)
    {
        var m = Mats.Lit(tint, 0.05f, 0f);
        var tx = FeltTex();
        m.SetTexture("_BaseMap", tx);
        m.mainTexture = tx;
        m.SetTextureScale("_BaseMap", new Vector2(24f, 24f));
        m.SetFloat("_Cull", 0f);
        return m;
    }

    public static Material RailMaterial()
    {
        // verniz escuro (madeira laqueada) que reflete o ambiente
        return Mats.Lit(new Color(0.16f, 0.055f, 0.05f), 0.88f, 0.05f);
    }

    static Texture2D BallTex(Color c)
    {
        const int W = 256, H = 128;
        var t = new Texture2D(W, H, TextureFormat.RGBA32, true);
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W, v = (y + 0.5f) / H;
                Color col = c;
                foreach (float cu in new[] { 0.25f, 0.75f })
                {
                    float du = (u - cu) / 0.075f, dv = (v - 0.5f) / 0.17f;
                    float d = du * du + dv * dv;
                    if (d < 1f) col = Color.Lerp(Color.white, col, Mathf.SmoothStep(0.85f, 1f, d));
                    // pontinho escuro no centro (marca do número)
                    float n = du * du * 9f + dv * dv * 9f;
                    if (n < 1f && d < 1f) col = Color.Lerp(new Color(0.05f, 0.05f, 0.08f), col, Mathf.SmoothStep(0.6f, 1f, n));
                }
                px[y * W + x] = col;
            }
        t.SetPixels32(px);
        t.wrapMode = TextureWrapMode.Repeat;
        t.anisoLevel = 8;
        t.Apply(true);
        return t;
    }

    public static Material BallMaterial(Color c)
    {
        var m = Mats.Lit(Color.white, 0.98f, 0f);
        var tx = BallTex(c);
        m.SetTexture("_BaseMap", tx);
        m.mainTexture = tx;
        return m;
    }
}
