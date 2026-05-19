using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    [Header("Settings")]
    public string sceneToLoad = "Main";
    public float minLoadingTime = 3.0f;
    
    [Header("UI References")]
    public Slider progressBar;
    public TMP_Text progressText;
    public UnityEngine.UI.Image background;
    public GameObject tapToStartText;

    [Header("Warmup Settings")]
    public GameObject[] warmupPrefabs;

    private void Start()
    {
        GenericPopup.ResetForSceneLoad();
        if (progressBar != null) progressBar.value = 0;
        if (progressText != null) progressText.text = "0%";
        if (tapToStartText != null) tapToStartText.SetActive(false);
        
        StartCoroutine(LoadingSequence());
    }

    private IEnumerator LoadingSequence()
    {
        yield return StartCoroutine(WarmupPrefabsRoutine());
        yield return StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator WarmupPrefabsRoutine()
    {
        if (warmupPrefabs == null || warmupPrefabs.Length == 0) yield break;

        for (int i = 0; i < warmupPrefabs.Length; i++)
        {
            if (warmupPrefabs[i] == null) continue;
            GameObject temp = Instantiate(warmupPrefabs[i], new Vector3(0, -1000, 0), Quaternion.identity);
            temp.SetActive(true);
            yield return null;
            Destroy(temp);
            
            float p = (float)i / warmupPrefabs.Length * 0.15f; 
            if (progressBar != null) progressBar.value = p;
            if (progressText != null) progressText.text = $"Loading... {(p * 100):F0}%";
}
        DiceRollSystem.WarmedUp = true;
    }

    private IEnumerator LoadSceneAsync()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad);
        op.allowSceneActivation = false;

        float startTime = Time.time;
        float visualProgress = progressBar != null ? progressBar.value : 0.15f;

        while (visualProgress < 1.0f)
        {
            float targetProgress = Mathf.Clamp01(op.progress / 0.9f);
            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, Time.deltaTime * 0.4f);
            
            if (progressBar != null) progressBar.value = visualProgress;
            if (progressText != null) progressText.text = $"Loading... {(visualProgress * 100):F0}%";

            if (op.progress >= 0.9f && Time.time - startTime >= minLoadingTime)
            {
                visualProgress = 1.0f;
                if (progressBar != null) progressBar.value = 1.0f;
                if (progressText != null) progressText.text = "100%";
            }

            yield return null;
        }

        if (tapToStartText != null)
        {
            tapToStartText.SetActive(true);
            if (progressBar != null) progressBar.gameObject.SetActive(false);
            if (progressText != null) progressText.gameObject.SetActive(false);
            
            while (true)
            {
                bool pressed = false;
                if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                {
                    pressed = true;
                }
                // Fallback for keyboard/any key
                else if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.anyKey.wasPressedThisFrame)
                {
                    pressed = true;
                }
                
                if (pressed) break;
                yield return null;
            }
}

        op.allowSceneActivation = true;
    }
}
