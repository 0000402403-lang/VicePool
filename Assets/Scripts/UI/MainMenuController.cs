using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Cena MainMenu: Menu Principal, Seleção de Modo, Configurações e Créditos (painéis de UI).</summary>
public class MainMenuController : MonoBehaviour
{
    GameObject main, mode, credits, settings;
    Camera cam;
    float t;

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
        CityBackdrop.Build(false);
        BuildUi();
    }

    void Update()
    {
        if (cam == null) return;
        t += Time.unscaledDeltaTime;
        cam.transform.position = new Vector3(Mathf.Sin(t * 0.12f) * 6f, 4.5f + Mathf.Sin(t * 0.2f) * 0.4f, -30f);
        cam.transform.LookAt(new Vector3(0f, 16f, 120f));
    }

    void BuildUi()
    {
        var canvas = UIKit.CreateCanvas("Canvas", 10);
        canvas.transform.SetParent(transform, false);

        // ---------- principal ----------
        main = NewPage(canvas.transform, "Principal");
        var vice = UIKit.Label(main.transform, "LogoVice", "Vice", 200, Palette.HotPink, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic, true);
        UIKit.Place(vice.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-230f, 300f), new Vector2(560f, 240f));
        vice.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);
        var pool = UIKit.Label(main.transform, "LogoPool", "Pool", 200, Palette.Ciano, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic, true);
        UIKit.Place(pool.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(230f, 270f), new Vector2(560f, 240f));
        pool.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);
        var sub = UIKit.Label(main.transform, "Sub", "Sinuca 3D arcade em mesa hexagonal", 38, Color.white, TextAnchor.MiddleCenter, FontStyle.Italic, true);
        UIKit.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1000f, 60f));

        UIKit.NeonButton(main.transform, "Jogar", new Vector2(-450f, 20f), new Vector2(460f, 96f), Palette.HotPink, () => Show(mode), 42);
        UIKit.NeonButton(main.transform, "Configurações", new Vector2(-450f, -90f), new Vector2(460f, 96f), Palette.Violeta, () => Show(settings), 42);
        UIKit.NeonButton(main.transform, "Créditos", new Vector2(-450f, -200f), new Vector2(460f, 96f), Palette.Ciano, () => Show(credits), 42);
        UIKit.NeonButton(main.transform, "Sair", new Vector2(-450f, -310f), new Vector2(460f, 96f), Palette.Laranja, Quit, 42);

        // ---------- seleção de modo ----------
        mode = NewPage(canvas.transform, "Modo");
        var mt = UIKit.Label(mode.transform, "Titulo", "Selecione o Modo de Jogo", 74, Palette.HotPink, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic, true);
        UIKit.Place(mt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1400f, 110f));

        var b1 = UIKit.NeonButton(mode.transform, "2 Jogadores\n(Local)", new Vector2(-450f, 20f), new Vector2(380f, 400f), Palette.HotPink, StartGame, 46);
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
