using UnityEngine;
using UnityEngine.UI;

// Indicateur de bruit à l'écran (trois barres) : 0 barre allumée à l'arrêt ou sur la roche,
// 1 en marchant sur le sable, 3 en courant. Construit par script en attendant l'emplacement
// prévu dans le HUD (S1-J5) ; il ne fait qu'écouter PlayerNoise.LevelChanged.
public class NoiseIndicator : MonoBehaviour
{
    [Header("Apparence")]
    public Vector2 screenOffset = new Vector2(30f, 30f);  // depuis le coin bas gauche
    public Color offColor = new Color(1f, 1f, 1f, 0.2f);
    public Color walkColor = new Color(1f, 0.85f, 0.3f);
    public Color runColor = new Color(1f, 0.3f, 0.2f);

    private PlayerNoise noise;
    private Image[] bars;

    void Start()
    {
        noise = FindFirstObjectByType<PlayerNoise>();
        if (noise == null)
        {
            Debug.LogWarning("NoiseIndicator : aucun PlayerNoise dans la scène.", this);
            enabled = false;
            return;
        }
        BuildUI();
        noise.LevelChanged += Show;
        Show(noise.Level);
    }

    void OnDestroy()
    {
        if (noise != null) noise.LevelChanged -= Show;
    }

    private void Show(PlayerNoise.NoiseLevel level)
    {
        // Nombre de barres allumées et leur couleur selon le niveau
        int lit = 0;
        Color on = walkColor;
        switch (level)
        {
            case PlayerNoise.NoiseLevel.Walking:
                lit = 1;
                break;
            case PlayerNoise.NoiseLevel.Running:
                lit = 3;
                on = runColor;
                break;
        }

        for (int i = 0; i < bars.Length; i++)
            bars[i].color = i < lit ? on : offColor;
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("NoiseIndicatorCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        bars = new Image[3];
        for (int i = 0; i < bars.Length; i++)
        {
            var barGo = new GameObject("Bar" + i);
            barGo.transform.SetParent(canvasGo.transform, false);
            bars[i] = barGo.AddComponent<Image>();
            RectTransform rt = bars[i].rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;   // coin bas gauche
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(14f, 16f + i * 12f); // barres de plus en plus hautes
            rt.anchoredPosition = screenOffset + new Vector2(i * 20f, 0f);
        }
    }
}
