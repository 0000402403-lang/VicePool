using UnityEngine;

/// <summary>Troca a cor da mesa (feltro + neon) conforme a fase.</summary>
public class TableVisuals : MonoBehaviour
{
    public static TableVisuals Instance;

    public Renderer felt;
    public Renderer[] neon;
    public Light[] neonLights;

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void Apply(PhaseConfig cfg)
    {
        if (felt != null)
        {
            var m = felt.material;
            m.SetColor("_BaseColor", cfg.feltColor);
            m.SetColor("_Color", cfg.feltColor);
        }
        if (neon != null)
        {
            foreach (var r in neon)
            {
                if (r == null) continue;
                var m = r.material;
                m.SetColor("_BaseColor", cfg.neonColor * 0.3f);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", cfg.neonColor * 3.2f);
            }
        }
        if (neonLights != null)
            foreach (var l in neonLights) if (l != null) l.color = cfg.neonColor;
        foreach (var p in FindObjectsByType<PocketTrigger>(FindObjectsSortMode.None)) p.SetNeon(cfg.neonColor);
    }
}
