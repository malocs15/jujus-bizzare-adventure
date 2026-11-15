using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFlow : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] float fadeDuration = 0.4f;

    CanvasGroup overlay;

    public bool IsLoading { get; private set; }

    void Awake()
    {
        overlay = BuildOverlay();
    }

    public void Load(string sceneName)
    {
        if (IsLoading) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Scene '" + sceneName + "' is not in the build Scene List.");
            return;
        }
        StartCoroutine(LoadRoutine(sceneName));
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        IsLoading = true;
        yield return Fade(0f, 1f);
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return Fade(1f, 0f);
        IsLoading = false;
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;       
            overlay.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        overlay.alpha = to;
    }

    CanvasGroup BuildOverlay()
    {
        var canvasGo = new GameObject("FadeOverlay");
        canvasGo.transform.SetParent(transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var group = canvasGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        var imageGo = new GameObject("Black");
        imageGo.transform.SetParent(canvasGo.transform, false);
        var image = imageGo.AddComponent<Image>();
        image.color = Color.black;
        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return group;
    }
}