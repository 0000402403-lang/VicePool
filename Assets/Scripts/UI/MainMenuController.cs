using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Cena MainMenu: Menu Principal, Seleção de Modo, Configurações e Créditos (painéis de UI).</summary>
public class MainMenuController : MonoBehaviour
{
    GameObject main, mode, credits, settings;
    Camera cam;
    float t;
    bool usePhoto;
    RawImage photo;

void Awake()
    {
        Time.timeScale = 1f;
        GameSettings.ApplyAll();
        AudioManager.Ensure();
        UIKit.EnsureEventSystem();

        cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Fundo;
            cam.farClipPlane = 1500f;
            cam.fieldOfView = 55f;
        }

        // Fundo realista: se existir Assets/Resources/MenuBackground (png/jpg), ele é usado.
        // Caso contrário, cai no cenário 3D procedural.
        usePhoto = Resources.Load<Texture2D>("MenuBackground") != null;
        if (!usePhoto) MenuBackdrop.Build();
        BuildUi();
    }

void Update()
    {
        t += Time.unscaledDeltaTime;
        if (usePhoto) { FitPhoto(); return; }
        if (cam == null) return;
        cam.transform.position = new Vector3(Mathf.Sin(t * 0.12f) * 2.5f, 3.0f + Mathf.Sin(t * 0.2f) * 0.25f, -30f);
        cam.transform.LookAt(new Vector3(8f, 11f, 100f));
    }

void BuildUi()
    {
        var canvas = UIKit.CreateCanvas("Canvas", 10);
        canvas.transform.SetParent(transform, false);

        if (usePhoto) BuildPhoto(canvas.transform);

        // ---------- principal (igual ao mockup: logo no triângulo + botões à esquerda) ----------
        main = NewPage(canvas.transform, "Principal");
        BuildMainPage(main.transform);

        // ---------- seleção de modo ----------
        mode = NewPage(canvas.transform, "Modo");
        var mt = UIKit.Label(mode.transform, "Titulo", "Selecione o Modo de Jogo", 74, Palette.HotPink, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic, true);
        UIKit.Place(mt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1400f, 110f));

        UIKit.NeonButton(mode.transform, "2 Jogadores\n(Local)", new Vector2(-450f, 20f), new Vector2(380f, 400f), Palette.HotPink, StartGame, 46);
        var bCpu = UIKit.NeonButton(mode.transform, "Contra a CPU\n\nEm breve", new Vector2(0f, 20f), new Vector2(380f, 400f), Palette.Ciano, null, 46);
        bCpu.interactable = false;
        UIKit.NeonButton(mode.transform, "Opções", new Vector2(450f, 20f), new Vector2(380f, 400f), Palette.Violeta, () => Show(settings), 46);
        UIKit.NeonButton(mode.transform, "← Voltar", new Vector2(-640f, -400f), new Vector2(300f, 80f), Palette.Ciano, () => Show(main), 36);
        mode.SetActive(false);

        // ---------- créditos ----------
        credits = NewPage(canvas.transform, "Creditos");
        var box = UIKit.Panel(credits.transform, "Caixa", Palette.A(Palette.Painel, 0.92f));
        UIKit.Place(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 760f));
        var bo = box.gameObject.AddComponent<Outline>();
        bo.effectColor = Palette.A(Palette.Ciano, 0.9f); bo.effectDistance = new Vector2(3f, -3f);
        var ct = UIKit.Label(box.transform, "Titulo", "Créditos", 76, Palette.HotPink, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic, true);
        UIKit.Place(ct.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(900f, 100f));
        ct.rectTransform.pivot = new Vector2(0.5f, 1f);
        var body = UIKit.Label(box.transform, "Texto",
            "VICE POOL\nSinuca 3D arcade em mesa hexagonal | Unity\n\n" +
            "Jean  -  Física e Taco (gameplay)\n" +
            "Alexandre  -  Regras e Fluxo\n" +
            "Melrilly  -  Interface e Cenas (UI)\n" +
            "Alisson  -  Arte 3D e Áudio\n\n" +
            "Codificação de Sistemas de Jogos Digitais\n" +
            "Instrutor: Angiley Moura  |  SENAI Sete Lagoas\n" +
            "Turma HT-PJD-02-N-26-13451",
            34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, false);
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        UIKit.Place(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1000f, 520f));
        body.lineSpacing = 1.25f;
        UIKit.NeonButton(box.transform, "Voltar", new Vector2(0f, -320f), new Vector2(300f, 80f), Palette.Ciano, () => Show(main), 36);
        credits.SetActive(false);

        // ---------- configurações ----------
        settings = SettingsPanel.Build(canvas.transform, () => Show(main));
        settings.SetActive(false);
    }

