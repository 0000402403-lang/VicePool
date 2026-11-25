using System.Collections.Generic;
using UnityEngine;

/// <summary>Geometria procedural suave (anéis lofted, torno, tubos) para props mais realistas.</summary>
public static class ProcMesh
{
    /// <summary>Malha a partir de anéis. Convenção: cross(A,B) do anel aponta para o próximo anel (normais para fora).</summary>
    public static Mesh Rings(List<Vector3[]> rings, bool capStart, bool capEnd)
    {
        int seg = rings[0].Length, n = rings.Count;
        var v = new List<Vector3>();
        var uv = new List<Vector2>();
        for (int i = 0; i < n; i++)
            for (int j = 0; j < seg; j++)
            {
                v.Add(rings[i][j]);
                uv.Add(new Vector2(j / (float)seg, i / (float)(n - 1)));
            }
        var t = new List<int>();
        for (int i = 0; i < n - 1; i++)
            for (int j = 0; j < seg; j++)
            {
                int j2 = (j + 1) % seg;
                int a = i * seg + j, b = i * seg + j2, c = (i + 1) * seg + j, d = (i + 1) * seg + j2;
                t.Add(a); t.Add(b); t.Add(c);
                t.Add(b); t.Add(d); t.Add(c);
            }
        if (capStart)
        {
            int ci = v.Count; v.Add(Center(rings[0])); uv.Add(new Vector2(0.5f, 0f));
            for (int j = 0; j < seg; j++) { t.Add(ci); t.Add((j + 1) % seg); t.Add(j); }
        }
        if (capEnd)
        {
            int ci = v.Count; v.Add(Center(rings[n - 1])); uv.Add(new Vector2(0.5f, 1f));
            int o = (n - 1) * seg;
            for (int j = 0; j < seg; j++) { t.Add(ci); t.Add(o + j); t.Add(o + (j + 1) % seg); }
        }
        var m = new Mesh();
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static Vector3 Center(Vector3[] r)
    {
        var c = Vector3.zero;
        foreach (var p in r) c += p;
        return c / r.Length;
    }

    /// <summary>Carroceria: seções superelípticas ao longo de Z (frente = +Z).</summary>
    public static Mesh Loft(float[] z, float[] hw, float[] yb, float[] yt, int seg, float power)
    {
        var rings = new List<Vector3[]>();
        for (int i = 0; i < z.Length; i++)
        {
            var r = new Vector3[seg];
            for (int j = 0; j < seg; j++)
            {
                float th = Mathf.PI * 2f * j / seg;
                float c = Mathf.Cos(th), s = Mathf.Sin(th);
                float p = s < 0f ? power * 1.8f : power;          // fundo mais plano
                float cx = Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 2f / power);
                float sy = Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 2f / p);
                r[j] = new Vector3(hw[i] * cx, (yb[i] + yt[i]) * 0.5f + (yt[i] - yb[i]) * 0.5f * sy, z[i]);
            }
            rings.Add(r);
        }
        return Rings(rings, true, true);
    }

    /// <summary>Torno ao redor do eixo X: perfil = lista de (raio, x), percorrido em +x.</summary>
    public static Mesh Lathe(Vector2[] profile, int seg)
    {
        var rings = new List<Vector3[]>();
        foreach (var p in profile)
        {
            var r = new Vector3[seg];
            for (int j = 0; j < seg; j++)
            {
                float a = Mathf.PI * 2f * j / seg;
                r[j] = new Vector3(p.y, p.x * Mathf.Cos(a), p.x * Mathf.Sin(a));
            }
            rings.Add(r);
        }
        return Rings(rings, false, false);
    }

    /// <summary>Tubo cônico seguindo uma curva (tronco de palmeira).</summary>
    public static Mesh Tube(Vector3[] pts, float[] radii, int seg)
    {
        var rings = new List<Vector3[]>();
        for (int i = 0; i < pts.Length; i++)
        {
            Vector3 T = (pts[Mathf.Min(i + 1, pts.Length - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
            Vector3 A = Vector3.Cross(T, Vector3.forward).normalized;
            Vector3 B = Vector3.Cross(T, A);
            var r = new Vector3[seg];
            for (int j = 0; j < seg; j++)
            {
                float th = Mathf.PI * 2f * j / seg;
                r[j] = pts[i] + radii[i] * (Mathf.Cos(th) * A + Mathf.Sin(th) * B);
            }
            rings.Add(r);
        }
        return Rings(rings, false, true);
    }
}

/// <summary>Carro esportivo com carroceria suave, vidros, rodas com pneu/aro e lanternas.</summary>
public static class RealCar
{
    static Mesh _tire, _rim, _rimGlow;

    static GameObject Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 euler, Vector3 scale)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    static Mesh Prim(PrimitiveType t)
    {
        var g = GameObject.CreatePrimitive(t);
        var m = g.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(g);
        return m;
    }

    public static GameObject Build(Transform parent)
    {
        var car = new GameObject("CarroNeon");
        car.transform.SetParent(parent, false);
        Transform c = car.transform;

        var paint = Mats.Lit(new Color(0.80f, 0.04f, 0.55f), 0.95f, 0.9f);
        var glass = Mats.Lit(new Color(0.015f, 0.02f, 0.045f), 0.99f, 0.25f);
        var tireM = Mats.Lit(new Color(0.025f, 0.025f, 0.03f), 0.25f, 0f);
        var rimM = Mats.Lit(new Color(0.78f, 0.78f, 0.84f), 0.92f, 1f);
        var dark = Mats.Lit(new Color(0.015f, 0.015f, 0.02f), 0.55f, 0.4f);
        var rimGlow = Mats.Glow(Palette.Ciano, 3.5f);
        var tail = Mats.Glow(Palette.H("ff1a40"), 6f);
        var head = Mats.Glow(Palette.H("e8f6ff"), 4f);

        // ---- carroceria inferior ----
        var body = ProcMesh.Loft(
            new[] { -2.25f, -2.20f, -2.00f, -1.50f, -0.80f, 0.00f, 0.80f, 1.40f, 1.90f, 2.20f, 2.30f },
            new[] { 0.35f, 0.78f, 0.92f, 0.95f, 0.94f, 0.94f, 0.93f, 0.90f, 0.82f, 0.65f, 0.30f },
            new[] { 0.34f, 0.30f, 0.28f, 0.28f, 0.28f, 0.28f, 0.28f, 0.28f, 0.30f, 0.32f, 0.34f },
            new[] { 0.62f, 0.78f, 0.88f, 0.86f, 0.80f, 0.76f, 0.70f, 0.62f, 0.52f, 0.44f, 0.38f },
            32, 3.2f);
        Part(c, "Carroceria", body, paint, Vector3.zero, Vector3.zero, Vector3.one);

        // ---- cabine (vidros escuros) ----
        var cabin = ProcMesh.Loft(
            new[] { -1.40f, -1.10f, -0.50f, 0.20f, 0.65f, 0.85f },
            new[] { 0.48f, 0.70f, 0.76f, 0.74f, 0.66f, 0.42f },
            new[] { 0.68f, 0.68f, 0.68f, 0.66f, 0.64f, 0.62f },
            new[] { 0.95f, 1.16f, 1.24f, 1.20f, 0.98f, 0.82f },
            28, 2.4f);
        Part(c, "Cabine", cabin, glass, Vector3.zero, Vector3.zero, Vector3.one);

        // ---- aerodinâmica ----
        var cube = Prim(PrimitiveType.Cube);
        Part(c, "Splitter", cube, dark, new Vector3(0f, 0.27f, 2.18f), Vector3.zero, new Vector3(1.55f, 0.04f, 0.36f));
        Part(c, "Difusor", cube, dark, new Vector3(0f, 0.30f, -2.16f), Vector3.zero, new Vector3(1.5f, 0.07f, 0.30f));
        Part(c, "Saia", cube, dark, new Vector3(0f, 0.31f, 0f), Vector3.zero, new Vector3(1.88f, 0.05f, 3.6f));
        Part(c, "Aerofolio", cube, paint, new Vector3(0f, 1.13f, -2.02f), new Vector3(-4f, 0f, 0f), new Vector3(1.72f, 0.045f, 0.44f));
        Part(c, "AeroApoioE", cube, dark, new Vector3(-0.55f, 0.98f, -1.96f), Vector3.zero, new Vector3(0.05f, 0.30f, 0.12f));
        Part(c, "AeroApoioD", cube, dark, new Vector3(0.55f, 0.98f, -1.96f), Vector3.zero, new Vector3(0.05f, 0.30f, 0.12f));

        // ---- luzes ----
        Part(c, "LanternaFaixa", cube, tail, new Vector3(0f, 0.70f, -2.17f), Vector3.zero, new Vector3(1.38f, 0.06f, 0.05f));
        Part(c, "LanternaE", cube, tail, new Vector3(-0.66f, 0.62f, -2.15f), Vector3.zero, new Vector3(0.26f, 0.12f, 0.06f));
        Part(c, "LanternaD", cube, tail, new Vector3(0.66f, 0.62f, -2.15f), Vector3.zero, new Vector3(0.26f, 0.12f, 0.06f));
        var sph = Prim(PrimitiveType.Sphere);
        Part(c, "FarolE", sph, head, new Vector3(-0.58f, 0.56f, 2.12f), new Vector3(0f, -12f, 0f), new Vector3(0.34f, 0.09f, 0.22f));
        Part(c, "FarolD", sph, head, new Vector3(0.58f, 0.56f, 2.12f), new Vector3(0f, 12f, 0f), new Vector3(0.34f, 0.09f, 0.22f));
        Part(c, "NeonLateralE", cube, rimGlow, new Vector3(-0.955f, 0.33f, 0f), Vector3.zero, new Vector3(0.03f, 0.03f, 3.4f));
        Part(c, "NeonLateralD", cube, rimGlow, new Vector3(0.955f, 0.33f, 0f), Vector3.zero, new Vector3(0.03f, 0.03f, 3.4f));

        // ---- rodas ----
        if (_tire == null)
        {
            _tire = ProcMesh.Lathe(new[] {
                new Vector2(0.23f, -0.14f), new Vector2(0.31f, -0.145f), new Vector2(0.36f, -0.115f), new Vector2(0.375f, -0.05f),
                new Vector2(0.378f, 0f), new Vector2(0.375f, 0.05f), new Vector2(0.36f, 0.115f), new Vector2(0.31f, 0.145f), new Vector2(0.23f, 0.14f) }, 40);
            _rim = ProcMesh.Lathe(new[] {
                new Vector2(0.23f, -0.10f), new Vector2(0.25f, 0.06f), new Vector2(0.24f, 0.12f), new Vector2(0.20f, 0.13f),
                new Vector2(0.12f, 0.115f), new Vector2(0.06f, 0.125f), new Vector2(0.0f, 0.13f) }, 40);
            _rimGlow = ProcMesh.Lathe(new[] { new Vector2(0.222f, 0.135f), new Vector2(0.205f, 0.137f), new Vector2(0.19f, 0.135f) }, 40);
        }
        float[] xs = { -0.88f, 0.88f };
        float[] zs = { -1.38f, 1.42f };
        foreach (float x in xs)
            foreach (float z in zs)
            {
                var w = new GameObject("Roda").transform;
                w.SetParent(c, false);
                w.localPosition = new Vector3(x, 0.375f, z);
                w.localRotation = Quaternion.Euler(0f, x < 0f ? 180f : 0f, 0f);
                Part(w, "Pneu", _tire, tireM, Vector3.zero, Vector3.zero, Vector3.one);
                Part(w, "Aro", _rim, rimM, Vector3.zero, Vector3.zero, Vector3.one);
                Part(w, "AroNeon", _rimGlow, rimGlow, Vector3.zero, Vector3.zero, Vector3.one);
                for (int k = 0; k < 5; k++)
                    Part(w, "Raio", cube, rimM, new Vector3(0.128f, 0f, 0f), new Vector3(k * 72f, 0f, 0f), new Vector3(0.012f, 0.40f, 0.045f));
            }

        // ---- brilho no chão + luzes ----
        var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(glow.GetComponent<Collider>());
        glow.name = "BrilhoChao";
        glow.transform.SetParent(c, false);
        glow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        glow.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        glow.transform.localScale = new Vector3(4.4f, 7.6f, 1f);
        glow.GetComponent<Renderer>().sharedMaterial = Mats.SpriteMat(Tex.Radial(64, Color.white, 2f), Palette.A(Palette.Ciano, 0.55f));
        glow.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var lg = new GameObject("LuzRosa");
        lg.transform.SetParent(c, false);
        lg.transform.localPosition = new Vector3(0f, 1.4f, -4.5f);
        var l1 = lg.AddComponent<Light>();
        l1.type = LightType.Point; l1.color = Palette.HotPink; l1.intensity = 14f; l1.range = 14f;
        var lc = new GameObject("LuzCiano");
        lc.transform.SetParent(c, false);
        lc.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        var l2 = lc.AddComponent<Light>();
        l2.type = LightType.Point; l2.color = Palette.Ciano; l2.intensity = 6f; l2.range = 6f;
        return car;
    }
}

/// <summary>Palmeira com tronco curvo anelado e copa de folhas pinadas (textura com alfa).</summary>
public static class RealPalm
{
    static Material _bark, _leaf;

    static Material Bark()
    {
        if (_bark == null) _bark = Mats.Lit(new Color(0.16f, 0.10f, 0.09f), 0.12f, 0f);
        return _bark;
    }

    static Texture2D LeafTex()
    {
        const int W = 64, H = 128;
        var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W, v = (y + 0.5f) / H;
                float d = Mathf.Abs(u - 0.5f) * 2f;
                float phase = v * 24f + d * 2.4f;
                bool leaflet = Mathf.Repeat(phase, 1f) < 0.62f;
                bool rib = d < 0.06f;
                float a = (leaflet || rib) && d < 0.98f ? 1f : 0f;
                Color col = Color.Lerp(new Color(0.03f, 0.10f, 0.04f), new Color(0.09f, 0.22f, 0.08f), v);
                if (rib) col = new Color(0.10f, 0.08f, 0.05f);
                col.a = a;
                px[y * W + x] = col;
            }
        t.SetPixels32(px);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        t.Apply(false);
        return t;
    }

    static Material Leaf()
    {
        if (_leaf != null) return _leaf;
        _leaf = Mats.Lit(new Color(0.7f, 0.75f, 0.7f), 0.2f, 0f);
        var tx = LeafTex();
        _leaf.SetTexture("_BaseMap", tx);
        _leaf.mainTexture = tx;
        _leaf.SetFloat("_Cull", 0f);
        _leaf.SetFloat("_AlphaClip", 1f);
        _leaf.SetFloat("_Cutoff", 0.4f);
        _leaf.EnableKeyword("_ALPHATEST_ON");
        _leaf.SetOverrideTag("RenderType", "TransparentCutout");
        _leaf.renderQueue = 2450;
        return _leaf;
    }

    static void AddFrond(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t,
                         Matrix4x4 m, float len, float width, float rise, float droop, int segs)
    {
        int b0 = v.Count;
        Vector3 up = m.MultiplyVector(Vector3.up).normalized;
        for (int i = 0; i <= segs; i++)
        {
            float k = i / (float)segs;
            var p = new Vector3(0f, rise * k - droop * k * k, k * len);
            float w = width * Mathf.Sin(Mathf.PI * Mathf.Pow(k, 0.7f));
            // leve dobra em V no meio da folha
            var l = p + new Vector3(-w, w * 0.18f, 0f);
            var r = p + new Vector3(w, w * 0.18f, 0f);
            v.Add(m.MultiplyPoint3x4(l)); v.Add(m.MultiplyPoint3x4(r));
            n.Add(up); n.Add(up);
            uv.Add(new Vector2(0f, k)); uv.Add(new Vector2(1f, k));
        }
        for (int i = 0; i < segs; i++)
        {
            int a = b0 + i * 2, b = a + 1, c = a + 2, d = a + 3;
            t.Add(a); t.Add(c); t.Add(b);
            t.Add(b); t.Add(c); t.Add(d);
        }
    }

    public static GameObject Build(Transform parent, Vector3 pos, float scale, System.Random rnd)
    {
        var root = new GameObject("Palmeira");
        root.transform.SetParent(parent, false);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
        root.transform.localScale = Vector3.one * scale;

        // tronco: curva de Bézier no plano XY
        float bend = 0.9f + (float)rnd.NextDouble() * 1.6f;
        Vector3 p0 = Vector3.zero, p1 = new Vector3(0.15f * bend, 5f, 0f), p2 = new Vector3(bend, 9f, 0f);
        const int N = 24;
        var pts = new Vector3[N];
        var rad = new float[N];
        for (int i = 0; i < N; i++)
        {
            float k = i / (float)(N - 1);
            pts[i] = (1 - k) * (1 - k) * p0 + 2 * (1 - k) * k * p1 + k * k * p2;
            float ridge = 1f + 0.05f * Mathf.Sin(i * 2.6f);
            rad[i] = (Mathf.Lerp(0.26f, 0.13f, k) + 0.22f * Mathf.Exp(-k * 16f)) * ridge;
        }
        var trunk = new GameObject("Tronco", typeof(MeshFilter), typeof(MeshRenderer));
        trunk.transform.SetParent(root.transform, false);
        trunk.GetComponent<MeshFilter>().sharedMesh = ProcMesh.Tube(pts, rad, 12);
        trunk.GetComponent<MeshRenderer>().sharedMaterial = Bark();

        // copa
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        Vector3 top = pts[N - 1];
        int count = 13;
        for (int i = 0; i < count; i++)
        {
            float yaw = i * (360f / count) + (float)rnd.NextDouble() * 12f;
            float lift = 8f + (float)rnd.NextDouble() * 32f;
            var m = Matrix4x4.TRS(top, Quaternion.Euler(-lift, yaw, 0f), Vector3.one);
            AddFrond(v, n, uv, t, m,
                3.7f + (float)rnd.NextDouble() * 0.9f, 0.85f,
                0.9f + (float)rnd.NextDouble() * 0.8f, 2.4f + (float)rnd.NextDouble() * 1.2f, 10);
        }
        var mesh = new Mesh();
        mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
        mesh.RecalculateBounds();
        var crown = new GameObject("Copa", typeof(MeshFilter), typeof(MeshRenderer));
        crown.transform.SetParent(root.transform, false);
        crown.GetComponent<MeshFilter>().sharedMesh = mesh;
        crown.GetComponent<MeshRenderer>().sharedMaterial = Leaf();
        return root;
    }
}

