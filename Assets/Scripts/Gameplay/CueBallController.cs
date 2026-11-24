using UnityEngine;

[RequireComponent(typeof(BallController))]
public class CueBallController : MonoBehaviour
{
    public float maxSpeed = 8f;
    BallController ball;

    void Awake() { ball = GetComponent<BallController>(); }

    /// <summary>Tacada: power01 de 0 a 1 multiplica a força máxima.</summary>
    public void Shoot(Vector3 dir, float power01)
    {
        if (ball == null) ball = GetComponent<BallController>();
        dir.y = 0f;
        dir.Normalize();
        float speed = Mathf.Lerp(0.8f, maxSpeed, Mathf.Clamp01(power01));
        ball.rb.linearVelocity = dir * speed;
        ball.rb.angularVelocity = Vector3.zero;
    }
}
