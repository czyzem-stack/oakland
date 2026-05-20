using UnityEngine;

public enum EnemyType
{
    Orc,
    TreasureChest,
    Mushroom,
    DragonBob,
    Worm
}

public class PointOfInterest : MonoBehaviour
{
    public EnemyType enemyType = EnemyType.Orc;
    
    [Header("Territory Settings")]
    public float patrolRadius = 4.0f;
    public float engagementRadius = 3.5f; // Reduced from 6.0f to avoid accidental engagements

    [Header("Prefabs")]
    public GameObject orcPrefab;
    public GameObject chestPrefab;
    public GameObject mushroomPrefab;
    public GameObject dragonPrefab;
    public GameObject wormPrefab;
    public GameObject healthCanvasPrefab;

    private void Start()
    {
        EnsureEnemy();
    }

    private void Update()
    {
        // Throttle proximity checks to once every 5 frames
        if (Time.frameCount % 5 != 0) return;

        // During FTUE, only allow engagement if this is a forced FTUE object
        if (FTUEManager.Instance != null && FTUEManager.Instance.isFTUEActive)
        {
            if (!name.Contains("Forced") && !name.Contains("FTUE")) return;
        }

        // Generic engagement check for non-patrolling enemies (Mushrooms, etc.)
        if (enemyType != EnemyType.Orc && enemyType != EnemyType.DragonBob && enemyType != EnemyType.TreasureChest)
        {
            CheckProximityEngagement();
        }
    }

    private void CheckProximityEngagement()
    {
        if (CombatSystem.Instance == null || CombatSystem.Instance.isInCombat) return;
        if (GenericPopup.IsOpen || EquipmentLootPopup.IsOpen) return;

        CharacterStats stats = GetComponentInChildren<CharacterStats>();
        if (stats == null || stats.isDead) return;

        CharacterStats player = CombatSystem.Instance.playerStats;
        if (player == null || player.isDead) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
if (dist < engagementRadius)
        {
            Debug.Log($"[POI] {enemyType} at {name} engaging player at distance {dist}");
            CombatSystem.Instance.StartCombat(stats);
        }
    }

