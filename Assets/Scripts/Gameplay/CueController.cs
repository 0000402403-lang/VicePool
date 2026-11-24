using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Taco + mira por mouse + força segurando o botão. Linha de mira com SphereCast (estimativa visual).</summary>
public class CueController : MonoBehaviour
{
    public float chargeTime = 1.5f;
    const float CuePitch = 16f;   // taco inclinado: ponta na bola, base acima da borda da mesa

    BallRegistry reg;
    GameStateMachine gsm;
    Camera cam;

    Transform stick;
    LineRenderer line1, line2;
    SpriteRenderer ghost;
    Vector3 aimDir = Vector3.forward;
    float power;
    int builtStyle = -1;
    bool visible = true;

    struct CueStyle { public Color shaft, butt, band, tip; }
    static readonly CueStyle[] Styles = new CueStyle[4];

    void Awake()
    {
        Styles[0] = new CueStyle { shaft = Palette.RosaPastel, butt = Palette.Turquesa, band = Palette.HotPink, tip = Palette.Ciano };       // Vice City
        Styles[1] = new CueStyle { shaft = Palette.Roxo, butt = Palette.Laranja, band = Palette.Magenta, tip = Color.white };                // Sunset
        Styles[2] = new CueStyle { shaft = Palette.H("1a1a1a"), butt = Palette.H("0e0e0e"), band = Palette.Dourado, tip = Palette.HotPink }; // Luxury
        Styles[3] = new CueStyle { shaft = Palette.BolaAzul, butt = Palette.Violeta, band = Palette.Ciano, tip = Color.white };            // Neon Nights

        var sg = new GameObject("TacoVisual");
        sg.transform.SetParent(transform, false);
        stick = sg.transform;

        line1 = MakeLine("MiraLinha", Palette.A(Palette.Ciano, 0.9f), 0.012f);
        line2 = MakeLine("MiraSaida", Palette.A(Palette.HotPink, 0.9f), 0.010f);

        var gg = new GameObject("MiraFantasma");
        gg.transform.SetParent(transform, false);
        ghost = gg.AddComponent<SpriteRenderer>();
        ghost.sprite = Sprite.Create(Tex.RingTex(128, 0.78f, 1f), new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100f);
        ghost.color = Palette.A(Palette.Ciano, 0.95f);
        gg.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        gg.transform.localScale = Vector3.one * (BallController.Radius * 2f / 1.28f);
        SetVisible(false);
    }

