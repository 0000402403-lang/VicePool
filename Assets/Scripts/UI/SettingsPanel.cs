using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Painel de Configurações (Vídeo, Áudio, Controles, Gameplay). Usado no menu e na pausa.</summary>
public static class SettingsPanel
{
    public static GameObject Build(Transform parent, Action onClose)
    {
        var root = new GameObject("PainelConfig", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        UIKit.Stretch(root.GetComponent<RectTransform>());

        var dim = UIKit.Panel(root.transform, "Escurecer", new Color(0.02f, 0f, 0.06f, 0.82f), false);
        UIKit.Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        var box = UIKit.Panel(root.transform, "Caixa", Palette.A(Palette.Painel, 0.96f));
        UIKit.Place(box.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 760f));
        var boxOl = box.gameObject.AddComponent<Outline>();
        boxOl.effectColor = Palette.A(Palette.HotPink, 0.9f); boxOl.effectDistance = new Vector2(3f, -3f);

        var title = UIKit.Label(box.transform, "Titulo", "Configurações", 64, Palette.HotPink, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic, true);
        UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(800f, 90f));
        title.rectTransform.pivot = new Vector2(0.5f, 1f);

        // estado pendente
        var resList = GameSettings.Resolutions();
        int resIdx = resList.FindIndex(v => v.x == (GameSettings.ResW > 0 ? GameSettings.ResW : Screen.width) &&
                                            v.y == (GameSettings.ResH > 0 ? GameSettings.ResH : Screen.height));
        if (resIdx < 0) resIdx = resList.Count - 1;
        int quality = Mathf.Clamp(GameSettings.Quality, 0, QualitySettings.names.Length - 1);
        bool full = GameSettings.Fullscreen, vsync = GameSettings.VSync, aim = GameSettings.AimLine;

        // abas
        var tabs = new List<GameObject>();
        var tabBtns = new List<Button>();
        string[] names = { "Vídeo", "Áudio", "Controles", "Gameplay" };
        Action<int> show = idx =>
        {
            for (int i = 0; i < tabs.Count; i++) tabs[i].SetActive(i == idx);
            for (int i = 0; i < tabBtns.Count; i++)
            {
                var img = tabBtns[i].GetComponent<Image>();
                img.color = i == idx ? Palette.A(Palette.Magenta, 0.9f) : Palette.A(Palette.Painel, 0.92f);
            }
        };

        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            var b = UIKit.NeonButton(box.transform, names[i], new Vector2(-440f, 190f - i * 110f), new Vector2(240f, 84f), Palette.Ciano, () => show(idx), 32);
            tabBtns.Add(b);

