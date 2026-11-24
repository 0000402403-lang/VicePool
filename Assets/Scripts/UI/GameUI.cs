using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD, pausa, configurações, aviso de fase e tela de Fim de Jogo. Só escuta os eventos do jogo.</summary>
public class GameUI : MonoBehaviour
{
    GameStateMachine gsm;
    BallRegistry reg;

    Image[][] dots = new Image[2][];
    Outline[] panelOutline = new Outline[2];
    Text[] names = new Text[2];
    Text phaseText, scoreText, ruleText, msgText, hintText, chosenText, bannerText, bannerSub, endWinner, endScore;
    Image powerFill;
    GameObject pausePanel, pauseBox, settingsPanel, endPanel, bannerPanel;
    float power, powerShown, msgTimer;
    Color msgColor = Color.white;

    void Awake()
    {
        UIKit.EnsureEventSystem();
        BuildHud();
        BuildPause();
        BuildEnd();
        BuildBanner();
    }

    void OnEnable()
    {
        GameEvents.OnHudDirty += Refresh;
        GameEvents.OnMessage += ShowMsg;
        GameEvents.OnFoul += ShowFoul;
        GameEvents.OnPowerChanged += OnPower;
        GameEvents.OnPauseChanged += OnPause;
        GameEvents.OnPhaseEnded += OnPhaseEnded;
        GameEvents.OnPhaseStarted += OnPhaseStarted;
        GameEvents.OnMatchEnded += OnMatchEnded;
    }

    void OnDisable()
    {
        GameEvents.OnHudDirty -= Refresh;
        GameEvents.OnMessage -= ShowMsg;
        GameEvents.OnFoul -= ShowFoul;
        GameEvents.OnPowerChanged -= OnPower;
        GameEvents.OnPauseChanged -= OnPause;
        GameEvents.OnPhaseEnded -= OnPhaseEnded;
        GameEvents.OnPhaseStarted -= OnPhaseStarted;
        GameEvents.OnMatchEnded -= OnMatchEnded;
    }