    public void EnsureEnemy()
    {
        CharacterStats existing = GetComponentInChildren<CharacterStats>();
        if (existing != null)
        {
            string expectedPrefix = enemyType.ToString();
            bool matches = existing.name.StartsWith(expectedPrefix) || (enemyType == EnemyType.DragonBob && existing.name.StartsWith("Red")) || (enemyType == EnemyType.Worm && existing.name.StartsWith("Worm"));
            if (!matches)
            {
                DestroyImmediate(existing.gameObject);
                SpawnEnemy();
            }
            else
            {
                // Ensure existing enemies have their unique behavior components
                switch (enemyType)
                {
                    case EnemyType.TreasureChest:
                        if (existing.GetComponent<ChestEnemy>() == null) existing.gameObject.AddComponent<ChestEnemy>();
                        var obstacle = existing.GetComponent<UnityEngine.AI.NavMeshObstacle>();
                        if (obstacle == null) 
                        {
                            obstacle = existing.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                            obstacle.carving = true;
                            obstacle.center = new Vector3(0, 0.5f, 0);
                            obstacle.size = new Vector3(1.2f, 1.0f, 1.2f);
                        }
                        break;
                    case EnemyType.Worm:
                        if (existing.GetComponent<WormEnemy>() == null) existing.gameObject.AddComponent<WormEnemy>();
                        break;
                    case EnemyType.Mushroom:
                        if (existing.GetComponent<MushroomEnemy>() == null) existing.gameObject.AddComponent<MushroomEnemy>();
                        break;
                    case EnemyType.Orc:
                        if (existing.GetComponent<OrcPatrol>() == null) existing.gameObject.AddComponent<OrcPatrol>();
                        break;
                    case EnemyType.DragonBob:
                        if (existing.GetComponent<DragonBob>() == null) existing.gameObject.AddComponent<DragonBob>();
                        break;
                }
            }
        }
        else
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        GameObject prefab = null;
        switch(enemyType)
        {
            case EnemyType.Orc: prefab = orcPrefab; break;
            case EnemyType.TreasureChest: prefab = chestPrefab; break;
            case EnemyType.Mushroom: prefab = mushroomPrefab; break;
            case EnemyType.DragonBob: prefab = dragonPrefab; break;
            case EnemyType.Worm: prefab = wormPrefab; break;
        }

        if (prefab == null) return;

        GameObject enemy = Instantiate(prefab, transform.position, transform.rotation, transform);
        enemy.name = enemyType.ToString() + "_" + name;
        
        if (enemyType == EnemyType.Worm)
        {
            enemy.transform.position += Vector3.down * 0.25f; // Sink him slightly
        }

        if (enemyType != EnemyType.DragonBob && enemyType != EnemyType.Worm)
{
            enemy.transform.localScale = Vector3.one * 0.75f;
        }

        CharacterStats stats = enemy.GetComponent<CharacterStats>();
        if (stats == null) stats = enemy.AddComponent<CharacterStats>();
        
        bool isFTUE = FTUEManager.Instance != null && FTUEManager.Instance.isFTUEActive;
        float difficultyScale = isFTUE ? 0.6f : 1.0f;

        if (enemyType == EnemyType.TreasureChest || enemyType == EnemyType.Mushroom)
        {
            bool isChest = enemyType == EnemyType.TreasureChest;
            stats.brawn = Mathf.RoundToInt((isChest ? 18 : 12) * difficultyScale);
            stats.grit = Mathf.RoundToInt((isChest ? 12 : 5) * difficultyScale);
            stats.finesse = 5;
            stats.ResetStats();
            var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            if (isChest)
            {
                if (enemy.GetComponent<ChestEnemy>() == null) enemy.AddComponent<ChestEnemy>();
                var obstacle = enemy.GetComponent<UnityEngine.AI.NavMeshObstacle>();
                if (obstacle == null) obstacle = enemy.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obstacle.carving = true;
                obstacle.center = new Vector3(0, 0.5f, 0);
                obstacle.size = new Vector3(1.2f, 1.0f, 1.2f);
            }
            else // Mushroom
            {
                if (enemy.GetComponent<MushroomEnemy>() == null) enemy.AddComponent<MushroomEnemy>();
            }
        }
        else if (enemyType == EnemyType.DragonBob)
        {
            if (enemy.GetComponent<DragonBob>() == null) enemy.AddComponent<DragonBob>();
            var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;
        }
        else if (enemyType == EnemyType.Worm)
        {
            stats.brawn = Mathf.RoundToInt(10 * difficultyScale); 
            stats.grit = Mathf.RoundToInt(8 * difficultyScale); 
            stats.ResetStats();
            if (enemy.GetComponent<WormEnemy>() == null) enemy.AddComponent<WormEnemy>();
        }
        else
        {
            stats.brawn = Mathf.RoundToInt(8 * difficultyScale); 
            stats.ResetStats();
            if (enemy.GetComponent<OrcPatrol>() == null) enemy.AddComponent<OrcPatrol>();
        }

        if (healthCanvasPrefab != null && enemy.GetComponentInChildren<HealthBar>() == null)
        {
            GameObject canvas = Instantiate(healthCanvasPrefab, enemy.transform);
            canvas.name = "HealthCanvas";
            float height = 2.5f;
            switch (enemyType)
            {
                case EnemyType.Orc: height = 3.0f; break;
                case EnemyType.Mushroom: height = 2.0f; break;
                case EnemyType.TreasureChest: height = 2.2f; break;
                case EnemyType.DragonBob: height = 5.5f; break;
                case EnemyType.Worm: height = 2.8f; break;
            }
            canvas.transform.localPosition = new Vector3(0, height, 0);
            var bar = canvas.GetComponentInChildren<HealthBar>();
            if (bar != null) bar.stats = stats;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, engagementRadius);

        if (enemyType == EnemyType.Orc)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, patrolRadius);
        }
    }
}