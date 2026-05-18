using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreenUI : MonoBehaviour
{
    public static LoadingScreenUI Instance;

    public CanvasGroup canvasGroup;
    public Text loadingText;
    public float fadeDuration = 0.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    private void Start()
    {
        // Start visible and wait for initialization
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    public void OnSystemInitialized()
    {
        // Can be called by systems when they are ready
        StartCoroutine(HideAfterDelay(0.5f));
    }

    private void Update()
    {
        if (canvasGroup.alpha > 0 && loadingText != null)
        {
            float scale = 1f + Mathf.Sin(Time.time * 5f) * 0.05f;
            loadingText.transform.localScale = Vector3.one * scale;
        }
    }

    public void Show()
{
        StopAllCoroutines();
        StartCoroutine(Fade(1f));
    }

    public void Hide(float delay = 0f)
    {
        StopAllCoroutines();
        StartCoroutine(HideAfterDelay(delay));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return Fade(0f);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.blocksRaycasts = targetAlpha > 0.5f;
    }
}
