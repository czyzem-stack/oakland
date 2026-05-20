using UnityEngine;

public enum FTUEStage
{
    Orc1,
    Chest1,
    Mushroom,
    Orc2,
    Chest2,
    Worm,
    Orc3,
    Chest3,
    Completed
}

public class FTUEManager : MonoBehaviour
{
    public static FTUEManager Instance;

    public FTUEStage currentStage = FTUEStage.Orc1;
    public bool isFTUEActive = true;

    private const string FTUE_COMPLETED_KEY = "FTUE_Completed_v4"; 

    void Awake()
    {
        Instance = this;
        if (PlayerPrefs.GetInt(FTUE_COMPLETED_KEY, 0) == 1)
        {
            isFTUEActive = false;
            currentStage = FTUEStage.Completed;
        }
    }

    public void OnStageCompleted()
    {
        if (!isFTUEActive) return;

        switch (currentStage)
        {
            case FTUEStage.Orc1: 
                var stats = CombatSystem.Instance?.playerStats;
                if (stats != null && stats.level == 1) stats.AddXP(stats.MaxXP);
                currentStage = FTUEStage.Chest1; 
                break;
            case FTUEStage.Chest1: currentStage = FTUEStage.Orc2; break;
            case FTUEStage.Orc2: currentStage = FTUEStage.Mushroom; break;
            case FTUEStage.Mushroom: currentStage = FTUEStage.Chest2; break;
            case FTUEStage.Chest2: currentStage = FTUEStage.Orc3; break;
            case FTUEStage.Orc3: currentStage = FTUEStage.Worm; break;
            case FTUEStage.Worm: currentStage = FTUEStage.Chest3; break;
            case FTUEStage.Chest3: 
                currentStage = FTUEStage.Completed; 
                isFTUEActive = false;
                PlayerPrefs.SetInt(FTUE_COMPLETED_KEY, 1);
                PlayerPrefs.Save();
                // Enable global POIs
                if (POIManager.Instance != null) POIManager.Instance.SetupPOIs();
                break;
        }
        
        Debug.Log($"[FTUE] Advanced to {currentStage}");
    }

    public Transform GetNextTarget(Vector3 playerPos)
    {
        if (!isFTUEActive) return null;

        EnemyType typeToSpawn = EnemyType.Orc;
        switch (currentStage)
        {
            case FTUEStage.Orc1: typeToSpawn = EnemyType.Orc; break;
            case FTUEStage.Chest1: typeToSpawn = EnemyType.TreasureChest; break;
            case FTUEStage.Orc2: typeToSpawn = EnemyType.Orc; break;
            case FTUEStage.Mushroom: typeToSpawn = EnemyType.Mushroom; break;
            case FTUEStage.Chest2: typeToSpawn = EnemyType.TreasureChest; break;
            case FTUEStage.Orc3: typeToSpawn = EnemyType.Orc; break;
            case FTUEStage.Worm: typeToSpawn = EnemyType.Worm; break;
            case FTUEStage.Chest3: typeToSpawn = EnemyType.TreasureChest; break;
            default: return null;
        }

        Vector3 center = Vector3.zero;
        Vector3 directionToCenter = (center - playerPos).normalized;
        Vector3 targetBasePos = playerPos + directionToCenter * 15f;

        PointOfInterest poi = POIManager.Instance.ForceSpawnPOI(typeToSpawn, targetBasePos, 0f, 5f);
        if (poi != null)
        {
            poi.gameObject.name = $"FTUE_{currentStage}_{typeToSpawn}";
            return poi.transform;
        }
        return null;
    }
}

public class PlayerStart : MonoBehaviour
{
    void Awake()
    {
        // Tag this object so we can find it
        gameObject.name = "PlayerSpawnPoint";
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 1f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
    }
}

