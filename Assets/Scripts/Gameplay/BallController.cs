using System.Collections;
using UnityEngine;

/// <summary>Física e estado de uma bola (branca, vermelha, azul ou dourada).</summary>
[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    public BallType type;
    public int index;
    public Vector3 startPosition;

    [HideInInspector] public bool onTable = true;
    [HideInInspector] public bool keptPocketed;
    [HideInInspector] public int lastPocketIndex;
    [HideInInspector] public bool hitCushionThisShot;
    [HideInInspector] public bool hasFirstHit;
    [HideInInspector] public BallType firstHitType;

    public Rigidbody rb { get; private set; }
    public const float Radius = TableGeometry.BallRadius;
    public const float StopSpeed = 0.05f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 0.17f;
        rb.linearDamping = 1.0f;
        rb.angularDamping = 0.8f;
        rb.maxAngularVelocity = 120f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;   // evita tunneling
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezePositionY;
    }

    void FixedUpdate()
    {
        if (!onTable || rb.isKinematic) return;
        float s = rb.linearVelocity.magnitude;
        if (s < StopSpeed)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else if (s < 0.35f)
        {
            rb.linearVelocity *= 0.985f;   // freio extra para a bola não "arrastar" no fim
            rb.angularVelocity *= 0.985f;
        }
    }

    public void BeginShot()
    {
        hitCushionThisShot = false;
        hasFirstHit = false;
    }

    void OnCollisionEnter(Collision c)
    {
        var other = c.collider.GetComponent<BallController>();
        if (other != null)
        {
            if (type == BallType.Cue && !hasFirstHit) { hasFirstHit = true; firstHitType = other.type; }
            if (GetInstanceID() < other.GetInstanceID()) GameEvents.RaiseBallHit(c.relativeVelocity.magnitude);
        }
        else if (c.collider.GetComponent<CushionMarker>() != null)
        {
            hitCushionThisShot = true;
            GameEvents.RaiseCushionHit();
        }
    }

    public void Pocket(int pocketIdx, Vector3 pocketPos)
    {
        if (!onTable) return;
        onTable = false;
        lastPocketIndex = pocketIdx;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        GetComponent<Collider>().enabled = false;
        StartCoroutine(Sink(pocketPos));
    }

    IEnumerator Sink(Vector3 target)
    {
        float t = 0f;
        Vector3 s = transform.position;
        Vector3 sc = Vector3.one * Radius * 2f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.22f;
            transform.position = Vector3.Lerp(s, new Vector3(target.x, -0.06f, target.z), Mathf.Clamp01(t));
            transform.localScale = sc * (1f - 0.6f * Mathf.Clamp01(t));
            yield return null;
        }
        gameObject.SetActive(false);
    }

    /// <summary>Coloca (ou devolve) a bola na mesa em uma posição.</summary>
    public void PlaceAt(Vector3 p)
    {
        StopAllCoroutines();
        gameObject.SetActive(true);
        onTable = true;
        keptPocketed = false;
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        GetComponent<Collider>().enabled = true;
        transform.localScale = Vector3.one * Radius * 2f;
        p.y = Radius;
        transform.position = p;
        rb.position = p;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
