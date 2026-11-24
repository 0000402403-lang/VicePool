using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Pós-processamento neon (Bloom + vinheta) criado em código, igual para menu e jogo.</summary>
public static class PostFx
{
    public static void Setup(Camera cam, float bloomIntensity = 1.3f)
    {
        if (cam != null)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }
        var go = new GameObject("PostFX");
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 50f;
        var prof = ScriptableObject.CreateInstance<VolumeProfile>();
        var b = prof.Add<Bloom>(true);
        b.threshold.Override(0.85f);
        b.intensity.Override(bloomIntensity);
        b.scatter.Override(0.7f);
        var v = prof.Add<Vignette>(true);
        v.intensity.Override(0.28f);
        v.smoothness.Override(0.5f);
        vol.sharedProfile = prof;
    }
}

/// <summary>Faz o texto olhar sempre para a câmera (números das caçapas).</summary>
public class FaceCamera : MonoBehaviour
{
    void LateUpdate()
    {
        var c = Camera.main;
        if (c != null) transform.rotation = c.transform.rotation;
    }
}

/// <summary>
/// Cena Game: monta mesa hexagonal (6 caçapas), 14 bolas, taco, câmera, luzes neon, cenário e HUD.
/// Tudo em código para o grupo conseguir ler e ajustar. A cor da mesa muda por fase (PhaseConfig).
/// </summary>
public class GameSceneBootstrap : MonoBehaviour
{
    const float R = TableGeometry.R;
    PhysicsMaterial ballPM, feltPM, cushionPM;

    void Awake()
    {
        Time.fixedDeltaTime = 0.01f;
        Time.timeScale = 1f;
        Physics.defaultSolverIterations = 12;
        Physics.defaultSolverVelocityIterations = 4;
        Physics.bounceThreshold = 0.05f;

        GameSettings.ApplyAll();
        AudioManager.Ensure();
        UIKit.EnsureEventSystem();

        SetupCamera();
        SetupLights();
        CityBackdrop.Build(true);

        MakePhysicsMaterials();
        var tableRoot = new GameObject("Mesa_Hexagonal");
        var tv = tableRoot.AddComponent<TableVisuals>();
        var neon = new List<Renderer>();
        var lights = new List<Light>();

        BuildTable(tableRoot.transform, tv, neon);
        var pockets = BuildPockets(tableRoot.transform);
        BuildNeonLights(tableRoot.transform, lights);
        tv.neon = neon.ToArray();
        tv.neonLights = lights.ToArray();

        var ballsRoot = new GameObject("Bolas");
        BuildBalls(ballsRoot.transform);

        var mgr = new GameObject("GameManager");
        var reg = mgr.AddComponent<BallRegistry>();
        var referee = mgr.AddComponent<RuleReferee>();
        var gsm = mgr.AddComponent<GameStateMachine>();
        gsm.Registry = reg;
        gsm.Referee = referee;

        var taco = new GameObject("Taco");
        taco.AddComponent<CueController>();

        var ui = new GameObject("UI");
        ui.AddComponent<GameUI>();
    }

