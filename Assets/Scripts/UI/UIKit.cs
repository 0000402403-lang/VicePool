using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Efeito de hover dos botões neon (brilho + leve zoom).</summary>
public class UIHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Outline outline;
    public Selectable selectable;
    Color baseOutline;
    bool hover;

    void Start() { if (outline != null) baseOutline = outline.effectColor; }
    public void OnPointerEnter(PointerEventData e) { hover = true; }
    public void OnPointerExit(PointerEventData e) { hover = false; }
    void OnDisable() { hover = false; transform.localScale = Vector3.one; }

    void Update()
    {
        bool on = hover && (selectable == null || selectable.interactable);
        float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * (on ? 1.06f : 1f), k);
        if (outline != null)
        {
            Color c = on ? Color.Lerp(baseOutline, Color.white, 0.45f) : baseOutline;
            outline.effectColor = Color.Lerp(outline.effectColor, c, k);
        }
    }
}

/// <summary>Helpers para montar a UI em código (estilo neon Vice City).</summary>
public static class UIKit
{
    static Font _font;
    public static Font Font
    {
        get
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _font;
        }
    }

    public static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    public static Canvas CreateCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = order;
        var s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        s.matchWidthOrHeight = 0.5f;
        return c;
    }

    public static RectTransform Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        return r;
    }

    public static RectTransform Place(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = anchor;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        return r;
    }

    public static Image NewImage(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static Image Panel(Transform parent, string name, Color color, bool rounded = true)
    {
        var img = NewImage(parent, name, rounded ? Tex.Rounded() : Tex.White(), color);
        if (rounded) { img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1.8f; }
        return img;
    }

    public static Text Label(Transform parent, string name, string text, int size, Color color,
                             TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold, bool glow = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = Font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        if (glow)
        {
            var o = go.AddComponent<Outline>();
            o.effectColor = Palette.A(color, 0.45f);
            o.effectDistance = new Vector2(2.5f, -2.5f);
        }
        return t;
    }

    public static Button NeonButton(Transform parent, string label, Vector2 pos, Vector2 size, Color neon, Action onClick, int fontSize = 36)
    {
        var img = Panel(parent, "Btn_" + label, Palette.A(Palette.Painel, 0.92f));
        img.raycastTarget = true;
        Place(img.rectTransform, new Vector2(0.5f, 0.5f), pos, size);
        var ol = img.gameObject.AddComponent<Outline>();
        ol.effectColor = Palette.A(neon, 0.95f);
        ol.effectDistance = new Vector2(3f, -3f);

        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.92f, 1f, 1f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.8f, 1f);
        cb.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.7f);
        b.colors = cb;
        b.onClick.AddListener(() => { AudioManager.Click(); if (onClick != null) onClick(); });

        var hv = img.gameObject.AddComponent<UIHover>();
        hv.outline = ol; hv.selectable = b;

        var t = Label(img.transform, "Texto", label, fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, false);
        Stretch(t.rectTransform);
        return b;
    }

    public static void SetButtonText(Button b, string s)
    {
        var t = b.GetComponentInChildren<Text>();
        if (t != null) t.text = s;
    }

    public static Slider CreateSlider(Transform parent, Vector2 pos, Vector2 size, Color neon, float value, Action<float> onChange)
    {
        var root = new GameObject("Slider", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        Place(root.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), pos, size);

        var bg = Panel(root.transform, "Fundo", Palette.A(Palette.Fundo, 0.9f));
        bg.rectTransform.anchorMin = new Vector2(0f, 0.5f); bg.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        bg.rectTransform.sizeDelta = new Vector2(0f, 16f);

        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        var fa = fillArea.GetComponent<RectTransform>();
        fa.anchorMin = new Vector2(0f, 0.5f); fa.anchorMax = new Vector2(1f, 0.5f);
        fa.sizeDelta = new Vector2(-24f, 16f);

        var fill = Panel(fillArea.transform, "Fill", neon);
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = Vector2.zero;

        var handleArea = new GameObject("HandleArea", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        var ha = handleArea.GetComponent<RectTransform>();
        ha.anchorMin = Vector2.zero; ha.anchorMax = Vector2.one;
        ha.offsetMin = new Vector2(12f, 0f); ha.offsetMax = new Vector2(-12f, 0f);

        var handle = NewImage(handleArea.transform, "Handle", Tex.Circle(), Color.white);
        handle.raycastTarget = true;
        handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        handle.rectTransform.sizeDelta = new Vector2(34f, 34f);
        var hol = handle.gameObject.AddComponent<Outline>();
        hol.effectColor = Palette.A(neon, 0.9f); hol.effectDistance = new Vector2(2f, -2f);

        var s = root.AddComponent<Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f; s.maxValue = 1f;
        s.value = value;
        s.onValueChanged.AddListener(v => { if (onChange != null) onChange(v); });
        return s;
    }
}
