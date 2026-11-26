using UnityEngine;
using UnityEngine.UI;

// Jauge de chaleur à l'écran : une barre qui se remplit et rougit, et qui clignote une fois pleine.
// Construite par script en attendant l'emplacement prévu dans le HUD (S1-J5) ;
// elle ne fait qu'écouter HeatGauge.HeatChanged.
public class HeatIndicator : MonoBehaviour
{
    [Header("Apparence")]
    public Vector2 screenOffset = new Vector2(110f, 30f); // depuis le coin bas gauche, à droite du bruit
    public Vector2 size = new Vector2(160f, 14f);
    public Color coolColor = new Color(1f, 0.85f, 0.4f);
    public Color hotColor = new Color(1f, 0.25f, 0.1f);

    private HeatGauge heat;
    private Image fill;
    private float value;

    void Start()
    {
        heat = FindFirstObjectByType<HeatGauge>();
        if (heat == null)
        {
            Debug.LogWarning("HeatIndicator : aucune HeatGauge dans la scène.", this);
            enabled = false;
            return;
        }
        BuildUI();
        heat.HeatChanged += Show;
        Show(heat.Heat);
    }

    void OnDestroy()
    {
        if (heat != null) heat.HeatChanged -= Show;
    }

    void Update()
    {
        // Jauge pleine : elle clignote pour prévenir que la vie baisse
        // (interface : temps réel, elle n'est pas figée par l'arrêt du temps)
        if (value >= 1f)
            fill.color = Color.Lerp(hotColor, Color.white, Mathf.PingPong(Time.unscaledTime * 4f, 1f));
    }

    private void Show(float newValue)
    {
        value = newValue;
        fill.rectTransform.sizeDelta = new Vector2(size.x * value, size.y);
        fill.color = Color.Lerp(coolColor, hotColor, value);
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("HeatIndicatorCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        Image back = CreateBar(canvasGo.transform, "HeatBack", new Color(0f, 0f, 0f, 0.4f));
        back.rectTransform.sizeDelta = size;
        fill = CreateBar(canvasGo.transform, "HeatFill", coolColor);
    }

    private Image CreateBar(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;   // coin bas gauche
        rt.pivot = Vector2.zero;                      // se remplit de gauche à droite
        rt.anchoredPosition = screenOffset;
        rt.sizeDelta = new Vector2(0f, size.y);
        return img;
    }
}
