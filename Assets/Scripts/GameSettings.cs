using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance;

    [Header("Enemy Unlock Settings")]
    public int orcsKilledToUnlockWorms = 2;
    public int totalOrcsKilled = 0;

    private void Awake()
    {
        Time.timeScale = 1f;
        GenericPopup.ResetForSceneLoad();

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterOrcKill()
    {
        totalOrcsKilled++;
        Debug.Log($"[GameSettings] Orc Killed! Total: {totalOrcsKilled}. Worms Unlocked: {AreWormsUnlocked()}");
    }

    public bool AreWormsUnlocked()
    {
        return totalOrcsKilled >= orcsKilledToUnlockWorms;
    }
}