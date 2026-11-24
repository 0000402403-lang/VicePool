using System.Collections.Generic;
using UnityEngine;

public class ShotResult
{
    public bool foul;
    public bool keepTurn;
    public bool phaseWon;
    public string message;
    public bool cueReturn;
    public List<BallController> toReturn = new List<BallController>();
}

/// <summary>Valida a jogada: falta, caçapa escolhida, tabela, bola dourada, bolas do adversário.</summary>
public class RuleReferee : MonoBehaviour
{
    public ShotResult Evaluate(int player, PhaseConfig cfg, int chosenPocket, int ownLeftAtStart,
                               BallController cue, List<BallRegistry.PocketRecord> rec)
    {
        var r = new ShotResult();
        BallType own = GameRules.OwnType(player);
        BallType opp = GameRules.OppType(player);

        bool foul = false;
        string msg = null;
        bool validOwn = false, opponentPocketed = false, goldWin = false;
        BallController gold = null;

        // --- faltas de contato ---
        if (!cue.onTable) { foul = true; r.cueReturn = true; msg = "Falta: bola branca encaçapada!"; }
        else if (!cue.hasFirstHit) { foul = true; msg = "Falta: a branca não tocou em nenhuma bola!"; }
        else if (cue.firstHitType == opp) { foul = true; msg = "Falta: primeiro contato na bola do adversário!"; }

        // --- bolas encaçapadas ---
        foreach (var p in rec)
        {
            var b = p.ball;
            if (b.type == BallType.Cue) { foul = true; r.cueReturn = true; if (msg == null || !msg.StartsWith("Falta: bola branca")) msg = "Falta: bola branca encaçapada!"; continue; }

            bool isGold = b.type == BallType.Gold;
            string why;
            bool ok = PocketOk(cfg, chosenPocket, p, isGold, out why);

            if (b.type == own)
            {
                if (ok) { b.keptPocketed = true; validOwn = true; }
                else { r.toReturn.Add(b); if (msg == null) msg = why; }
            }
            else if (b.type == opp)
            {
                b.keptPocketed = true;
                opponentPocketed = true;
                if (msg == null) msg = "Bola do adversário encaçapada!";
            }
            else if (isGold)
            {
                gold = b;
                if (ownLeftAtStart > 0) { r.toReturn.Add(b); if (msg == null) msg = "Bola dourada antes da hora! Volta ao centro."; }
                else if (ok) { b.keptPocketed = true; goldWin = true; }
                else { r.toReturn.Add(b); if (msg == null) msg = why; }
            }
        }

        if (foul && goldWin)
        {
            gold.keptPocketed = false;
            r.toReturn.Add(gold);
            goldWin = false;
        }

        r.foul = foul;
        r.phaseWon = goldWin;
        r.keepTurn = !foul && validOwn && !opponentPocketed && !goldWin;
        r.message = msg;
        return r;
    }

    bool PocketOk(PhaseConfig cfg, int chosen, BallRegistry.PocketRecord p, bool isGold, out string why)
    {
        why = null;
        bool needChoice = cfg.requireChosenPocket || (isGold && cfg.goldChoosesPocket);
        if (needChoice && p.pocket != chosen) { why = "Caçapa errada! A bola volta à mesa."; return false; }
        if (cfg.requireCushion && !p.cushion) { why = "Sem tabela! A bola volta à mesa."; return false; }
        return true;
    }
}
