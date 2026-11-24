using UnityEngine;

/// <summary>State Pattern: cada estado da partida (mira, força, simulação, avaliação...).</summary>
public abstract class GameState
{
    protected GameStateMachine m;
    protected GameState(GameStateMachine machine) { m = machine; }
    public abstract GameStateId Id { get; }
    public virtual void Enter() { }
    public virtual void Tick() { }
    public virtual void FixedTick() { }
    public virtual void Exit() { }
}

public class PlacingState : GameState
{
    public PlacingState(GameStateMachine mm) : base(mm) { }
    public override GameStateId Id { get { return GameStateId.Placing; } }
    public override void Enter() { m.OnEnterPlacing(); }
    public override void Tick() { m.TickPlacing(); }
    public override void Exit() { m.OnExitPlacing(); }
}

public class AimingState : GameState
{
    public AimingState(GameStateMachine mm) : base(mm) { }
    public override GameStateId Id { get { return GameStateId.Aiming; } }
    public override void Enter() { m.OnEnterAiming(); }
}

public class ChargingState : GameState
{
    public ChargingState(GameStateMachine mm) : base(mm) { }
    public override GameStateId Id { get { return GameStateId.Charging; } }
}

public class BallsMovingState : GameState
{
    float timer;
    public BallsMovingState(GameStateMachine mm) : base(mm) { }
    public override GameStateId Id { get { return GameStateId.BallsMoving; } }
    public override void Enter() { timer = 0f; }
    public override void FixedTick()
    {
        timer += Time.fixedDeltaTime;
        // o turno só muda depois que TODAS as bolas pararem
        if (timer > 0.3f && m.Registry.Settled) m.Change(m.SEvaluating);
        else if (timer > 25f) { m.Registry.StopAll(); m.Change(m.SEvaluating); }
    }
}

public class EvaluatingState : GameState
{
    public EvaluatingState(GameStateMachine mm) : base(mm) { }
    public override GameStateId Id { get { return GameStateId.Evaluating; } }
    public override void Enter() { m.EvaluateShot(); }
}

public class TurnEndState : GameState
{
    float timer;
    public TurnEndState(GameStateMachine mm) : base(mm) { }
    public override GameStateId Id { get { return GameStateId.TurnEnd; } }
    public override void Enter() { timer = m.TurnEndDelay; }
    public override void Tick()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f) m.AfterTurnEnd();
    }
}

public class PhaseEndState : GameState
{
    float timer;
    public PhaseEndState(GameStateMachine mm) : base(mm) { }
    public override GameStateId Id { get { return GameStateId.PhaseEnd; } }
    public override void Enter() { timer = 3.2f; }
    public override void Tick()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f) m.AfterPhaseEnd();
    }
}

public class MatchEndState : GameState
{
    public MatchEndState(GameStateMachine mm) : base(mm) { }
    public override GameStateId Id { get { return GameStateId.MatchEnd; } }
    public override void Enter() { m.OnEnterMatchEnd(); }
}
