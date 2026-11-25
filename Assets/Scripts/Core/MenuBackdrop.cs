using UnityEngine;

/// <summary>
/// Plano de fundo do Menu Principal, no estilo do mockup do GDD:
/// pôr do sol retro sobre o mar, skyline com janelas neon, palmeiras em silhueta,
/// avenida com carro esportivo neon e um helicóptero cruzando o céu.
/// </summary>
public static class MenuBackdrop
{
    const float SunX = 35f, SunZ = 286f, SunY = 38f;

    public static GameObject Build()
    {
        var root = new GameObject("CenarioMenu");
        var rnd = new System.Random(2611);

        SetupLighting();
        Sky(root.transform);
        Sun(root.transform);
        Water(root.transform);
        SunReflection(root.transform);
        Skyline(root.transform, rnd);
        Land(root.transform);
        Road(root.transform);
        Palms(root.transform, rnd);
        Car(root.transform);
        Heli(root.transform);
        return root;
    }

    // ------------------------------------------------------------ luz
    static void SetupLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Palette.H("4a2a70");
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            l.color = Color.Lerp(Palette.Laranja, Palette.HotPink, 0.45f);
            l.intensity = 1.0f;
            l.transform.rotation = Quaternion.Euler(14f, 190f, 0f);   // luz vinda do pôr do sol
        }
    }

    // ------------------------------------------------------------ céu e sol