    LineRenderer MakeLine(string n, Color c, float w)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = c;
        lr.startWidth = lr.endWidth = w;
        lr.alignment = LineAlignment.View;
        lr.useWorldSpace = true;
        lr.numCapVertices = 4;
        lr.positionCount = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        return lr;
    }

    void SetVisible(bool v)
    {
        visible = v;
        if (stick != null) stick.gameObject.SetActive(v);
        if (line1 != null) line1.enabled = v && GameSettings.AimLine;
        if (line2 != null) line2.enabled = false;
        if (ghost != null) ghost.enabled = v && GameSettings.AimLine;
    }

    // ---------------- visual do taco ----------------
    void BuildStick(int style)
    {
        builtStyle = style;
        for (int i = stick.childCount - 1; i >= 0; i--) Destroy(stick.GetChild(i).gameObject);
        var s = Styles[style];
        float z = 0f;
        z = Seg(z, 0.020f, 0.0060f, Mats.Glow(s.tip, 1.5f));
        z = Seg(z, 0.030f, 0.0070f, Mats.Lit(Color.white, 0.6f));
        z = Seg(z, 0.680f, 0.0090f, Mats.Lit(s.shaft, 0.7f, 0.1f));
        z = Seg(z, 0.030f, 0.0125f, Mats.Glow(s.band, 3f));
        z = Seg(z, 0.600f, 0.0140f, Mats.Lit(s.butt, 0.7f, 0.1f));
        z = Seg(z, 0.020f, 0.0145f, Mats.Glow(s.band, 3f));
        z = Seg(z, 0.030f, 0.0150f, Mats.Lit(Palette.H("0b0b12"), 0.4f));
    }

    float Seg(float zStart, float len, float radius, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        var col = go.GetComponent<Collider>();
        if (col != null) DestroyImmediate(col);
        go.transform.SetParent(stick, false);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        go.transform.localScale = new Vector3(radius * 3.2f, len * 0.5f, radius * 3.2f);
        go.transform.localPosition = new Vector3(0f, 0f, -(zStart + len * 0.5f));
        go.GetComponent<Renderer>().sharedMaterial = m;
        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return zStart + len;
    }

    // ---------------- loop ----------------
    void Update()
    {
        if (gsm == null) gsm = GameStateMachine.Instance;
        if (reg == null) reg = BallRegistry.Instance;
        if (cam == null) cam = Camera.main;
        if (gsm == null || reg == null || reg.cueBall == null || cam == null) return;

        var st = gsm.StateId;
        bool aiming = st == GameStateId.Aiming || st == GameStateId.Charging;
        if (!aiming)
        {
            if (visible) SetVisible(false);
            if (power != 0f) { power = 0f; GameEvents.RaisePower(0f); }
            return;
        }
        if (gsm.Paused) return;

        var mouse = Mouse.current;
        var kb = Keyboard.current;
        if (mouse == null) return;
        if (!visible) SetVisible(true);

        int style = (gsm.PhaseIndex * 2 + gsm.CurrentPlayer) % 4;
        if (style != builtStyle) BuildStick(style);

        Vector3 cuePos = reg.cueBall.transform.position;
        cuePos.y = BallController.Radius;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        var plane = new Plane(Vector3.up, new Vector3(0f, BallController.Radius, 0f));
        float d;
        Vector3 mouseWorld = cuePos + aimDir;
        if (plane.Raycast(ray, out d)) mouseWorld = ray.GetPoint(d);

        if (st == GameStateId.Aiming)
        {
            Vector3 dv = mouseWorld - cuePos; dv.y = 0f;
            if (dv.sqrMagnitude > 0.0025f) aimDir = dv.normalized;

            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) gsm.SelectPocket(1);
                if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) gsm.SelectPocket(2);
                if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) gsm.SelectPocket(3);
                if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) gsm.SelectPocket(4);
                if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) gsm.SelectPocket(5);
                if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) gsm.SelectPocket(6);
            }
            if (mouse.rightButton.wasPressedThisFrame)
            {
                int p = NearestPocket(mouseWorld, 0.45f);
                if (p > 0) gsm.SelectPocket(p);
            }
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (gsm.NeedsPocketChoice && gsm.ChosenPocket == 0)
                {
                    int p = NearestPocket(mouseWorld, 0.45f);
                    if (p > 0) gsm.SelectPocket(p);
                    else GameEvents.RaiseMessage("Escolha a caçapa antes da tacada!");
                }
                else if (gsm.BeginCharging()) { power = 0f; }
            }
        }
        else // Charging
        {
            power = Mathf.Min(1f, power + Time.deltaTime / chargeTime);
            GameEvents.RaisePower(power);
            if (mouse.rightButton.wasPressedThisFrame)
            {
                power = 0f; GameEvents.RaisePower(0f);
                gsm.CancelCharging();
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                float p = power;
                power = 0f; GameEvents.RaisePower(0f);
                if (p > 0.05f) gsm.Shoot(aimDir, p);
                else gsm.CancelCharging();
            }
        }

        // taco: ponta encostada na bola, recua conforme a força
        float pull = power * 0.30f;
        Quaternion flat = Quaternion.LookRotation(aimDir, Vector3.up);
        Quaternion rot = Quaternion.AngleAxis(CuePitch, flat * Vector3.right) * flat;
        stick.rotation = rot;
        stick.position = cuePos + (rot * Vector3.back) * (BallController.Radius + 0.012f + pull);

        UpdateAimLine(cuePos);
    }

    int NearestPocket(Vector3 point, float maxDist)
    {
        int best = 0; float bd = maxDist;
        for (int i = 0; i < reg.pockets.Length; i++)
        {
            var p = reg.pockets[i];
            if (p == null) continue;
            Vector3 pp = p.transform.position;
            float dx = pp.x - point.x, dz = pp.z - point.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist < bd) { bd = dist; best = i + 1; }
        }
        return best;
    }

    void UpdateAimLine(Vector3 origin)
    {
        bool on = GameSettings.AimLine;
        line1.enabled = on; ghost.enabled = on;
        if (!on) { line2.enabled = false; return; }

        var hits = Physics.SphereCastAll(origin, BallController.Radius * 0.98f, aimDir, 6f, ~0, QueryTriggerInteraction.Ignore);
        bool found = false; float bd = 999f; RaycastHit best = default(RaycastHit);
        foreach (var h in hits)
        {
            if (h.collider.gameObject == reg.cueBall.gameObject) continue;
            bool isBall = h.collider.GetComponent<BallController>() != null;
            bool isCushion = h.collider.GetComponent<CushionMarker>() != null;
            if (!isBall && !isCushion) continue;
            if (h.distance < bd) { bd = h.distance; best = h; found = true; }
        }

        float y = 0.012f;
        Vector3 start = new Vector3(origin.x, y, origin.z);
        Vector3 end = found ? origin + aimDir * bd : origin + aimDir * 3f;
        end.y = y;
        line1.SetPosition(0, start);
        line1.SetPosition(1, end);

        if (!found) { ghost.enabled = false; line2.enabled = false; return; }
        ghost.transform.position = end;
        var bc = best.collider.GetComponent<BallController>();
        if (bc != null)
        {
            Vector3 tp = bc.transform.position; tp.y = y;
            Vector3 outDir = tp - end; outDir.y = 0f;
            if (outDir.sqrMagnitude < 1e-6f) outDir = aimDir;
            outDir.Normalize();
            line2.SetPosition(0, tp);
            line2.SetPosition(1, tp + outDir * 0.45f);
            line2.enabled = true;
        }
        else
        {
            Vector3 n = best.normal; n.y = 0f; n.Normalize();
            Vector3 refl = Vector3.Reflect(aimDir, n); refl.y = 0f; refl.Normalize();
            line2.SetPosition(0, end);
            line2.SetPosition(1, end + refl * 0.6f);
            line2.enabled = true;
        }
    }
}
