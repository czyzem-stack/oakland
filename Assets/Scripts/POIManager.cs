using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class POIManager : MonoBehaviour
{
    public static POIManager Instance;

    [Header("Configuration")]
    [Range(10, 100)] public int totalSpots = 50;
    [Range(5, 30)] public int maxActiveAtOnce = 12;
    [Range(5f, 30f)] public float minDistance = 15f;
    [Range(0, 10)] public int burnoutTurns = 5;

    public GameObject poiBasePrefab;
    public Transform poiRoot;

    [Header("Map Bounds")]
    public float minX = -45f;
    public float maxX = 45f;
    public float minZ = -45f;
    public float maxZ = 45f;

    private List<PointOfInterest> allPOIs = new List<PointOfInterest>();
    private Dictionary<PointOfInterest, CharacterStats> statsCache = new Dictionary<PointOfInterest, CharacterStats>();
    private Dictionary<PointOfInterest, int> burnoutRegistry = new Dictionary<PointOfInterest, int>();
    
    private float updateTimer = 0f;
    private const float updateInterval = 0.5f;

    void Awake()
    {
        Instance = this;
        // Aggressive cleanup on awake to prevent stray enemies from previous runs or scene artifacts
        DeactivateAllPOIs();
    }

    private void DeactivateAllPOIs()
    {
        PointOfInterest[] all = Object.FindObjectsByType<PointOfInterest>(FindObjectsInactive.Include);
        foreach (var poi in all)
        {
            // Only deactivate if it's not a forced FTUE object that was just spawned
            if (!poi.name.Contains("Forced") && !poi.name.Contains("FTUE"))
            {
                poi.gameObject.SetActive(false);
            }
        }
    }

    void Start()
    {
        if (poiRoot == null) poiRoot = transform;
        SetupPOIs();

        // Extra aggressive cleanup for FTUE
        if (FTUEManager.Instance != null && FTUEManager.Instance.isFTUEActive)
        {
            DeactivateAllPOIs();
        }
    }

    void Update()
    {
        // Don't maintain random POIs during FTUE
        if (FTUEManager.Instance != null && FTUEManager.Instance.isFTUEActive)
        {
            return;
        }

        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            MaintainActiveCount();
        }
    }

    public void OnTurnPassed()
    {
        List<PointOfInterest> keys = new List<PointOfInterest>(burnoutRegistry.Keys);
        foreach (var poi in keys)
        {
            burnoutRegistry[poi]--;
            if (burnoutRegistry[poi] <= 0)
            {
                burnoutRegistry.Remove(poi);
                Debug.Log($"[POIManager] POI {poi.name} is back in the pool.");
            }
        }
    }

    public void SetupPOIs()
    {
        PointOfInterest[] existing = poiRoot.GetComponentsInChildren<PointOfInterest>(true);
        allPOIs.Clear();
        statsCache.Clear();
        burnoutRegistry.Clear();
        allPOIs.AddRange(existing);

        foreach (var poi in allPOIs) 
        {
            SnapToSurface(poi.transform);
            CacheStats(poi);
        }

        if (allPOIs.Count < totalSpots)
        {
            SpawnToDensity();
        }

        // Deactivate everything in the scene
        DeactivateAllPOIs();
        
        // Only maintain active count if FTUE is NOT active
        if (FTUEManager.Instance == null || !FTUEManager.Instance.isFTUEActive)
        {
            MaintainActiveCount();
        }
    }

    private void CacheStats(PointOfInterest poi)
    {
        if (poi == null) return;
        CharacterStats stats = poi.GetComponentInChildren<CharacterStats>();
        if (stats != null) statsCache[poi] = stats;
    }

    private void SpawnToDensity()
    {
        GameObject template = allPOIs.Count > 0 ? allPOIs[0].gameObject : poiBasePrefab;
        if (template == null) return;

        int toSpawn = totalSpots - allPOIs.Count;
        List<Vector3> existingPos = new List<Vector3>();
        foreach (var p in allPOIs) existingPos.Add(p.transform.position);

        for (int i = 0; i < toSpawn; i++)
        {
            Vector3 pos;
            if (TryFindSpawnPosition(existingPos, out pos))
            {
                GameObject newGo = Instantiate(template, pos, Quaternion.identity, poiRoot);
                newGo.name = $"POI_Spot_{allPOIs.Count}";
                newGo.SetActive(false);
                PointOfInterest poi = newGo.GetComponent<PointOfInterest>();
                if (poi != null)
                {
                    System.Array types = System.Enum.GetValues(typeof(EnemyType));
                    poi.enemyType = (EnemyType)types.GetValue(Random.Range(0, types.Length));
                    poi.EnsureEnemy();
                    allPOIs.Add(poi);
                    CacheStats(poi);
                    existingPos.Add(pos);
                }
            }
        }
    }

    private void MaintainActiveCount()
    {
        int activeCount = 0;
        List<PointOfInterest> candidatePool = new List<PointOfInterest>();
        
        foreach (var poi in allPOIs)
        {
            if (poi == null) continue;
            
            if (poi.gameObject.activeSelf)
            {
                if (!statsCache.TryGetValue(poi, out CharacterStats stats) || stats == null)
                {
                    stats = poi.GetComponentInChildren<CharacterStats>();
                    if (stats != null) statsCache[poi] = stats;
                }

                if (stats == null || stats.isDead)
                {
                    // Check if this POI is currently in combat
                    bool inCombat = false;
                    if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat)
                    {
                        if (CombatSystem.Instance.currentEnemyStats == stats)
                        {
                            inCombat = true;
                        }
                    }

                    if (!inCombat)
                    {
                        poi.gameObject.SetActive(false);
                        if (burnoutTurns > 0)
                        {
                            burnoutRegistry[poi] = burnoutTurns;
                            Debug.Log($"[POIManager] POI {poi.name} burned. Cooling down for {burnoutTurns} turns.");
                        }
                    }
                }
                else
                {
                    activeCount++;
                }
}
            else
            {
                // Only allow activation if not in burnout
                if (!burnoutRegistry.ContainsKey(poi))
                {
                    candidatePool.Add(poi);
                }
            }
        }

        while (activeCount < maxActiveAtOnce && candidatePool.Count > 0)
        {
            int index = Random.Range(0, candidatePool.Count);
            PointOfInterest toActivate = candidatePool[index];
            
            // Check distance from existing actives
            bool tooClose = false;
            foreach (var activePoi in allPOIs)
            {
                if (activePoi.gameObject.activeSelf && activePoi != toActivate)
                {
                    if (Vector3.Distance(toActivate.transform.position, activePoi.transform.position) < minDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }
            }

            if (!tooClose)
            {
                toActivate.gameObject.SetActive(true);
                toActivate.EnsureEnemy(); 
                activeCount++;
            }
            
            candidatePool.RemoveAt(index);
        }
    }

    private void SnapToSurface(Transform t)
    {
        Vector3 pos = t.position;
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(pos.x, 100f, pos.z), Vector3.down, out hit, 200f))
        {
            t.position = hit.point;
        }
        else
        {
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(pos, out navHit, 50f, NavMesh.AllAreas))
            {
                t.position = navHit.position;
            }
        }
    }

    private bool TryFindSpawnPosition(List<Vector3> existing, out Vector3 position)
    {
        position = Vector3.zero;
        Vector3 safeReference = new Vector3(33.1f, 0f, -0.7f); // Campfire area

        for (int attempts = 0; attempts < 200; attempts++)
        {
            float rx = Random.Range(minX, maxX);
            float rz = Random.Range(minZ, maxZ);
            Vector3 candidate = new Vector3(rx, 50f, rz);
            
            NavMeshHit navHit;
            // Only sample walkable area (Area 0) if possible, or all areas but check normal
            if (NavMesh.SamplePosition(candidate, out navHit, 100f, NavMesh.AllAreas))
            {
                Vector3 finalPos = navHit.position;
                
                // 1. Check for steep slopes
                RaycastHit groundHit;
                if (Physics.Raycast(new Vector3(finalPos.x, finalPos.y + 10f, finalPos.z), Vector3.down, out groundHit, 20f))
                {
                    float angle = Vector3.Angle(groundHit.normal, Vector3.up);
                    if (angle > 30f) continue; // Too steep
                    finalPos = groundHit.point;
                }
                else
                {
                    continue; // No ground found directly below NavMesh point? Skip.
                }

                // 2. Check Reachability (ensure Steve can actually get there)
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(safeReference, finalPos, NavMesh.AllAreas, path))
                {
                    if (path.status != NavMeshPathStatus.PathComplete) continue; // Unreachable
                }
                else
                {
                    continue;
                }

                // 3. Distance check
                bool tooClose = false;
                foreach (var p in existing)
                {
                    if (Vector3.Distance(finalPos, p) < minDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    position = finalPos;
                    return true;
                }
            }
        }
        return false;
    }

    public PointOfInterest ForceSpawnPOI(EnemyType type, Vector3 nearPosition, float minRange, float maxRange)
    {
        for (int i = 0; i < 50; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minRange, maxRange);
            Vector3 candidate = nearPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(candidate, out navHit, 15f, NavMesh.AllAreas))
            {
                GameObject template = allPOIs.Count > 0 ? allPOIs[0].gameObject : poiBasePrefab;
                GameObject newGo = Instantiate(template, navHit.position, Quaternion.identity, poiRoot);
                newGo.name = $"Forced_{type}_{System.Guid.NewGuid().ToString().Substring(0,4)}";
                PointOfInterest poi = newGo.GetComponent<PointOfInterest>();
                poi.enemyType = type;
                poi.EnsureEnemy();
                allPOIs.Add(poi);
                CacheStats(poi);
                newGo.SetActive(true);
                return poi;
            }
        }
        return null;
    }
}