static void Sky(Transform parent)
    {
        var real = Resources.Load<Material>("MenuSkyCity");
        if (real != null)
        {
            RenderSettings.skybox = real;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            DynamicGI.UpdateEnvironment();
            var c = Camera.main;
            if (c != null) c.clearFlags = CameraClearFlags.Skybox;
            return;
        }
        var sky = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sky.name = "Ceu";
        Object.Destroy(sky.GetComponent<Collider>());
        sky.transform.SetParent(parent, false);
        sky.transform.localScale = Vector3.one * 600f;
        var m = Mats.UnlitTex(Tex.Sky(), Color.white);
        m.SetFloat("_Cull", 0f);
        var r = sky.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

static void Sun(Transform parent)
    {
        if (Resources.Load<Material>("MenuSkyCity") != null) return;   // o céu real já tem a luz
        var sun = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sun.name = "Sol";
        Object.Destroy(sun.GetComponent<Collider>());
        sun.transform.SetParent(parent, false);
        sun.transform.position = new Vector3(SunX, SunY, SunZ);
        sun.transform.localScale = Vector3.one * 125f;
        sun.GetComponent<Renderer>().sharedMaterial = Mats.SpriteMat(Tex.Sun(), Color.white);
    }

    // ------------------------------------------------------------ mar
    static void Water(Transform parent)
    {
        if (Resources.Load<Material>("MenuSkyCity") != null) { RealSurfaces.Water(parent); return; }
        const int N = 256;
        var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        var stops = new[]
        {
            new Tex.Stop(0.00f, Palette.H("0d0518")),
            new Tex.Stop(0.35f, Palette.H("2b0b4d")),
            new Tex.Stop(0.70f, Palette.H("6a1670")),
            new Tex.Stop(0.90f, Palette.H("c02a80")),
            new Tex.Stop(1.00f, Palette.H("ff7a5a"))
        };
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = (x / (N - 1f) - 0.5f) * 2f, dy = (y / (N - 1f) - 0.5f) * 2f;
                float rho = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                t.SetPixel(x, y, Tex.Eval(stops, rho));
            }
        t.Apply();

        var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Mar";
        Object.Destroy(water.GetComponent<Collider>());
        water.transform.SetParent(parent, false);
        water.transform.localScale = new Vector3(60f, 1f, 60f);
        water.GetComponent<Renderer>().sharedMaterial = Mats.UnlitTex(t, Color.white);
        water.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// <summary>Faixa de reflexo do sol na água (listras que engrossam perto de quem olha).</summary>
    static void SunReflection(Transform parent)
    {
        if (Resources.Load<Material>("MenuSkyCity") != null) return;   // sem faixa falsa de reflexo
        const int W = 64, H = 256;
        var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        float zNear = -30f, zFar = SunZ;
        for (int y = 0; y < H; y++)
        {
            float k = y / (H - 1f);                       // 0 = perto, 1 = longe
            float d = Mathf.Max(18f, (k * (zFar - zNear)) + 18f);
            float phase = 900f / d;
            float duty = Mathf.Lerp(0.70f, 0.28f, k);
            bool on = Mathf.Repeat(phase, 1f) < duty;
            float width = Mathf.Lerp(0.95f, 0.16f, k);
            float env = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k / 0.12f)) * Mathf.Lerp(1f, 0.85f, k);
            Color c = Color.Lerp(Palette.HotPink, Palette.Laranja, k);
            for (int x = 0; x < W; x++)
            {
                float u = Mathf.Abs(x / (W - 1f) * 2f - 1f);
                float edge = Mathf.Clamp01(1f - u / width);
                edge = Mathf.SmoothStep(0f, 1f, edge);
                float a = on ? edge * env : 0f;
                t.SetPixel(x, y, new Color(c.r, c.g, c.b, a));
            }
        }
        t.Apply();

        float len = new Vector2(SunX, SunZ + 30f).magnitude;
        float yaw = Mathf.Atan2(SunX, SunZ + 30f) * Mathf.Rad2Deg;
        Vector3 start = new Vector3(0f, 0.08f, -30f);
        Vector3 dir = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));

        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "ReflexoSol";
        Object.Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(parent, false);
        q.transform.position = start + dir * (len * 0.5f);
        q.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(90f, 0f, 0f);
        q.transform.localScale = new Vector3(80f, len, 1f);
        q.GetComponent<Renderer>().sharedMaterial = Mats.SpriteMat(t, Color.white);
        q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ------------------------------------------------------------ skyline
    static void Skyline(Transform parent, System.Random rnd)
    {
        if (Resources.Load<Material>("MenuSkyCity") != null) return;   // skyline real vem da foto 360 (HDRI)
        var wins = new Material[3];
        for (int i = 0; i < 3; i++)
        {
            Texture2D b, e;
            Tex.Windows(100 + i * 17, out b, out e);
            wins[i] = Mats.LitTex(b, e, Color.white, 1.8f, 0.1f);
        }
        // camada de trás (mais clara, enevoada) e camada da frente (mais escura)
        for (int layer = 0; layer < 2; layer++)
        {
            int count = layer == 0 ? 46 : 34;
            Color tint = layer == 0 ? new Color(0.42f, 0.22f, 0.55f) : new Color(0.16f, 0.08f, 0.26f);
            for (int i = 0; i < count; i++)
            {
                float ang = Mathf.Lerp(-55f, 55f, (float)rnd.NextDouble()) * Mathf.Deg2Rad;
                float rad = layer == 0 ? Mathf.Lerp(250f, 282f, (float)rnd.NextDouble())
                                       : Mathf.Lerp(190f, 230f, (float)rnd.NextDouble());
                float x = Mathf.Sin(ang) * rad, z = Mathf.Cos(ang) * rad;
                float w = 9f + (float)rnd.NextDouble() * 14f;
                float d = 9f + (float)rnd.NextDouble() * 10f;
                float h = (layer == 0 ? 35f : 18f) + (float)rnd.NextDouble() * (layer == 0 ? 95f : 60f);
                // deixa o sol aparecer: prédios na frente dele ficam baixos
                if (x > -5f && x < 80f) h = Mathf.Min(h, layer == 0 ? 30f : 24f);

                var bld = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bld.name = "Predio_" + layer + "_" + i;
                Object.Destroy(bld.GetComponent<Collider>());
                bld.transform.SetParent(parent, false);
                bld.transform.position = new Vector3(x, h * 0.5f, z);
                bld.transform.localScale = new Vector3(w, h, d);
                bld.transform.rotation = Quaternion.Euler(0f, -Mathf.Rad2Deg * ang, 0f);
                var m = new Material(wins[(i + layer) % 3]);
                m.SetColor("_BaseColor", tint);
                m.SetColor("_Color", tint);
                m.SetTextureScale("_BaseMap", new Vector2(w / 6f, h / 14f));
                m.SetTextureScale("_EmissionMap", new Vector2(w / 6f, h / 14f));
                bld.GetComponent<Renderer>().sharedMaterial = m;
                bld.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }

    // ------------------------------------------------------------ terra, avenida, postes
    static GameObject Cube(Transform parent, string name, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    static void Land(Transform parent)
    {
        // península escura à esquerda (onde ficam as palmeiras em silhueta)
        var dark = Mats.Lit(Palette.H("0b0618"), 0.1f);
        Cube(parent, "Peninsula", new Vector3(-36f, -0.3f, 14f), new Vector3(46f, 0.6f, 110f), dark);
        Cube(parent, "PeninsulaBorda", new Vector3(-12.9f, 0.02f, 14f), new Vector3(0.18f, 0.05f, 110f), Mats.Glow(Palette.Ciano, 2.5f));
    }

    static void Road(Transform parent)
    {
        var asphalt = RealSurfaces.Asphalt(12f, 300f);
        Cube(parent, "Avenida", new Vector3(10f, -0.03f, 90f), new Vector3(12f, 0.1f, 300f), asphalt);
        Cube(parent, "BordaCiano", new Vector3(4.1f, 0.03f, 90f), new Vector3(0.16f, 0.04f, 300f), Mats.Glow(Palette.Ciano, 3f));
        Cube(parent, "BordaRosa", new Vector3(15.9f, 0.03f, 90f), new Vector3(0.16f, 0.04f, 300f), Mats.Glow(Palette.HotPink, 3f));

        var dash = Mats.Glow(Palette.RosaPastel, 2f);
        for (float z = -40f; z < 230f; z += 7f)
            Cube(parent, "Faixa", new Vector3(10f, 0.03f, z), new Vector3(0.2f, 0.03f, 2.6f), dash);

        // postes com luz neon ao longo da avenida
        var pole = Mats.Lit(Palette.H("120a22"), 0.4f, 0.3f);
        var lamp = Mats.Glow(Palette.RosaNeon, 4f);
        for (float z = -10f; z < 200f; z += 26f)
        {
            Cube(parent, "Poste", new Vector3(16.6f, 3f, z), new Vector3(0.18f, 6f, 0.18f), pole);
            Cube(parent, "PosteBraco", new Vector3(15.9f, 6f, z), new Vector3(1.6f, 0.14f, 0.14f), pole);
            Cube(parent, "Lampada", new Vector3(15.2f, 5.92f, z), new Vector3(0.7f, 0.1f, 0.22f), lamp);
        }
    }

    // ------------------------------------------------------------ palmeiras
    static void Palms(Transform parent, System.Random rnd)
    {
        // esquerda (em primeiro plano) e ao longo da avenida
        Palm(parent, new Vector3(-16f, 0f, -4f), 2.0f, rnd);
        Palm(parent, new Vector3(-24f, 0f, 14f), 1.7f, rnd);
        Palm(parent, new Vector3(-15f, 0f, 34f), 1.4f, rnd);
        Palm(parent, new Vector3(-30f, 0f, 48f), 1.5f, rnd);
        for (int i = 0; i < 6; i++)
            Palm(parent, new Vector3(18.6f + (float)rnd.NextDouble() * 1.5f, 0f, 6f + i * 30f), 1.3f - i * 0.06f, rnd);
    }

static void Palm(Transform parent, Vector3 pos, float scale, System.Random rnd)
    {
        RealPalm.Build(parent, pos, scale, rnd);
    }

    // ------------------------------------------------------------ carro neon
static void Car(Transform parent)
    {
        var car = RealCar.Build(parent);
        car.transform.position = new Vector3(6.6f, 0.03f, -17f);
        car.transform.rotation = Quaternion.Euler(0f, -26f, 0f);
        car.transform.localScale = Vector3.one * 1.6f;
    }

    static void CarPart(Transform parent, string name, Vector3 pos, Vector3 scale, Material m, float pitch = 0f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ------------------------------------------------------------ helicóptero
    static void Heli(Transform parent)
    {
        var h = new GameObject("Helicoptero");
        h.transform.SetParent(parent, false);
        h.transform.localScale = Vector3.one * 2.4f;
        var dark = Mats.Lit(Palette.H("06030d"), 0.2f);

        CarPart(h.transform, "Corpo", Vector3.zero, new Vector3(1.4f, 1.2f, 2.8f), dark);
        CarPart(h.transform, "Cauda", new Vector3(0f, 0.25f, -2.6f), new Vector3(0.25f, 0.3f, 2.6f), dark);
        CarPart(h.transform, "Mastro", new Vector3(0f, 0.75f, 0f), new Vector3(0.14f, 0.4f, 0.14f), dark);
        var rotor = new GameObject("Rotor");
        rotor.transform.SetParent(h.transform, false);
        rotor.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        CarPart(rotor.transform, "Pa1", Vector3.zero, new Vector3(7f, 0.04f, 0.22f), dark);
        CarPart(rotor.transform, "Pa2", Vector3.zero, new Vector3(0.22f, 0.04f, 7f), dark);

        var blink = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(blink.GetComponent<Collider>());
        blink.name = "LuzPisca";
        blink.transform.SetParent(h.transform, false);
        blink.transform.localPosition = new Vector3(0f, 0.4f, -3.9f);
        blink.transform.localScale = Vector3.one * 0.35f;
        blink.GetComponent<Renderer>().sharedMaterial = Mats.Glow(Palette.H("ff2a2a"), 6f);

        var anim = h.AddComponent<MenuHeli>();
        anim.rotor = rotor.transform;
        anim.blink = blink.GetComponent<Renderer>();
    }
}

/// <summary>Helicóptero do fundo do menu: cruza o céu devagar, rotor girando e luz piscando.</summary>
public class MenuHeli : MonoBehaviour
{
    public Transform rotor;
    public Renderer blink;
    float x = -150f;

    void Update()
    {
        x += Time.unscaledDeltaTime * 6f;
        if (x > 170f) x = -170f;
        transform.position = new Vector3(x, 62f + Mathf.Sin(Time.unscaledTime * 0.4f) * 2f, 235f);
        transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        if (rotor != null) rotor.Rotate(0f, 1500f * Time.unscaledDeltaTime, 0f);
        if (blink != null) blink.enabled = Mathf.Repeat(Time.unscaledTime, 1.2f) < 0.18f;
    }
}