void BuildMainPage(Transform p)
    {
        // logo dentro do triângulo neon (como no mockup)
        var tri = UIKit.NewImage(p, "LogoTriangulo", TriangleSprite(), Color.white);
        UIKit.Place(tri.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 215f), new Vector2(600f, 600f));

        var vice = UIKit.Label(p, "LogoVice", "Vice", 150, Palette.HotPink, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic, true);
        UIKit.Place(vice.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-105f, 335f), new Vector2(360f, 160f));
        vice.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 7f);
        var pool = UIKit.Label(p, "LogoPool", "Pool", 150, Palette.Ciano, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic, true);
        UIKit.Place(pool.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(105f, 270f), new Vector2(360f, 160f));
        pool.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 7f);

        // botões em coluna, lado esquerdo
        float x = -620f;
        var sz = new Vector2(500f, 84f);
        UIKit.NeonButton(p, "Jogar", new Vector2(x, 10f), sz, Palette.HotPink, () => Show(mode), 38);
        UIKit.NeonButton(p, "Configurações", new Vector2(x, -90f), sz, Palette.Violeta, () => Show(settings), 38);
        UIKit.NeonButton(p, "Créditos", new Vector2(x, -190f), sz, Palette.Ciano, () => Show(credits), 38);
        UIKit.NeonButton(p, "Sair", new Vector2(x, -290f), sz, Palette.Laranja, Quit, 38);
    }

    void BuildPhoto(Transform canvasT)
    {
        var go = new GameObject("FotoFundo", typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(canvasT, false);
        UIKit.Stretch(go.GetComponent<RectTransform>());
        photo = go.GetComponent<RawImage>();
        photo.texture = Resources.Load<Texture2D>("MenuBackground");
        photo.raycastTarget = false;

        // escurece levemente o lado esquerdo para os botões ficarem legíveis
        var g = new Texture2D(128, 1, TextureFormat.RGBA32, false);
        g.wrapMode = TextureWrapMode.Clamp;
        for (int i = 0; i < 128; i++)
        {
            float a = Mathf.Clamp01(1f - (i / 127f) / 0.6f) * 0.55f;
            g.SetPixel(i, 0, new Color(0.02f, 0f, 0.06f, a));
        }
        g.Apply();
        var ov = UIKit.NewImage(canvasT, "GradienteEsquerda", Sprite.Create(g, new Rect(0, 0, 128, 1), new Vector2(0.5f, 0.5f)), Color.white);
        UIKit.Stretch(ov.rectTransform);
    }

    void FitPhoto()
    {
        if (photo == null || photo.texture == null) return;
        float sa = (float)Screen.width / Mathf.Max(1, Screen.height);
        float ta = (float)photo.texture.width / photo.texture.height;
        float w = 1f, h = 1f;
        if (sa > ta) h = ta / sa; else w = sa / ta;
        float zoom = 1f / (1.04f + Mathf.Sin(t * 0.15f) * 0.015f);
        w *= zoom; h *= zoom;
        float ox = (1f - w) * 0.5f + Mathf.Sin(t * 0.1f) * 0.012f;
        float oy = (1f - h) * 0.5f;
        photo.uvRect = new Rect(ox, oy, w, h);
    }

    static float SegDist(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float k = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * k)).magnitude;
    }

    static float Side(Vector2 p, Vector2 a, Vector2 b)
    {
        return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
    }

    /// <summary>Triângulo invertido com borda neon rosa→ciano e preenchimento escuro translúcido.</summary>
    static Sprite TriangleSprite()
    {
        const int N = 512;
        var tx = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tx.wrapMode = TextureWrapMode.Clamp;
        var a = new Vector2(0.04f, 0.90f);
        var b = new Vector2(0.96f, 0.90f);
        var c = new Vector2(0.50f, 0.06f);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                var p = new Vector2((x + 0.5f) / N, (y + 0.5f) / N);
                float d = Mathf.Min(SegDist(p, a, b), Mathf.Min(SegDist(p, b, c), SegDist(p, c, a)));
                float s1 = Side(p, a, b), s2 = Side(p, b, c), s3 = Side(p, c, a);
                bool inside = !((s1 < 0f || s2 < 0f || s3 < 0f) && (s1 > 0f || s2 > 0f || s3 > 0f));
                float line = Mathf.Clamp01(1f - d / 0.011f);
                float glow = Mathf.Clamp01(1f - d / 0.05f);
                glow = glow * glow * 0.6f;
                float k = Mathf.Max(line, glow);
                Color neon = Color.Lerp(Palette.HotPink, Palette.Ciano, p.x);
                float fa = inside ? 0.78f : 0f;
                var col = new Color(
                    Mathf.Lerp(0.05f, neon.r, k),
                    Mathf.Lerp(0.02f, neon.g, k),
                    Mathf.Lerp(0.12f, neon.b, k),
                    Mathf.Max(fa, k));
                tx.SetPixel(x, y, col);
            }
        tx.Apply();
        return Sprite.Create(tx, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
    }


    GameObject NewPage(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        UIKit.Stretch(go.GetComponent<RectTransform>());
        return go;
    }

    void Show(GameObject page)
    {
        main.SetActive(page == main);
        mode.SetActive(page == mode);
        credits.SetActive(page == credits);
        settings.SetActive(page == settings);
    }

    void StartGame()
    {
        GameSession.Mode = 0;
        SceneManager.LoadScene("Game");
    }

    void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
