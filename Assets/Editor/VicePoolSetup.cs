using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Cria os PhaseConfig, as cenas MainMenu e Game e registra no Build Settings. Menu: Vice Pool.</summary>
public static class VicePoolSetup
{
    [MenuItem("Vice Pool/Construir Cenas e Fases")]
    public static void BuildAll()
    {
        CreatePhaseAssets();
        CreateScene("Assets/Scenes/MainMenu.unity", "MenuController", typeof(MainMenuController), typeof(MenuPostFx));
        CreateScene("Assets/Scenes/Game.unity", "GameBootstrap", typeof(GameSceneBootstrap));

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true)
        };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("[VicePool] Cenas MainMenu e Game criadas, fases em Resources/Phases, Build Settings atualizado.");
    }

    static void CreatePhaseAssets()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Phases")) AssetDatabase.CreateFolder("Assets/Resources", "Phases");

        MakePhase("Fase1_Facil", 1, "Fase 1", "Fácil", Palette.Magenta, Palette.HotPink, false, false, true,
            "Qualquer caçapa vale. Na bola dourada (última) você escolhe a caçapa.");
        MakePhase("Fase2_Medio", 2, "Fase 2", "Médio", Palette.Turquesa, Palette.Ciano, true, false, false,
            "Escolha a caçapa antes de cada tacada. A bola só vale se entrar nela.");
        MakePhase("Fase3_Dificil", 3, "Fase 3", "Difícil", Palette.Roxo, Palette.Violeta, true, true, false,
            "Escolha a caçapa e a bola precisa bater em pelo menos 1 tabela antes de entrar.");
    }

    static void MakePhase(string file, int order, string name, string diff, Color felt, Color neon,
                          bool chosen, bool cushion, bool goldChoose, string rule)
    {
        string path = "Assets/Resources/Phases/" + file + ".asset";
        var so = AssetDatabase.LoadAssetAtPath<PhaseConfig>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<PhaseConfig>();
            AssetDatabase.CreateAsset(so, path);
        }
        so.order = order;
        so.displayName = name;
        so.difficulty = diff;
        so.feltColor = felt;
        so.neonColor = neon;
        so.requireChosenPocket = chosen;
        so.requireCushion = cushion;
        so.goldChoosesPocket = goldChoose;
        so.ruleText = rule;
        EditorUtility.SetDirty(so);
    }

    static void CreateScene(string path, string rootName, params System.Type[] components)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var go = new GameObject(rootName);
        foreach (var t in components) go.AddComponent(t);
        EditorSceneManager.SaveScene(scene, path);
    }
}
