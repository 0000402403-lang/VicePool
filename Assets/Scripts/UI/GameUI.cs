using UnityEngine;
using UnityEngine.UI;

/// <summary>HUD, pausa, configurações, aviso de fase e tela de Fim de Jogo. Só escuta os eventos do jogo.</summary>
public class GameUI : MonoBehaviour
{
    GameStateMachine gsm;
    BallRegistry reg;

    Image[][] dots = new Image[2][];
    Outline[][] dotOutlines = new Outline[2][];
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
            var panel = UIKit.Panel(hud.transform, "Jogador" + (p + 1), Palette.A(Palette.Painel, 0.95f));
            UIKit.Place(panel.rectTransform, new Vector2(left ? 0f : 1f, 1f), new Vector2(left ? 30f : -30f, -30f), new Vector2(520f, 150f));
            panelOutline[p] = panel.gameObject.AddComponent<Outline>();
            panelOutline[p].effectColor = Palette.A(pc, 0.4f);
            panelOutline[p].effectDistance = new Vector2(3f, -3f);

            var av = UIKit.NewImage(panel.transform, "Avatar", Tex.Circle(), pc);
            UIKit.Place(av.rectTransform, new Vector2(left ? 0f : 1f, 0.5f), new Vector2(left ? 18f : -18f, 0f), new Vector2(104f, 104f));
            var num = UIKit.Label(av.transform, "Num", (p + 1).ToString(), 62, Palette.Fundo, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            UIKit.Stretch(num.rectTransform);

            names[p] = UIKit.Label(panel.transform, "Nome", GameRules.PlayerName(p), 40, Color.white, left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, FontStyle.Bold, true);
            UIKit.Place(names[p].rectTransform, new Vector2(left ? 0f : 1f, 1f), new Vector2(left ? 144f : -144f, -14f), new Vector2(350f, 54f));
            names[p].rectTransform.pivot = new Vector2(left ? 0f : 1f, 1f);

            dots[p] = new Image[7];
            dotOutlines[p] = new Outline[7];
            for (int i = 0; i < 7; i++)
            {
<<<<<<< Updated upstream
                var d = UIKit.NewImage(panel.transform, "Bola" + i, Tex.Circle(), Palette.A(Color.white, 0.18f));
                float x = (left ? 144f : -144f) + (left ? 1 : -1) * i * 50f;
                UIKit.Place(d.rectTransform, new Vector2(left ? 0f : 1f, 0f), new Vector2(x, 24f), new Vector2(34f, 34f));
=======
                var d = UIKit.NewImage(panel.transform, "Bola" + i, Tex.Circle(), Palette.A(Color.white, 0.30f));
                float x = (left ? 144f : -144f) + (left ? 1 : -1) * i * 52f;
                UIKit.Place(d.rectTransform, new Vector2(left ? 0f : 1f, 0f), new Vector2(x, 22f), new Vector2(40f, 40f));
>>>>>>> Stashed changes
                d.rectTransform.pivot = new Vector2(left ? 0f : 1f, 0f);
                var o = d.gameObject.AddComponent<Outline>();
                o.effectDistance = new Vector2(3f, -3f);
                o.effectColor = Palette.A(Color.white, 0.6f);
                dots[p][i] = d;
                dotOutlines[p][i] = o;
            }
        }

        phaseText = UIKit.Label(hud.transform, "Fase", "", 48, Palette.HotPink, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic, true);
        UIKit.Place(phaseText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(760f, 64f));
        phaseText.rectTransform.pivot = new Vector2(0.5f, 1f);
        scoreText = UIKit.Label(hud.transform, "Serie", "", 34, Palette.Ciano, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(scoreText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(760f, 46f));
        scoreText.rectTransform.pivot = new Vector2(0.5f, 1f);
        ruleText = UIKit.Label(hud.transform, "Regra", "", 26, Palette.A(Color.white, 0.75f), TextAnchor.MiddleCenter, FontStyle.Italic, false);
        UIKit.Place(ruleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(760f, 40f));
        ruleText.rectTransform.pivot = new Vector2(0.5f, 1f);

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

        // legenda de controles
        var leg = UIKit.Panel(hud.transform, "Legenda", Palette.A(Palette.Painel, 0.78f));
        UIKit.Place(leg.rectTransform, new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(430f, 170f));
        var lt = UIKit.Label(leg.transform, "Texto",
            "Mouse: ajustar mira\nSegurar clique: alterar força\nSoltar: dar tacada\nQ/E: girar câmera   Esc: pausa", 26,
            Color.white, TextAnchor.MiddleLeft, FontStyle.Normal, false);
        UIKit.Stretch(lt.rectTransform);
        lt.rectTransform.offsetMin = new Vector2(22f, 0f);

        // mensagem central
        msgText = UIKit.Label(hud.transform, "Mensagem", "", 46, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(msgText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 240f), new Vector2(1500f, 80f));
        msgText.rectTransform.pivot = new Vector2(0.5f, 0f);
        msgText.color = new Color(1f, 1f, 1f, 0f);

        hintText = UIKit.Label(hud.transform, "Dica", "", 34, Palette.Dourado, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1500f, 60f));
        hintText.rectTransform.pivot = new Vector2(0.5f, 0f);
        chosenText = UIKit.Label(hud.transform, "Escolhida", "", 34, Palette.Ciano, TextAnchor.MiddleCenter, FontStyle.Bold, true);
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

        // versões mais claras das bolas para contrastar com o painel escuro
        Color[] ballCol = { Palette.H("5B8CFF"), Palette.H("FF4D63") };
        for (int p = 0; p < 2; p++)
        {
            int kept = reg.CountKept(GameRules.OwnType(p));
            for (int i = 0; i < 6; i++) SetDot(p, i, i < kept, ballCol[p]);
            bool goldKept = reg.goldBall != null && !reg.goldBall.onTable && reg.goldBall.keptPocketed && gsm.CurrentPlayer == p && gsm.StateId == GameStateId.PhaseEnd;
            SetDot(p, 6, goldKept, Palette.Dourado);
            bool turn = gsm.CurrentPlayer == p;
            panelOutline[p].effectColor = turn ? Palette.A(GameRules.PlayerColor(p), 1f) : Palette.A(GameRules.PlayerColor(p), 0.2f);
            names[p].text = GameRules.PlayerName(p) + (turn ? "  ◄" : "");
            if (p == 1) names[p].text = (turn ? "►  " : "") + GameRules.PlayerName(p);
        }
    }

    // bola encaçapada = cor cheia com contorno branco; vazia = cinza claro translúcido com contorno fino
    void SetDot(int p, int i, bool filled, Color full)
    {
        dots[p][i].color = filled ? full : Palette.A(Color.white, 0.22f);
        dotOutlines[p][i].effectColor = filled ? Color.white : Palette.A(Color.white, 0.55f);
        dotOutlines[p][i].effectDistance = filled ? new Vector2(4f, -4f) : new Vector2(2.5f, -2.5f);
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
        hintText.text = choosing && gsm.ChosenPocket == 0 ? "Escolha a caçapa: botão direito nela ou teclas 1 a 6" :
                        (st == GameStateId.Placing ? "Mova o mouse e clique para posicionar a bola branca" : "");
        chosenText.text = choosing && gsm.ChosenPocket > 0 ? "Caçapa escolhida: " + gsm.ChosenPocket : "";
    }
}
