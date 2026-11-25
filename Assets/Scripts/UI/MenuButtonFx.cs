using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Botão "vidro" do menu: entra deslizando, destaca no hover (barra, borda, seta e leve zoom).</summary>
public class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    RectTransform rt;
    Vector2 basePos;
    Image border, bar;
    Text arrow;
    CanvasGroup cg;
    Color accent;
    bool primary, over;
    float t, delay, hover;

    public void Init(RectTransform rt, Vector2 basePos, Image border, Image bar, Text arrow,
                     CanvasGroup cg, Color accent, bool primary, float delay)
    {
        this.rt = rt; this.basePos = basePos; this.border = border; this.bar = bar; this.arrow = arrow;
        this.cg = cg; this.accent = accent; this.primary = primary; this.delay = delay;
        cg.alpha = 0f;
    }

    void OnEnable() { t = 0f; hover = 0f; }
    public void OnPointerEnter(PointerEventData e) { over = true; }
    public void OnPointerExit(PointerEventData e) { over = false; }
    void OnDisable() { over = false; }

    void Update()
    {
        if (rt == null) return;
        float dt = Time.unscaledDeltaTime;
        t += dt;
        float appear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - delay) / 0.55f));
        hover = Mathf.MoveTowards(hover, over ? 1f : 0f, dt * 7f);
        float h = Mathf.SmoothStep(0f, 1f, hover);

        rt.anchoredPosition = basePos + new Vector2(-70f * (1f - appear) + 16f * h, 0f);
        rt.localScale = Vector3.one * (1f + 0.025f * h);
        cg.alpha = appear;
        border.color = Palette.A(accent, Mathf.Lerp(primary ? 0.95f : 0.30f, 1f, h));
        bar.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(5f, 10f, h), 0f);
        var ac = accent; ac.a = Mathf.Lerp(0.45f, 1f, h);
        arrow.color = ac;
    }
}
