using UnityEngine;

/// <summary>Caçapa numerada de 1 a 6 (trigger). Avisa o BallRegistry quando uma bola entra.</summary>
[RequireComponent(typeof(SphereCollider))]
public class PocketTrigger : MonoBehaviour
{
    public int index = 1;
    public Renderer ring;
    public Color neon = Color.magenta;

    bool highlighted;
    Material ringMat;

    void Awake()
    {
        var c = GetComponent<SphereCollider>();
        c.isTrigger = true;
        if (ring != null) ringMat = ring.material;
    }

    void OnTriggerEnter(Collider other)
    {
        var b = other.GetComponent<BallController>();
        if (b == null || !b.onTable) return;
        if (BallRegistry.Instance != null) BallRegistry.Instance.OnPocketed(b, this);
    }

    public void SetHighlight(bool on) { highlighted = on; }
    public void SetNeon(Color c) { neon = c; }

    void Update()
    {
        if (ringMat == null) return;
        Color c = neon;
        if (highlighted)
        {
            float k = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 8f);
            c = Color.Lerp(neon, Color.white, 0.35f);
            c.a = k;
            ring.transform.localScale = Vector3.one * (0.34f + 0.03f * Mathf.Sin(Time.unscaledTime * 8f));
        }
        else
        {
            c.a = 0.55f;
            ring.transform.localScale = Vector3.one * 0.30f;
        }
        ringMat.color = c;
    }
}
