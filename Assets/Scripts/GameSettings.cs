using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance;

    [Header("Enemy Unlock Settings")]
    public int orcsKilledToUnlockWorms = 2;
    public int totalOrcsKilled = 0;

    [Header("Navigation Settings")]
    public float metersPerDicePoint = 2.5f;
    public float arrivalDistance = 1.0f;

    private void Awake()
{
        Time.timeScale = 1f;
        GenericPopup.ResetForSceneLoad();
        EquipmentLootPopup.ResetForSceneLoad();

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSettings();
        }
        else
        {
            Instance.InitializeSettings(); // Refresh settings even if instance exists
            Destroy(gameObject);
        }
    }

    public void InitializeSettings()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
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