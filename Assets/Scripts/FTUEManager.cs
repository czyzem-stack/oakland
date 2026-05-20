using UnityEngine;

public enum FTUEStage
{
    Orc1,
    Chest1,
    Orc2,
    Mushroom,
    Chest2,
    Orc3,
    Worm,
    Chest3,
    Completed
}

public class FTUEManager : MonoBehaviour
{
    public static FTUEManager Instance;

    public FTUEStage currentStage = FTUEStage.Orc1;
    public bool isFTUEActive = true;

    private const string FTUE_COMPLETED_KEY = "FTUE_Completed_v5"; 

    void Awake()
    {
        Instance = this;
        // Use a unique key for the current structure to force a reset for the user
        if (PlayerPrefs.GetInt(FTUE_COMPLETED_KEY, 0) == 1)
        {
            isFTUEActive = false;
            currentStage = FTUEStage.Completed;
        }

        if (isFTUEActive)
        {
            Debug.Log("[FTUE] Active. Starting Stage: " + currentStage);
            // Suppress all existing POIs in the scene immediately
            SuppressAllScenePOIs();
        }
    }

    private void SuppressAllScenePOIs()
    {
        PointOfInterest[] all = Object.FindObjectsByType<PointOfInterest>(FindObjectsInactive.Include);
        foreach (var poi in all)
        {
            // If it doesn't have our tutorial prefix, kill it
            if (!poi.name.Contains("FTUE") && !poi.name.Contains("Forced"))
            {
                poi.gameObject.SetActive(false);
            }
        }
    }

    private void Start()
    {
        if (isFTUEActive) SuppressAllScenePOIs();
    }

    public void OnStageCompleted()
    {
        if (!isFTUEActive) return;

        FTUEStage next = currentStage;
        switch (currentStage)
        {
            case FTUEStage.Orc1: 
                var stats = CombatSystem.Instance?.playerStats;
                if (stats != null && stats.level == 1) stats.AddXP(stats.MaxXP);
                next = FTUEStage.Chest1; 
                break;
            case FTUEStage.Chest1: next = FTUEStage.Orc2; break;
            case FTUEStage.Orc2: next = FTUEStage.Mushroom; break;
            case FTUEStage.Mushroom: next = FTUEStage.Chest2; break;
            case FTUEStage.Chest2: next = FTUEStage.Orc3; break;
            case FTUEStage.Orc3: next = FTUEStage.Worm; break;
            case FTUEStage.Worm: next = FTUEStage.Chest3; break;
            case FTUEStage.Chest3: 
                next = FTUEStage.Completed; 
                isFTUEActive = false;
                PlayerPrefs.SetInt(FTUE_COMPLETED_KEY, 1);
                PlayerPrefs.Save();
                if (POIManager.Instance != null) POIManager.Instance.SetupPOIs();
                break;
        }
        
        currentStage = next;
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
