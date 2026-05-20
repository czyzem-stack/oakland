using UnityEngine;

public enum FTUEStage
{
    InitialFight,
    FirstChest,
    MushroomFight,
    OrcFight,
    SecondChest,
    WormOrcFight,
    LastChest,
    Completed
}

public class FTUEManager : MonoBehaviour
{
    public static FTUEManager Instance;

    public FTUEStage currentStage = FTUEStage.InitialFight;
    public bool isFTUEActive = true;

    private const string FTUE_COMPLETED_KEY = "FTUE_Completed_v2"; // Versioned to force reset if needed

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
                // Ensure Steve levels up after the first fight
                var stats = CombatSystem.Instance?.playerStats;
                if (stats != null && stats.level == 1)
                {
                    stats.AddXP(stats.MaxXP); // Force level up
                }
                currentStage = FTUEStage.FirstChest; 
                break;
            case FTUEStage.FirstChest: currentStage = FTUEStage.MushroomFight; break;
            case FTUEStage.MushroomFight: currentStage = FTUEStage.OrcFight; break;
            case FTUEStage.OrcFight: currentStage = FTUEStage.SecondChest; break;
            case FTUEStage.SecondChest: currentStage = FTUEStage.WormOrcFight; break;
            case FTUEStage.WormOrcFight: currentStage = FTUEStage.LastChest; break;
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
            case FTUEStage.SecondChest: typeToSpawn = EnemyType.TreasureChest; break;
            case FTUEStage.WormOrcFight: typeToSpawn = Random.value > 0.5f ? EnemyType.Worm : EnemyType.Orc; break;
            case FTUEStage.LastChest: typeToSpawn = EnemyType.TreasureChest; break;
            default: return null;
        }

        // Force spawn near player (reasonable distance)
        PointOfInterest poi = POIManager.Instance.ForceSpawnPOI(typeToSpawn, playerPos, 12f, 20f);
        return poi != null ? poi.transform : null;
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

