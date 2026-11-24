using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Câmera da mesa: ângulo inclinado como no mockup. Q/E giram a mesa em passos de 60°, scroll aproxima.</summary>
public class CameraRig : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 3.0f, -2.3f);
    public Vector3 lookAt = new Vector3(0f, 0f, 0.05f);

    float yaw, targetYaw, zoom = 1f, targetZoom = 1f;

    void LateUpdate()
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (Time.timeScale > 0f)
        {
            if (kb != null)
            {
                if (kb.qKey.wasPressedThisFrame) targetYaw -= 60f;
                if (kb.eKey.wasPressedThisFrame) targetYaw += 60f;
            }
            if (mouse != null)
            {
                float s = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(s) > 0.01f) targetZoom = Mathf.Clamp(targetZoom - Mathf.Sign(s) * 0.08f, 0.65f, 1.3f);
            }
        }
        float k = 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime);
        yaw = Mathf.LerpAngle(yaw, targetYaw, k);
        zoom = Mathf.Lerp(zoom, targetZoom, k);
        transform.position = Quaternion.Euler(0f, yaw, 0f) * (offset * zoom);
        transform.LookAt(lookAt);
    }
}
