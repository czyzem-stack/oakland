using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    [Header("Settings")]
    public string sceneToLoad = "Main";
    public float minLoadingTime = 3.0f; // Give it enough time to feel like it's loading everything
    
    [Header("UI References")]
    public Slider progressBar;
    public TMP_Text progressText;
    public Image background;

    [Header("Warmup Settings")]
    public GameObject[] warmupPrefabs;
    public bool warmupInProgress = false;

    private void Start()
    {
        if (progressBar != null) progressBar.value = 0;
        if (progressText != null) progressText.text = "0%";
        
        StartCoroutine(LoadingSequence());
    }

    private IEnumerator LoadingSequence()
    {
        yield return StartCoroutine(WarmupPrefabsRoutine());
        yield return StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator WarmupPrefabsRoutine()
    {
        warmupInProgress = true;
        if (warmupPrefabs == null || warmupPrefabs.Length == 0) yield break;

        for (int i = 0; i < warmupPrefabs.Length; i++)
        {
            if (warmupPrefabs[i] == null) continue;
            
            // Spawn far away
            GameObject temp = Instantiate(warmupPrefabs[i], new Vector3(0, -1000, 0), Quaternion.identity);
            temp.SetActive(true);
            
            // Wait one frame for the engine to register it
            yield return null;
            
            Destroy(temp);
            
            // Update visual progress slightly for warmup
            float p = (float)i / warmupPrefabs.Length * 0.2f; // Warmup takes first 20%
            if (progressBar != null) progressBar.value = p;
            if (progressText != null) progressText.text = $"Warming assets... {(p * 100):F0}%";
        }
        
        // Mark DiceRollSystem as warmed up so it doesn't do it again
        DiceRollSystem.WarmedUp = true;
        warmupInProgress = false;
    }

    private IEnumerator LoadSceneAsync()
    {
        // Give the UI a frame to update
        yield return null;

        // Force a GC collect to start clean
        System.GC.Collect();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
        operation.allowSceneActivation = false;

        float startTime = Time.time;
        float visualProgress = 0f;

        while (visualProgress < 1.0f)
        {
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);
            
            // Smoothly move the progress bar
            visualProgress = Mathf.MoveTowards(visualProgress, targetProgress, Time.deltaTime * 0.5f);
            
            if (progressBar != null)
                progressBar.value = visualProgress;
            
            if (progressText != null)
                progressText.text = $"{(visualProgress * 100):F0}%";

            // If loading is actually finished at 90% (Unity's limit for allowSceneActivation=false)
            if (operation.progress >= 0.9f)
            {
                // We still wait for the minLoadingTime to satisfy the "load everything" feel
                if (Time.time - startTime >= minLoadingTime)
                {
                    visualProgress = 1.0f;
                }
            }

            yield return null;
        }

        if (progressBar != null) progressBar.value = 1.0f;
        if (progressText != null) progressText.text = "100%";

        // Small delay to show 100%
        yield return new WaitForSeconds(0.5f);

        operation.allowSceneActivation = true;
    }
}