/// <summary>Asfalto molhado (PBR da Poly Haven) e água escura refletindo o céu.</summary>
public static class RealSurfaces
{
    public static Material Asphalt(float sizeX, float sizeZ)
    {
        var src = Resources.Load<Material>("RoadAsphalt");
        if (src == null) return Mats.Lit(Palette.H("0c0816"), 0.85f, 0.2f);
        var m = new Material(src);
        m.SetTextureScale("_BaseMap", new Vector2(sizeX / 3f, sizeZ / 3f));
        return m;
    }

public static Material Pavement(float sizeX, float sizeZ)
    {
        var src = Resources.Load<Material>("Pavement");
        if (src == null) return Mats.Lit(Palette.H("1a1226"), 0.3f, 0f);
        var m = new Material(src);
        m.SetTextureScale("_BaseMap", new Vector2(sizeX / 3f, sizeZ / 3f));
        return m;
    }


    public static void Water(Transform parent)
    {
        var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Mar";
        Object.Destroy(water.GetComponent<Collider>());
        water.transform.SetParent(parent, false);
        water.transform.localScale = new Vector3(60f, 1f, 60f);
        var r = water.GetComponent<Renderer>();
        r.sharedMaterial = Mats.Lit(new Color(0.02f, 0.01f, 0.05f), 0.9f, 0.3f);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
