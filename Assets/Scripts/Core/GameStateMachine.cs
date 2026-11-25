using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Máquina de estados da partida: turnos, fases, placar da série (2 de 3), pausa e fim de jogo.</summary>
public class GameStateMachine : MonoBehaviour
{
    public static GameStateMachine Instance;

    public PhaseConfig[] phases;
    public BallRegistry Registry;
    public RuleReferee Referee;

    // estado da série
    public int CurrentPlayer { get; private set; }
    public int PhaseIndex { get; private set; }
    public int[] Wins = new int[2];
    public int ChosenPocket { get; private set; }
    public bool Paused { get; private set; }
    public int MatchWinner { get; private set; } = -1;
    public GameStateId StateId { get { return current != null ? current.Id : GameStateId.None; } }
    public float TurnEndDelay { get; private set; } = 0.6f;
    public PhaseConfig CurrentPhase { get { return phases[Mathf.Clamp(PhaseIndex, 0, phases.Length - 1)]; } }

    // estados
    public GameState SPlacing, SAiming, SCharging, SBallsMoving, SEvaluating, STurnEnd, SPhaseEnd, SMatchEnd;
    GameState current;

    int starter;
    int ownLeftAtStart;
    bool needPlacement;
    bool placementValid;
    int lastPhaseWinner;

    public bool NeedsPocketChoice
    {
        get
        {
            var cfg = CurrentPhase;
            if (cfg.requireChosenPocket) return true;
            return cfg.goldChoosesPocket && Registry.CountOnTable(GameRules.OwnType(CurrentPlayer)) == 0;
        }
    }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        if (Registry == null) Registry = FindFirstObjectByType<BallRegistry>();
        if (Referee == null) Referee = FindFirstObjectByType<RuleReferee>();
        if (phases == null || phases.Length == 0)
            phases = Resources.LoadAll<PhaseConfig>("Phases").OrderBy(p => p.order).ToArray();
        if (phases.Length == 0) phases = BuildDefaultPhases();

        SPlacing = new PlacingState(this);
        SAiming = new AimingState(this);
        SCharging = new ChargingState(this);
        SBallsMoving = new BallsMovingState(this);
        SEvaluating = new EvaluatingState(this);
        STurnEnd = new TurnEndState(this);
        SPhaseEnd = new PhaseEndState(this);
        SMatchEnd = new MatchEndState(this);
    }

    static PhaseConfig[] BuildDefaultPhases()
    {
        var a = ScriptableObject.CreateInstance<PhaseConfig>();
        a.order = 1; a.displayName = "Fase 1"; a.difficulty = "Fácil"; a.feltColor = Palette.Magenta; a.neonColor = Palette.HotPink; a.goldChoosesPocket = true;
        var b = ScriptableObject.CreateInstance<PhaseConfig>();
        b.order = 2; b.displayName = "Fase 2"; b.difficulty = "Médio"; b.feltColor = Palette.Turquesa; b.neonColor = Palette.Ciano; b.requireChosenPocket = true;
        var c = ScriptableObject.CreateInstance<PhaseConfig>();
        c.order = 3; c.displayName = "Fase 3"; c.difficulty = "Difícil"; c.feltColor = Palette.Roxo; c.neonColor = Palette.Violeta; c.requireChosenPocket = true; c.requireCushion = true;
        return new[] { a, b, c };
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    void Start() { StartMatch(); }

    // ---------------- fluxo ----------------
    public void Change(GameState next)
    {
        if (current != null) current.Exit();
        current = next;
        current.Enter();
        GameEvents.RaiseStateChanged(current.Id);
        GameEvents.RaiseHud();
    }

    public void StartMatch()
    {
        Time.timeScale = 1f;
        Paused = false;
        Wins[0] = 0; Wins[1] = 0;
        MatchWinner = -1;
        starter = 0;
        StartPhase(0);
    }

    public void StartPhase(int idx)
    {
        PhaseIndex = idx;
        CurrentPlayer = starter;
        ChosenPocket = 0;
        needPlacement = false;
        Registry.ResetAll();
        if (TableVisuals.Instance != null) TableVisuals.Instance.Apply(CurrentPhase);
        GameEvents.RaisePhaseStarted(idx);
        GameEvents.RaiseMessage(CurrentPhase.displayName + " (" + CurrentPhase.difficulty + ") - " + GameRules.PlayerName(CurrentPlayer) + " começa");
        Change(SAiming);
    }

    public void OnEnterAiming()
    {
        if (!Registry.cueBall.onTable) Registry.ReturnToTable(Registry.cueBall);
        Registry.HighlightPocket(ChosenPocket);
        if (NeedsPocketChoice && ChosenPocket == 0)
            GameEvents.RaiseMessage("Escolha a caçapa");
    }

    public void SelectPocket(int idx)
    {
        if (StateId != GameStateId.Aiming) return;
        if (idx < 1 || idx > 6) return;
        ChosenPocket = idx;
        Registry.HighlightPocket(idx);
        GameEvents.RaiseMessage("Caçapa " + idx + " escolhida");
        GameEvents.RaiseHud();
    }

    public bool BeginCharging()
    {
        if (StateId != GameStateId.Aiming || Paused) return false;
        if (NeedsPocketChoice && ChosenPocket == 0)
        {
            GameEvents.RaiseMessage("Escolha a caçapa antes da tacada!");
            return false;
        }
        Change(SCharging);
        return true;
    }

    public void CancelCharging()
    {
        if (StateId == GameStateId.Charging) Change(SAiming);
    }

    public void Shoot(Vector3 dir, float power01)
    {
        if (StateId != GameStateId.Charging) return;
        ownLeftAtStart = Registry.CountOnTable(GameRules.OwnType(CurrentPlayer));
        Registry.BeginShot();
        Registry.cueBall.GetComponent<CueBallController>().Shoot(dir, power01);
        GameEvents.RaiseShot(power01);
        Change(SBallsMoving);
    }

    void FixedUpdate() { if (current != null && !Paused) current.FixedTick(); }

void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) TogglePause();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (kb != null && !Paused) DebugKeys(kb);
#endif
        if (current != null && !Paused) current.Tick();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>Atalhos de teste (só no Editor / build de desenvolvimento): F1 só falta a dourada, F2 vence a fase, F3 vai para a Fase 3 com 1 x 1.</summary>
    void DebugKeys(Keyboard kb)
    {
        if (StateId != GameStateId.Aiming) return;
        if (kb.f1Key.wasPressedThisFrame)
        {
            var own = GameRules.OwnType(CurrentPlayer);
            foreach (var b in Registry.all)
                if (b.type == own && b.onTable) { b.onTable = false; b.keptPocketed = true; b.gameObject.SetActive(false); }
            GameEvents.RaiseMessage("[TESTE] Só falta a bola dourada");
            GameEvents.RaiseHud();
        }
        else if (kb.f2Key.wasPressedThisFrame)
        {
            GameEvents.RaiseMessage("[TESTE] Fase vencida");
            EndPhase(CurrentPlayer);
        }
        else if (kb.f3Key.wasPressedThisFrame)
        {
            Wins[0] = 1; Wins[1] = 1; MatchWinner = -1;
            GameEvents.RaiseMessage("[TESTE] Série 1 x 1 - Fase 3");
            StartPhase(phases.Length - 1);
        }
    }
