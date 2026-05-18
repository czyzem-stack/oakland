using UnityEngine;

public enum EnemyType
{
    Orc,
    TreasureChest,
    Mushroom,
    DragonBob
}

public class PointOfInterest : MonoBehaviour
{
    public EnemyType enemyType = EnemyType.Orc;
    
    [Header("Prefabs")]
    public GameObject orcPrefab;
    public GameObject chestPrefab;
    public GameObject mushroomPrefab;
    public GameObject dragonPrefab;
    public GameObject healthCanvasPrefab;

    private void Start()
    {
        EnsureEnemy();
    }

    public void EnsureEnemy()
    {
        // Remove existing enemy children if they don't match the selection
        CharacterStats existing = GetComponentInChildren<CharacterStats>();
        if (existing != null)
        {
            string expectedPrefix = enemyType.ToString();
            // Special check for Bob since his name might differ from prefix "DragonBob"
            bool matches = existing.name.StartsWith(expectedPrefix) || (enemyType == EnemyType.DragonBob && existing.name.StartsWith("Red"));
            if (!matches)
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
        GameObject prefab = null;
        switch(enemyType)
        {
            case EnemyType.Orc: prefab = orcPrefab; break;
            case EnemyType.TreasureChest: prefab = chestPrefab; break;
            case EnemyType.Mushroom: prefab = mushroomPrefab; break;
            case EnemyType.DragonBob: prefab = dragonPrefab; break;
        }

        if (prefab == null) return;

        GameObject enemy = Instantiate(prefab, transform.position, transform.rotation, transform);
        enemy.name = enemyType.ToString() + "_" + name;
        
        // Dragon doesn't need the 0.75 scaling or nav setup
        if (enemyType != EnemyType.DragonBob)
        {
            enemy.transform.localScale = Vector3.one * 0.75f;
        }

        // Setup CharacterStats if missing
        CharacterStats stats = enemy.GetComponent<CharacterStats>();
        if (stats == null) stats = enemy.AddComponent<CharacterStats>();
        
        // Base Stats setup
        if (enemyType == EnemyType.TreasureChest || enemyType == EnemyType.Mushroom)
        {
            stats.brawn = (enemyType == EnemyType.TreasureChest) ? 10 : 12;
            stats.finesse = 5;
            stats.grit = 5;
            stats.ResetStats();
            
            // Static enemies - disable navigation
            var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;
        }
        else if (enemyType == EnemyType.DragonBob)
        {
            if (enemy.GetComponent<DragonBob>() == null)
                enemy.AddComponent<DragonBob>();
                
            // Ensure no navmesh agent on dragon
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
            
            // Adjust height based on enemy type
            float height = 2.0f;
            switch (enemyType)
            {
                case EnemyType.Orc: height = 2.5f; break;
                case EnemyType.Mushroom: height = 1.6f; break;
                case EnemyType.TreasureChest: height = 1.8f; break;
                case EnemyType.DragonBob: height = 4.0f; break;
            }
            canvas.transform.localPosition = new Vector3(0, height, 0);

            var bar = canvas.GetComponentInChildren<HealthBar>();
            if (bar != null) bar.stats = stats;
        }
}
}
