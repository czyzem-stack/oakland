using UnityEngine;

public enum EnemyType
{
    Orc,
    TreasureChest
}

public class PointOfInterest : MonoBehaviour
{
    public EnemyType enemyType = EnemyType.Orc;
    
    [Header("Prefabs")]
    public GameObject orcPrefab;
    public GameObject chestPrefab;
    public GameObject healthCanvasPrefab;

    private void Start()
    {
        EnsureEnemy();
    }

    public void EnsureEnemy()
    {
        // Remove existing enemy children if they don't match the selection
        // We look for objects with CharacterStats
        CharacterStats existing = GetComponentInChildren<CharacterStats>();
        if (existing != null)
        {
            string expectedPrefix = enemyType.ToString();
            if (!existing.name.StartsWith(expectedPrefix))
            {
                DestroyImmediate(existing.gameObject);
                SpawnEnemy();
            }
        }
        else
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        GameObject prefab = (enemyType == EnemyType.Orc) ? orcPrefab : chestPrefab;
        if (prefab == null) return;

        GameObject enemy = Instantiate(prefab, transform.position, transform.rotation, transform);
        enemy.name = enemyType.ToString() + "_" + name;

        // Setup CharacterStats if missing
        CharacterStats stats = enemy.GetComponent<CharacterStats>();
        if (stats == null) stats = enemy.AddComponent<CharacterStats>();
        
        // Base Stats setup
        if (enemyType == EnemyType.TreasureChest)
        {
            stats.brawn = 10;
            stats.finesse = 5;
            stats.grit = 5;
            stats.ResetStats();
            
            // Chests are static
            var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;
        }
        else
        {
            stats.brawn = 8; // Orc base
            stats.ResetStats();

            // Orcs patrol
            if (enemy.GetComponent<OrcPatrol>() == null)
                enemy.AddComponent<OrcPatrol>();
        }

        // Add Health Canvas if missing
        if (healthCanvasPrefab != null && enemy.GetComponentInChildren<HealthBar>() == null)
        {
            GameObject canvas = Instantiate(healthCanvasPrefab, enemy.transform);
            canvas.name = "HealthCanvas";
            // HealthBar needs to know about stats
            var bar = canvas.GetComponentInChildren<HealthBar>();
            if (bar != null) bar.stats = stats;
        }
        }
        }