#endif

    // ---------------- avaliação ----------------
    public void EvaluateShot()
    {
        var res = Referee.Evaluate(CurrentPlayer, CurrentPhase, ChosenPocket, ownLeftAtStart, Registry.cueBall, Registry.shotPockets);

        foreach (var b in res.toReturn) Registry.ReturnToTable(b);
        needPlacement = false;
        if (res.cueReturn)
        {
            Registry.cueBall.PlaceAt(Registry.FindFreeSpot(Registry.cueBall.startPosition, Registry.cueBall));
            needPlacement = true;
        }
        ChosenPocket = 0;
        Registry.HighlightPocket(0);

        if (res.message != null)
        {
            if (res.foul) GameEvents.RaiseFoul(res.message);
            else GameEvents.RaiseMessage(res.message);
        }

        if (res.phaseWon)
        {
            EndPhase(CurrentPlayer);
            return;
        }

        TurnEndDelay = res.message != null ? 1.4f : 0.5f;
        if (!res.keepTurn)
        {
            CurrentPlayer = 1 - CurrentPlayer;
            GameEvents.RaiseTurnEnded(CurrentPlayer);
        }
        else
        {
            GameEvents.RaiseMessage("Boa! " + GameRules.PlayerName(CurrentPlayer) + " joga de novo");
        }
        Change(STurnEnd);
    }

    public void AfterTurnEnd()
    {
        if (needPlacement) Change(SPlacing);
        else Change(SAiming);
    }

    void EndPhase(int winner)
    {
        Wins[winner]++;
        lastPhaseWinner = winner;
        starter = 1 - winner;                  // quem perdeu a fase começa a próxima
        GameEvents.RaisePhaseEnded(winner, PhaseIndex);
        Change(SPhaseEnd);
    }

    public void AfterPhaseEnd()
    {
        bool over = Wins[0] >= 2 || Wins[1] >= 2 || PhaseIndex >= phases.Length - 1;
        if (over)
        {
            MatchWinner = Wins[0] > Wins[1] ? 0 : 1;
            Change(SMatchEnd);
        }
        else StartPhase(PhaseIndex + 1);
    }

    public void OnEnterMatchEnd()
    {
        GameEvents.RaiseMatchEnded(MatchWinner);
    }

    // ---------------- colocar a branca (após falta) ----------------
    public void OnEnterPlacing()
    {
        var cue = Registry.cueBall;
        cue.rb.isKinematic = true;
        placementValid = true;
        GameEvents.RaiseMessage(GameRules.PlayerName(CurrentPlayer) + ": posicione a bola branca");
    }

    public void OnExitPlacing()
    {
        var cue = Registry.cueBall;
        cue.rb.isKinematic = false;
        cue.rb.linearVelocity = Vector3.zero;
        needPlacement = false;
    }

    public void TickPlacing()
    {
        var cam = Camera.main;
        var mouse = Mouse.current;
        if (cam == null || mouse == null || Paused) return;
        var cue = Registry.cueBall;
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        var plane = new Plane(Vector3.up, new Vector3(0f, BallController.Radius, 0f));
        float d;
        if (plane.Raycast(ray, out d))
        {
            Vector3 p = ray.GetPoint(d);
            p.y = BallController.Radius;
            bool ok = TableGeometry.Inside(p, 0.09f) && Registry.IsFree(p, cue, 0.012f);
            placementValid = ok;
            if (ok)
            {
                cue.transform.position = p;
                cue.rb.position = p;
            }
        }
        if (mouse.leftButton.wasPressedThisFrame && placementValid) Change(SAiming);
    }

    // ---------------- pausa ----------------
    public void TogglePause() { SetPaused(!Paused); }

    public void SetPaused(bool p)
    {
        if (StateId == GameStateId.MatchEnd) return;
        Paused = p;
        Time.timeScale = p ? 0f : 1f;
        GameEvents.RaisePause(p);
    }

    public void RestartMatch()
    {
        Paused = false;
        Time.timeScale = 1f;
        GameEvents.RaisePause(false);
        StartMatch();
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
