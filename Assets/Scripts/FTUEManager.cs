using UnityEngine;

public enum FTUEStage
{
    InitialFight,
    FirstChest,
    MushroomFight,
    OrcFight,
    ChestAfterOrc,
    WormFight,
    OrcFightFinal,
    LastChest,
    Completed
}

public class FTUEManager : MonoBehaviour
{
    public static FTUEManager Instance;

    public FTUEStage currentStage = FTUEStage.InitialFight;
    public bool isFTUEActive = true;

    private const string FTUE_COMPLETED_KEY = "FTUE_Completed_v3"; 

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
            case FTUEStage.InitialFight: 
                var stats = CombatSystem.Instance?.playerStats;
                if (stats != null && stats.level == 1) stats.AddXP(stats.MaxXP);
                currentStage = FTUEStage.FirstChest; 
                break;
            case FTUEStage.FirstChest: currentStage = FTUEStage.MushroomFight; break;
            case FTUEStage.MushroomFight: currentStage = FTUEStage.OrcFight; break;
            case FTUEStage.OrcFight: currentStage = FTUEStage.ChestAfterOrc; break;
            case FTUEStage.ChestAfterOrc: currentStage = FTUEStage.WormFight; break;
            case FTUEStage.WormFight: currentStage = FTUEStage.OrcFightFinal; break;
            case FTUEStage.OrcFightFinal: currentStage = FTUEStage.LastChest; break;
            case FTUEStage.LastChest: 
                currentStage = FTUEStage.Completed; 
                isFTUEActive = false;
                PlayerPrefs.SetInt(FTUE_COMPLETED_KEY, 1);
                PlayerPrefs.Save();
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
            case FTUEStage.InitialFight: typeToSpawn = EnemyType.Orc; break;
            case FTUEStage.FirstChest: typeToSpawn = EnemyType.TreasureChest; break;
            case FTUEStage.MushroomFight: typeToSpawn = EnemyType.Mushroom; break;
            case FTUEStage.OrcFight: typeToSpawn = EnemyType.Orc; break;
            case FTUEStage.ChestAfterOrc: typeToSpawn = EnemyType.TreasureChest; break;
            case FTUEStage.WormFight: typeToSpawn = EnemyType.Worm; break;
            case FTUEStage.OrcFightFinal: typeToSpawn = EnemyType.Orc; break;
            case FTUEStage.LastChest: typeToSpawn = EnemyType.TreasureChest; break;
            default: return null;
        }

        Vector3 center = Vector3.zero;
        Vector3 directionToCenter = (center - playerPos).normalized;
        Vector3 targetBasePos = playerPos + directionToCenter * 15f;

        PointOfInterest poi = POIManager.Instance.ForceSpawnPOI(typeToSpawn, targetBasePos, 0f, 5f);
        if (poi != null)
        {
            if (currentStage == FTUEStage.ChestAfterOrc)
            {
                poi.gameObject.name = "FTUE_Second_Chest_POI";
            }
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