    // ---------------- construção ----------------
    void BuildHud()
    {
        var canvas = UIKit.CreateCanvas("Canvas", 10);
        canvas.transform.SetParent(transform, false);
        var hud = new GameObject("HUD", typeof(RectTransform));
        hud.transform.SetParent(canvas.transform, false);
        UIKit.Stretch(hud.GetComponent<RectTransform>());

        for (int p = 0; p < 2; p++)
        {
            bool left = p == 0;
            Color pc = GameRules.PlayerColor(p);
            var panel = UIKit.Panel(hud.transform, "Jogador" + (p + 1), Palette.A(Palette.Painel, 0.85f));
            UIKit.Place(panel.rectTransform, new Vector2(left ? 0f : 1f, 1f), new Vector2(left ? 30f : -30f, -30f), new Vector2(440f, 140f));
            panelOutline[p] = panel.gameObject.AddComponent<Outline>();
            panelOutline[p].effectColor = Palette.A(pc, 0.4f);
            panelOutline[p].effectDistance = new Vector2(3f, -3f);

            var av = UIKit.NewImage(panel.transform, "Avatar", Tex.Circle(), pc);
            UIKit.Place(av.rectTransform, new Vector2(left ? 0f : 1f, 0.5f), new Vector2(left ? 16f : -16f, 0f), new Vector2(96f, 96f));
            var num = UIKit.Label(av.transform, "Num", (p + 1).ToString(), 62, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.Stretch(num.rectTransform);

            names[p] = UIKit.Label(panel.transform, "Nome", GameRules.PlayerName(p), 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.Place(names[p].rectTransform, new Vector2(0.5f, 1f), new Vector2(left ? 65f : -65f, -14f), new Vector2(300f, 54f));
            names[p].fontSize = 38;

            dots[p] = new Image[7];
            for (int i = 0; i < 7; i++)
            {
                var d = UIKit.NewImage(panel.transform, "Bola" + i, Tex.Circle(), Palette.A(Color.white, 0.18f));
                float x = (left ? 138f : -138f) + (left ? 1 : -1) * i * 44f;
                UIKit.Place(d.rectTransform, new Vector2(left ? 0f : 1f, 0f), new Vector2(x, 22f), new Vector2(30f, 30f));
                d.rectTransform.pivot = new Vector2(left ? 0f : 1f, 0f);
                dots[p][i] = d;
            }
        }

        phaseText = UIKit.Label(hud.transform, "Fase", "", 48, Palette.HotPink, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic, true);
        UIKit.Place(phaseText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(760f, 64f));
        phaseText.rectTransform.pivot = new Vector2(0.5f, 1f);
        scoreText = UIKit.Label(hud.transform, "Serie", "", 30, Palette.Ciano, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(scoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(760f, 46f));
        scoreText.rectTransform.pivot = new Vector2(0.5f, 1f);
        ruleText = UIKit.Label(hud.transform, "Regra", "", 24, Color.white, TextAnchor.UpperCenter, FontStyle.Italic, true);
        UIKit.Place(ruleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -136f), new Vector2(640f, 70f));
        ruleText.rectTransform.pivot = new Vector2(0.5f, 1f);
        ruleText.horizontalOverflow = HorizontalWrapMode.Wrap;

        // barra de força (vertical)
        var pBox = UIKit.Panel(hud.transform, "BarraForca", Palette.A(Palette.Painel, 0.85f));
        UIKit.Place(pBox.rectTransform, new Vector2(1f, 0.5f), new Vector2(-50f, 0f), new Vector2(84f, 460f));
        pBox.rectTransform.pivot = new Vector2(1f, 0.5f);
        var pOl = pBox.gameObject.AddComponent<Outline>();
        pOl.effectColor = Palette.A(Palette.Ciano, 0.8f); pOl.effectDistance = new Vector2(3f, -3f);
        powerFill = UIKit.NewImage(pBox.transform, "Preenchimento", Tex.VerticalGradient("power", Palette.Ciano, Palette.HotPink), Color.white);
        powerFill.rectTransform.anchorMin = new Vector2(0f, 0f); powerFill.rectTransform.anchorMax = new Vector2(1f, 1f);
        powerFill.rectTransform.offsetMin = new Vector2(14f, 14f); powerFill.rectTransform.offsetMax = new Vector2(-14f, -14f);
        powerFill.type = Image.Type.Filled;
        powerFill.fillMethod = Image.FillMethod.Vertical;
        powerFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        powerFill.fillAmount = 0f;
        var pl = UIKit.Label(hud.transform, "ForcaTxt", "FORÇA", 28, Palette.Ciano, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(pl.rectTransform, new Vector2(1f, 0.5f), new Vector2(-50f, 260f), new Vector2(120f, 40f));
        pl.rectTransform.pivot = new Vector2(1f, 0.5f);


        // mensagem central
        msgText = UIKit.Label(hud.transform, "Mensagem", "", 46, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(msgText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 240f), new Vector2(1500f, 80f));
        msgText.rectTransform.pivot = new Vector2(0.5f, 0f);
        msgText.color = new Color(1f, 1f, 1f, 0f);

        hintText = UIKit.Label(hud.transform, "Dica", "", 34, Palette.Dourado, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1500f, 60f));
        hintText.rectTransform.pivot = new Vector2(0.5f, 0f);
        chosenText = UIKit.Label(hud.transform, "Escolhida", "", 30, Palette.Ciano, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(chosenText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(900f, 50f));
        chosenText.rectTransform.pivot = new Vector2(0.5f, 0f);
    }

    GameObject Overlay(string name, string title, Color titleColor, out Image box)
    {
        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(transform.GetChild(0), false);
        UIKit.Stretch(root.GetComponent<RectTransform>());
        var dim = UIKit.Panel(root.transform, "Escurecer", new Color(0.02f, 0f, 0.06f, 0.78f), false);
        UIKit.Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        box = UIKit.Panel(root.transform, "Caixa", Palette.A(Palette.Painel, 0.96f));
        UIKit.Place(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 700f));
        var ol = box.gameObject.AddComponent<Outline>();
        ol.effectColor = Palette.A(titleColor, 0.9f); ol.effectDistance = new Vector2(3f, -3f);
        var t = UIKit.Label(box.transform, "Titulo", title, 92, titleColor, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic, true);
        UIKit.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(720f, 120f));
        t.rectTransform.pivot = new Vector2(0.5f, 1f);
        return root;
    }

    void BuildPause()
    {
        Image box;
        pausePanel = Overlay("Pausa", "Pausado", Palette.HotPink, out box);
        pauseBox = box.gameObject;
        UIKit.NeonButton(box.transform, "Continuar", new Vector2(0f, 80f), new Vector2(520f, 92f), Palette.HotPink, () => gsm.SetPaused(false), 40);
        UIKit.NeonButton(box.transform, "Configurações", new Vector2(0f, -30f), new Vector2(520f, 92f), Palette.Ciano, () => { pauseBox.SetActive(false); settingsPanel.SetActive(true); }, 40);
        UIKit.NeonButton(box.transform, "Menu Principal", new Vector2(0f, -140f), new Vector2(520f, 92f), Palette.Violeta, () => gsm.GoToMenu(), 40);
        UIKit.NeonButton(box.transform, "Sair do Jogo", new Vector2(0f, -250f), new Vector2(520f, 92f), Palette.Laranja, () => gsm.QuitGame(), 40);
        pausePanel.SetActive(false);

        settingsPanel = SettingsPanel.Build(pausePanel.transform, () => { settingsPanel.SetActive(false); pauseBox.SetActive(true); });
    }

    void BuildEnd()
    {
        Image box;
        endPanel = Overlay("FimDeJogo", "Fim de Jogo!", Palette.HotPink, out box);
        endWinner = UIKit.Label(box.transform, "Vencedor", "", 64, Palette.Ciano, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(endWinner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(720f, 90f));
        endScore = UIKit.Label(box.transform, "Placar", "", 44, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, false);
        UIKit.Place(endScore.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(720f, 70f));
        UIKit.NeonButton(box.transform, "Jogar Novamente", new Vector2(0f, -110f), new Vector2(520f, 92f), Palette.HotPink, () => { endPanel.SetActive(false); gsm.RestartMatch(); }, 40);
        UIKit.NeonButton(box.transform, "Menu Principal", new Vector2(0f, -230f), new Vector2(520f, 92f), Palette.Ciano, () => gsm.GoToMenu(), 40);
        endPanel.SetActive(false);
    }

    void BuildBanner()
    {
        bannerPanel = new GameObject("AvisoFase", typeof(RectTransform));
        bannerPanel.transform.SetParent(transform.GetChild(0), false);
        var bg = UIKit.Panel(bannerPanel.transform, "Fundo", Palette.A(Palette.Fundo, 0.88f));
        UIKit.Place(bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1200f, 260f));
        var ol = bg.gameObject.AddComponent<Outline>();
        ol.effectColor = Palette.A(Palette.Dourado, 0.9f); ol.effectDistance = new Vector2(3f, -3f);
        bannerText = UIKit.Label(bg.transform, "Texto", "", 70, Palette.Dourado, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic, true);
        UIKit.Place(bannerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1160f, 110f));
        bannerSub = UIKit.Label(bg.transform, "Sub", "", 38, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, false);
        UIKit.Place(bannerSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(1160f, 60f));
        bannerPanel.SetActive(false);
    }

