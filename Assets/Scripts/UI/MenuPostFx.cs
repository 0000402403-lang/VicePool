using UnityEngine;

/// <summary>Liga o Bloom neon no Menu Principal (mesma pegada visual do jogo).</summary>
public class MenuPostFx : MonoBehaviour
{
    void Start() { PostFx.Setup(Camera.main, 1.1f); }
}