            var c = new GameObject("Aba_" + names[i], typeof(RectTransform));
            c.transform.SetParent(box.transform, false);
            UIKit.Place(c.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(140f, 20f), new Vector2(760f, 520f));
            tabs.Add(c);
        }

        // ---- Vídeo ----
        var v = tabs[0].transform;
        Row(v, "Resolução", 190f);
        var bRes = UIKit.NeonButton(v, Fmt(resList[resIdx]), new Vector2(190f, 190f), new Vector2(330f, 68f), Palette.Ciano, null, 30);
        bRes.onClick.AddListener(() => { resIdx = (resIdx + 1) % resList.Count; UIKit.SetButtonText(bRes, Fmt(resList[resIdx])); });

        Row(v, "Qualidade gráfica", 70f);
        var bQ = UIKit.NeonButton(v, QualitySettings.names[quality], new Vector2(190f, 70f), new Vector2(330f, 68f), Palette.Ciano, null, 30);
        bQ.onClick.AddListener(() => { quality = (quality + 1) % QualitySettings.names.Length; UIKit.SetButtonText(bQ, QualitySettings.names[quality]); });

        Row(v, "Tela cheia", -50f);
        var bF = UIKit.NeonButton(v, full ? "Ligado" : "Desligado", new Vector2(190f, -50f), new Vector2(330f, 68f), Palette.Ciano, null, 30);
        bF.onClick.AddListener(() => { full = !full; UIKit.SetButtonText(bF, full ? "Ligado" : "Desligado"); });

        Row(v, "VSync", -170f);
        var bV = UIKit.NeonButton(v, vsync ? "Ligado" : "Desligado", new Vector2(190f, -170f), new Vector2(330f, 68f), Palette.Ciano, null, 30);
        bV.onClick.AddListener(() => { vsync = !vsync; UIKit.SetButtonText(bV, vsync ? "Ligado" : "Desligado"); });

        // ---- Áudio ----
        var a = tabs[1].transform;
        Row(a, "Volume geral", 150f);
        UIKit.CreateSlider(a, new Vector2(190f, 150f), new Vector2(340f, 50f), Palette.HotPink, GameSettings.Master, x => { GameSettings.Master = x; GameSettings.ApplyAudio(); });
        Row(a, "Música", 20f);
        UIKit.CreateSlider(a, new Vector2(190f, 20f), new Vector2(340f, 50f), Palette.Ciano, GameSettings.Music, x => { GameSettings.Music = x; });
        Row(a, "Efeitos", -110f);
        UIKit.CreateSlider(a, new Vector2(190f, -110f), new Vector2(340f, 50f), Palette.Violeta, GameSettings.Sfx, x => { GameSettings.Sfx = x; });

        // ---- Controles ----
        var c2 = tabs[2].transform;
        var ctrl = UIKit.Label(c2, "Lista",
            "Mouse  ......  direciona a mira\n" +
            "Segurar botão esquerdo  ......  enche a barra de força\n" +
            "Soltar o botão  ......  executa a tacada\n" +
            "Botão direito / teclas 1 a 6  ......  escolhe a caçapa\n" +
            "Botão direito (carregando)  ......  cancela a tacada\n" +
            "Q / E  ......  gira a câmera\n" +
            "Scroll  ......  aproxima / afasta\n" +
            "Esc  ......  menu de pausa",
            30, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal, false);
        ctrl.horizontalOverflow = HorizontalWrapMode.Wrap;
        UIKit.Stretch(ctrl.rectTransform);
        ctrl.lineSpacing = 1.35f;

        // ---- Gameplay ----
        var g = tabs[3].transform;
        Row(g, "Linha de mira", 150f);
        var bA = UIKit.NeonButton(g, aim ? "Ligada" : "Desligada", new Vector2(190f, 150f), new Vector2(330f, 68f), Palette.Ciano, null, 30);
        bA.onClick.AddListener(() => { aim = !aim; UIKit.SetButtonText(bA, aim ? "Ligada" : "Desligada"); });
        var tip = UIKit.Label(g, "Dica", "A linha de mira é só uma estimativa visual\nda trajetória da bola branca.", 26,
            Palette.A(Color.white, 0.65f), TextAnchor.MiddleLeft, FontStyle.Italic, false);
        UIKit.Place(tip.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(700f, 100f));

        // rodapé
        UIKit.NeonButton(box.transform, "Voltar", new Vector2(-300f, -320f), new Vector2(260f, 80f), Palette.Ciano, () => { if (onClose != null) onClose(); }, 34);
        UIKit.NeonButton(box.transform, "Aplicar", new Vector2(300f, -320f), new Vector2(260f, 80f), Palette.HotPink, () =>
        {
            GameSettings.ResW = resList[resIdx].x; GameSettings.ResH = resList[resIdx].y;
            GameSettings.Quality = quality; GameSettings.Fullscreen = full; GameSettings.VSync = vsync; GameSettings.AimLine = aim;
            PlayerPrefs.Save();
            GameSettings.ApplyAll();
        }, 34);

        show(0);
        root.SetActive(false);
        return root;
    }

    static string Fmt(Vector2Int r) { return r.x + " x " + r.y; }

    static void Row(Transform p, string label, float y)
    {
        var t = UIKit.Label(p, "L_" + label, label, 34, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold, false);
        UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-250f, y), new Vector2(330f, 60f));
    }
}