    // ---------------- eventos ----------------
    void Refresh()
    {
        if (gsm == null) gsm = GameStateMachine.Instance;
        if (reg == null) reg = BallRegistry.Instance;
        if (gsm == null || reg == null) return;
        var cfg = gsm.CurrentPhase;
        phaseText.text = cfg.displayName + " - " + cfg.difficulty;
        phaseText.color = cfg.neonColor;
        scoreText.text = "Série: " + gsm.Wins[0] + " x " + gsm.Wins[1] + "   (vence quem ganhar 2 fases)";
        ruleText.text = cfg.ruleText;

        Color[] ballCol = { Palette.BolaAzul, Palette.BolaVermelha };
        for (int p = 0; p < 2; p++)
        {
            int kept = reg.CountKept(GameRules.OwnType(p));
            for (int i = 0; i < 6; i++) dots[p][i].color = i < kept ? ballCol[p] : Palette.A(Color.white, 0.18f);
            bool goldKept = reg.goldBall != null && !reg.goldBall.onTable && reg.goldBall.keptPocketed && gsm.CurrentPlayer == p && gsm.StateId == GameStateId.PhaseEnd;
            dots[p][6].color = goldKept ? Palette.Dourado : Palette.A(Palette.Dourado, 0.22f);
            bool turn = gsm.CurrentPlayer == p;
            panelOutline[p].effectColor = turn ? Palette.A(GameRules.PlayerColor(p), 1f) : Palette.A(GameRules.PlayerColor(p), 0.2f);
            names[p].text = GameRules.PlayerName(p) + (turn ? "  ◄" : "");
            if (p == 1) names[p].text = (turn ? "►  " : "") + GameRules.PlayerName(p);
        }
    }

    void ShowMsg(string s) { msgText.text = s; msgColor = Color.white; msgTimer = 2.4f; }
    void ShowFoul(string s) { msgText.text = s; msgColor = Palette.HotPink; msgTimer = 3f; }
    void OnPower(float v) { power = v; }
    void OnPause(bool p)
    {
        pausePanel.SetActive(p);
        pauseBox.SetActive(true);
        settingsPanel.SetActive(false);
    }
    void OnPhaseEnded(int winner, int idx)
    {
        bannerText.text = GameRules.PlayerName(winner) + " venceu a " + gsm.phases[idx].displayName + "!";
        bannerSub.text = "Série: " + gsm.Wins[0] + " x " + gsm.Wins[1];
        bannerPanel.SetActive(true);
        Refresh();
    }
    void OnPhaseStarted(int idx) { bannerPanel.SetActive(false); endPanel.SetActive(false); Refresh(); }
    void OnMatchEnded(int winner)
    {
        bannerPanel.SetActive(false);
        endWinner.text = GameRules.PlayerName(winner) + " venceu!";
        endScore.text = "Placar da série: " + gsm.Wins[0] + " x " + gsm.Wins[1];
        endPanel.SetActive(true);
    }

    void Update()
    {
        if (gsm == null) gsm = GameStateMachine.Instance;
        if (gsm == null) return;

        powerShown = Mathf.Lerp(powerShown, power, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
        powerFill.fillAmount = powerShown;

        if (msgTimer > 0f)
        {
            msgTimer -= Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(msgTimer / 0.5f);
            msgText.color = new Color(msgColor.r, msgColor.g, msgColor.b, a);
        }

        var st = gsm.StateId;
        bool choosing = (st == GameStateId.Aiming || st == GameStateId.Charging) && gsm.NeedsPocketChoice;
        hintText.text = choosing && gsm.ChosenPocket == 0 ? "Escolha a caçapa" :
                        (st == GameStateId.Placing ? "Posicione a bola branca" : "");
        chosenText.text = choosing && gsm.ChosenPocket > 0 ? "Caçapa escolhida: " + gsm.ChosenPocket : "";
    }
}