    // ------------------------------------------------------------------ câmera e luz
    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var cg = new GameObject("Main Camera");
            cg.tag = "MainCamera";
            cam = cg.AddComponent<Camera>();
            cg.AddComponent<AudioListener>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Palette.Fundo;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 1500f;
        cam.fieldOfView = 42f;
        var rig = cam.GetComponent<CameraRig>();
        if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
        rig.offset = new Vector3(0f, 3.8f, -2.9f);
        rig.lookAt = new Vector3(0f, 0f, 0.02f);
        PostFx.Setup(cam, 1.3f);
    }

    void SetupLights()
    {
        Light sun = null;
        foreach (var l in FindObjectsByType<Light>())
            if (l.type == LightType.Directional) sun = l;
        if (sun == null)
        {
            var g = new GameObject("Directional Light");
            sun = g.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.color = Palette.RosaPastel;
        sun.intensity = 0.9f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(60f, -25f, 0f);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Palette.H("4a4590");
    }

    void BuildNeonLights(Transform parent, List<Light> list)
    {
        for (int i = 0; i < 3; i++)
        {
            float a = (i * 120f + 30f) * Mathf.Deg2Rad;
            var g = new GameObject("LuzNeon" + (i + 1));
            g.transform.SetParent(parent, false);
            g.transform.position = new Vector3(Mathf.Cos(a) * 1.1f, 1.1f, Mathf.Sin(a) * 1.1f);
            var l = g.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 2.6f;
            l.intensity = 1.1f;
            l.color = Palette.HotPink;
            l.shadows = LightShadows.None;
            list.Add(l);
        }
    }

    // ------------------------------------------------------------------ física
    void MakePhysicsMaterials()
    {
        ballPM = new PhysicsMaterial("Bola") { bounciness = 0.95f, dynamicFriction = 0.05f, staticFriction = 0.05f };
        feltPM = new PhysicsMaterial("Feltro") { bounciness = 0f, dynamicFriction = 0.1f, staticFriction = 0.1f };
        cushionPM = new PhysicsMaterial("Tabela") { bounciness = 0.75f, dynamicFriction = 0.02f, staticFriction = 0.02f };
    }

    // ------------------------------------------------------------------ mesa
    void BuildTable(Transform root, TableVisuals tv, List<Renderer> neon)
    {
        // feltro (hexágono plano com MeshCollider)
        var felt = new GameObject("Feltro");
        felt.transform.SetParent(root, false);
        var fmesh = HexMesh(R, 0f);
        felt.AddComponent<MeshFilter>().sharedMesh = fmesh;
        var fr = felt.AddComponent<MeshRenderer>();
        fr.sharedMaterial = DoubleSided(Mats.Lit(Palette.Magenta, 0.15f));
        var fc = felt.AddComponent<MeshCollider>();
        fc.sharedMesh = fmesh;
        fc.sharedMaterial = feltPM;
        tv.felt = fr;

        // base escura + placa neon por baixo
        MakeMesh("BaseMesa", root, HexPrism(R + 0.45f, -0.28f, -0.004f), DoubleSided(Mats.Lit(Palette.H("120a22"), 0.6f, 0.3f)));
        neon.Add(MakeMesh("BrilhoBase", root, HexPrism(R + 0.47f, -0.31f, -0.28f), DoubleSided(Mats.Glow(Palette.HotPink, 3f))));

        // pedestal
        var ped = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ped.name = "Pedestal";
        Destroy(ped.GetComponent<Collider>());
        ped.transform.SetParent(root, false);
        ped.transform.localPosition = new Vector3(0f, -0.515f, 0f);
        ped.transform.localScale = new Vector3(0.9f, 0.205f, 0.9f);
        ped.GetComponent<Renderer>().sharedMaterial = Mats.Lit(Palette.H("0e0719"), 0.5f, 0.4f);

        // 6 bordas (tabelas) com colisor grosso + faixa neon
        float len = R - 0.30f;
        var railMat = Mats.Lit(Palette.H("1b0f33"), 0.65f, 0.35f);
        for (int k = 0; k < 6; k++)
        {
            float a = (k * 60f + 30f) * Mathf.Deg2Rad;
            var n = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            var rot = Quaternion.LookRotation(n, Vector3.up);

            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "Borda_" + (k + 1);
            rail.transform.SetParent(root, false);
            rail.transform.localPosition = n * (TableGeometry.Apothem + 0.15f) + new Vector3(0f, 0.045f, 0f);
            rail.transform.localRotation = rot;
            rail.transform.localScale = new Vector3(len, 0.15f, 0.30f);
            rail.GetComponent<Renderer>().sharedMaterial = railMat;
            var col = rail.GetComponent<BoxCollider>();
            col.sharedMaterial = cushionPM;
            rail.AddComponent<CushionMarker>();

            var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "NeonBorda_" + (k + 1);
            Destroy(strip.GetComponent<Collider>());
            strip.transform.SetParent(root, false);
            strip.transform.localPosition = n * (TableGeometry.Apothem + 0.012f) + new Vector3(0f, 0.121f, 0f);
            strip.transform.localRotation = rot;
            strip.transform.localScale = new Vector3(len, 0.016f, 0.024f);
            var sr = strip.GetComponent<Renderer>();
            sr.sharedMaterial = Mats.Glow(Palette.HotPink, 3.2f);
            sr.shadowCastingMode = ShadowCastingMode.Off;
            neon.Add(sr);
        }

        // cantos (colar escuro em volta de cada caçapa)
        var collarMat = Mats.Lit(Palette.H("0e0719"), 0.5f, 0.4f);
        for (int i = 1; i <= 6; i++)
        {
            var v = TableGeometry.Vertex(i);
            var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            c.name = "CantoCacapa_" + i;
            Destroy(c.GetComponent<Collider>());
            c.transform.SetParent(root, false);
            c.transform.localPosition = v * 1.12f + new Vector3(0f, 0.045f, 0f);
            c.transform.localScale = new Vector3(0.34f, 0.075f, 0.34f);
            c.GetComponent<Renderer>().sharedMaterial = collarMat;
        }
    }

    PocketTrigger[] BuildPockets(Transform root)
    {
        var result = new PocketTrigger[6];
        var holeMat = Mats.Lit(Color.black, 0.1f);
        var ringTex = Tex.RingTex(128, 0.78f, 1f);
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        for (int i = 1; i <= 6; i++)
        {
            var v = TableGeometry.Vertex(i);
            var go = new GameObject("Cacapa_" + i);
            go.SetActive(false);                       // configura antes do Awake rodar
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(v.x, BallController.Radius, v.z);

            var pt = go.AddComponent<PocketTrigger>();
            var sc = go.GetComponent<SphereCollider>();
            sc.radius = 0.13f;
            sc.isTrigger = true;
            pt.index = i;
            pt.neon = Palette.HotPink;

            // buraco preto
            var hole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hole.name = "Buraco";
            Destroy(hole.GetComponent<Collider>());
            hole.transform.SetParent(go.transform, false);
            hole.transform.localPosition = new Vector3(0f, -BallController.Radius + 0.002f, 0f);
            hole.transform.localScale = new Vector3(0.25f, 0.001f, 0.25f);
            hole.GetComponent<Renderer>().sharedMaterial = holeMat;
            hole.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // anel neon (destaca a caçapa escolhida)
            var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ring.name = "Anel";
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(go.transform, false);
            ring.transform.localPosition = new Vector3(0f, -BallController.Radius + 0.005f, 0f);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.transform.localScale = Vector3.one * 0.30f;
            var rr = ring.GetComponent<Renderer>();
            rr.sharedMaterial = Mats.SpriteMat(ringTex, Color.white);
            rr.shadowCastingMode = ShadowCastingMode.Off;
            pt.ring = rr;

            // número da caçapa (1 a 6)
            var label = new GameObject("Numero");
            label.transform.SetParent(go.transform, false);
            label.transform.position = new Vector3(v.x * 1.12f, 0.19f, v.z * 1.12f);
            label.AddComponent<FaceCamera>();
            var tm = label.AddComponent<TextMesh>();
            tm.font = font;
            tm.text = i.ToString();
            tm.fontSize = 64;
            tm.characterSize = 0.045f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = Palette.Dourado;
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;

            go.SetActive(true);
            result[i - 1] = pt;
        }
        return result;
    }

    // ------------------------------------------------------------------ bolas
    void BuildBalls(Transform root)
    {
        float r = BallController.Radius;
        // branca (posição inicial: parte de baixo da mesa, como no esboço do GDD)
        MakeBall(root, BallType.Cue, 0, new Vector3(0f, r, -0.7f), Color.white, "BolaBranca");

        // dourada no centro do grupo (sempre a última)
        var center = new Vector3(0f, r, 0.25f);
        MakeBall(root, BallType.Gold, 0, center, Palette.Dourado, "BolaDourada");

        // 12 bolas em dois anéis ao redor da dourada: 6 vermelhas + 6 azuis
        BallType[] ring1 = { BallType.Red, BallType.Blue, BallType.Red, BallType.Blue, BallType.Red, BallType.Blue };
        BallType[] ring2 = { BallType.Blue, BallType.Red, BallType.Blue, BallType.Red, BallType.Blue, BallType.Red };
        int red = 0, blue = 0;
        for (int ring = 0; ring < 2; ring++)
        {
            var types = ring == 0 ? ring1 : ring2;
            float dist = ring == 0 ? 0.086f : 0.174f;
            for (int i = 0; i < 6; i++)
            {
                float a = (90f + i * 60f) * Mathf.Deg2Rad;
                var pos = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * dist;
                var t = types[i];
                int idx = t == BallType.Red ? ++red : ++blue;
                MakeBall(root, t, idx, pos, t == BallType.Red ? Palette.BolaVermelha : Palette.BolaAzul,
                         (t == BallType.Red ? "Vermelha_" : "Azul_") + idx);
            }
        }
    }

    void MakeBall(Transform root, BallType type, int index, Vector3 pos, Color color, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * BallController.Radius * 2f;
        go.GetComponent<SphereCollider>().sharedMaterial = ballPM;

        Material m;
        if (type == BallType.Gold)
        {
            m = Mats.Lit(color, 0.95f, 0.5f);
            Mats.Emit(m, color, 0.8f);
        }
        else if (type == BallType.Cue)
        {
            m = Mats.Lit(Color.white, 0.95f, 0f);
            Mats.Emit(m, Color.white, 0.12f);
        }
        else
        {
            m = Mats.Lit(color, 0.95f, 0.1f);
            Mats.Emit(m, color, 0.35f);
        }
        go.GetComponent<Renderer>().sharedMaterial = m;

        var bc = go.AddComponent<BallController>();
        bc.type = type;
        bc.index = index;
        bc.startPosition = pos;
        if (type == BallType.Cue) go.AddComponent<CueBallController>();
    }

    // ------------------------------------------------------------------ malhas
    static Material DoubleSided(Material m)
    {
        m.SetFloat("_Cull", 0f);
        return m;
    }

    static MeshRenderer MakeMesh(string name, Transform parent, Mesh mesh, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        return r;
    }

    static Vector3 Corner(int i, float r, float y)
    {
        float a = (i % 6) * 60f * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
    }

    static Mesh HexMesh(float radius, float y)
    {
        var verts = new Vector3[7];
        var uvs = new Vector2[7];
        verts[0] = new Vector3(0f, y, 0f);
        uvs[0] = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < 6; i++)
        {
            verts[i + 1] = Corner(i, radius, y);
            uvs[i + 1] = new Vector2(verts[i + 1].x / (2f * radius) + 0.5f, verts[i + 1].z / (2f * radius) + 0.5f);
        }
        var tris = new int[18];
        for (int i = 0; i < 6; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = ((i + 1) % 6) + 1;
            tris[i * 3 + 2] = i + 1;
        }
        var m = new Mesh { name = "HexFeltro" };
        m.vertices = verts;
        m.uv = uvs;
        m.triangles = tris;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    static Mesh HexPrism(float r, float y0, float y1)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();

        int c = verts.Count;
        verts.Add(new Vector3(0f, y1, 0f));
        for (int i = 0; i < 6; i++) verts.Add(Corner(i, r, y1));
        for (int i = 0; i < 6; i++) { tris.Add(c); tris.Add(c + 1 + (i + 1) % 6); tris.Add(c + 1 + i); }

        for (int i = 0; i < 6; i++)
        {
            int s = verts.Count;
            verts.Add(Corner(i, r, y0));
            verts.Add(Corner(i, r, y1));
            verts.Add(Corner(i + 1, r, y1));
            verts.Add(Corner(i + 1, r, y0));
            tris.AddRange(new[] { s, s + 1, s + 2, s, s + 2, s + 3 });
        }

        var m = new Mesh { name = "HexPrisma" };
        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}
