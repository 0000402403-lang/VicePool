using UnityEngine;

/// <summary>Regra de cada fase. A dificuldade muda só aqui (ScriptableObject), a física é a mesma.</summary>
[CreateAssetMenu(menuName = "VicePool/PhaseConfig", fileName = "PhaseConfig")]
public class PhaseConfig : ScriptableObject
{
    public int order;
    public string displayName = "Fase 1";
    public string difficulty = "Fácil";
    public Color feltColor = Color.magenta;
    public Color neonColor = Color.magenta;

    [Tooltip("Fase 2 e 3: o jogador escolhe a caçapa antes de cada tacada.")]
    public bool requireChosenPocket;
    [Tooltip("Fase 3: a bola precisa bater em pelo menos 1 tabela antes de entrar.")]
    public bool requireCushion;
    [Tooltip("Fase 1: na bola dourada (última) o jogador escolhe a caçapa.")]
    public bool goldChoosesPocket;

    [TextArea] public string ruleText;
}
