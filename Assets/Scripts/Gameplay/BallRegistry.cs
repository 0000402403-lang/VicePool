using System.Collections.Generic;
using UnityEngine;

/// <summary>Guarda todas as bolas e caçapas, detecta "bolas paradas" e acha posições livres.</summary>
public class BallRegistry : MonoBehaviour
{
    public static BallRegistry Instance;

    public class PocketRecord
    {
        public BallController ball;
        public int pocket;
        public bool cushion;
    }

    public List<BallController> all = new List<BallController>();
    public BallController cueBall, goldBall;
    public PocketTrigger[] pockets = new PocketTrigger[6];
    public List<PocketRecord> shotPockets = new List<PocketRecord>();

    int stillFrames;
    public bool Settled { get { return stillFrames >= 12; } }

    void Awake()
    {
        Instance = this;
        Collect();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    public void Collect()
    {
        all.Clear();
        cueBall = null; goldBall = null;
        foreach (var b in FindObjectsByType<BallController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            all.Add(b);
            if (b.type == BallType.Cue) cueBall = b;
            if (b.type == BallType.Gold) goldBall = b;
        }
        foreach (var p in FindObjectsByType<PocketTrigger>(FindObjectsSortMode.None))
            if (p.index >= 1 && p.index <= 6) pockets[p.index - 1] = p;
    }

    void FixedUpdate()
    {
        bool moving = false;
        for (int i = 0; i < all.Count; i++)
        {
            var b = all[i];
            if (b.onTable && b.rb != null && b.rb.linearVelocity.sqrMagnitude > BallController.StopSpeed * BallController.StopSpeed) { moving = true; break; }
        }
        if (moving) stillFrames = 0; else stillFrames++;
    }

    public void BeginShot()
    {
        shotPockets.Clear();
        stillFrames = 0;
        foreach (var b in all) b.BeginShot();
    }

    public void OnPocketed(BallController b, PocketTrigger p)
    {
        if (!b.onTable) return;
        shotPockets.Add(new PocketRecord { ball = b, pocket = p.index, cushion = b.hitCushionThisShot });
        b.Pocket(p.index, p.transform.position);
        GameEvents.RaiseBallPocketed(b, p.index);
    }

    public int CountOnTable(BallType t)
    {
        int n = 0;
        foreach (var b in all) if (b.type == t && b.onTable) n++;
        return n;
    }

    /// <summary>Bolas daquela cor que já foram encaçapadas (e ficaram encaçapadas).</summary>
    public int CountKept(BallType t)
    {
        int n = 0;
        foreach (var b in all) if (b.type == t && !b.onTable && b.keptPocketed) n++;
        return n;
    }

    public void ResetAll()
    {
        shotPockets.Clear();
        foreach (var b in all) b.PlaceAt(b.startPosition);
        stillFrames = 0;
        foreach (var p in pockets) if (p != null) p.SetHighlight(false);
    }

    public void StopAll()
    {
        foreach (var b in all)
            if (b.onTable && !b.rb.isKinematic) { b.rb.linearVelocity = Vector3.zero; b.rb.angularVelocity = Vector3.zero; }
    }

    public void HighlightPocket(int idx1to6)
    {
        for (int i = 0; i < pockets.Length; i++)
            if (pockets[i] != null) pockets[i].SetHighlight(i + 1 == idx1to6);
    }

    public bool IsFree(Vector3 p, BallController ignore, float margin)
    {
        float min = 2f * BallController.Radius + margin;
        foreach (var b in all)
        {
            if (b == ignore || !b.onTable) continue;
            float dx = b.transform.position.x - p.x, dz = b.transform.position.z - p.z;
            if (dx * dx + dz * dz < min * min) return false;
        }
        return true;
    }

    public Vector3 FindFreeSpot(Vector3 desired, BallController ball)
    {
        desired.y = BallController.Radius;
        if (IsFree(desired, ball, 0.004f)) return desired;
        for (float r = 0.09f; r < 1.2f; r += 0.09f)
        {
            int n = Mathf.Max(6, Mathf.RoundToInt(2f * Mathf.PI * r / 0.09f));
            for (int i = 0; i < n; i++)
            {
                float a = i * 2f * Mathf.PI / n;
                var c = desired + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                if (TableGeometry.Inside(c, 0.09f) && IsFree(c, ball, 0.004f)) return c;
            }
        }
        return desired;
    }

    public void ReturnToTable(BallController b)
    {
        b.PlaceAt(FindFreeSpot(b.startPosition, b));
    }
}
