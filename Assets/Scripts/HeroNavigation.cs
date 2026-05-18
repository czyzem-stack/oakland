using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class HeroNavigation : MonoBehaviour
{
    [Header("Navigation Settings")]
    public Transform poiRoot;
    public float metersPerDicePoint = 2.5f; // Increased from 1.0 for longer strides
    public float arrivalDistance = 1.0f;
    public GameObject coinPrefab;
    public GameObject wormPrefab;

    private NavMeshAgent agent;
    private Animator animator;
    private List<Transform> availablePOIs = new List<Transform>();
    private Transform currentTarget;
    private CharacterStats stats;

    [Header("Status")]
    public float remainingMeters = 0f;
    public bool isMoving = false;
    private Vector3 lastPosition;
    private float totalDistanceForCurrentMove = 0f;
    private float metersTraveledSinceLastCoin = 0f;
    private bool spawnedCoinsForCurrentTarget = false;

    private static int rollsCount = 0;

    public void TrySpawnWorm(Vector3 position)
    {
        if (wormPrefab == null) return;
        if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat) return;
        if (GameSettings.Instance != null && !GameSettings.Instance.AreWormsUnlocked()) return;

        if (Random.value < 0.1f)
        {
            Debug.Log("[HeroNavigation] WORM AMBUSH!");
            GameObject worm = Instantiate(wormPrefab, position, Quaternion.identity);
            worm.name = "WormMonster_" + System.Guid.NewGuid().ToString().Substring(0, 5);
            CharacterStats wormStats = worm.GetComponent<CharacterStats>();
            if (wormStats == null) wormStats = worm.AddComponent<CharacterStats>();
            wormStats.brawn = 15; wormStats.grit = 10; wormStats.ResetStats();

            PointOfInterest poi = Object.FindAnyObjectByType<PointOfInterest>();
            if (poi != null && poi.healthCanvasPrefab != null)
            {
                GameObject canvas = Instantiate(poi.healthCanvasPrefab, worm.transform);
                canvas.name = "HealthCanvas";
                canvas.transform.localPosition = new Vector3(0, 2.2f, 0);
                var bar = canvas.GetComponentInChildren<HealthBar>();
                if (bar != null) bar.stats = wormStats;
            }
            CombatSystem.Instance.StartCombat(wormStats);
        }
    }

    public float DistanceToTarget
    {
        get {
            if (currentTarget == null || agent == null || !agent.isOnNavMesh) return 0f;
            if (agent.pathPending) return Vector3.Distance(transform.position, currentTarget.position);
            return agent.remainingDistance;
        }
    }

    public string TargetName => currentTarget != null ? currentTarget.name : "None";

    private void EnsureComponents()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        if (stats == null) stats = GetComponent<CharacterStats>();
    }

    void Start()
    {
        EnsureComponents();
        if (agent != null) 
        { 
            agent.autoBraking = false; // Disable braking for more consistent speed
            agent.stoppingDistance = arrivalDistance;
            agent.acceleration = 20f; // High acceleration for 'buttery' feel
            agent.angularSpeed = 600f; // Fast turning
        }
        if (poiRoot != null) ResetPOIs();
        lastPosition = transform.position;
    }

    void Update()
    {
        if (agent == null) return;

        if (agent.enabled && !agent.isOnNavMesh && Time.frameCount % 30 == 0)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas)) agent.Warp(hit.position);
        }

        // REMOVED: Periodic SetDestination was causing stutters/hiccups. 
        // NavMesh carving from Bob's obstacle will be handled automatically by the agent's internal pathfinding.

        if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat) return;

        if (isMoving && remainingMeters > 0)
        {
            // Ensure agent is not stopped if we have distance to cover
            if (agent.isStopped) agent.isStopped = false;

            float distMoved = Vector3.Distance(transform.position, lastPosition);
            remainingMeters -= distMoved;
            metersTraveledSinceLastCoin += distMoved;
            lastPosition = transform.position;

            float speed = agent.velocity.magnitude / agent.speed;
            if (animator != null) animator.SafeSetFloat("Speed", speed);

            if (currentTarget != null && !agent.pathPending && agent.isOnNavMesh && agent.remainingDistance <= agent.stoppingDistance) OnReachedPOI();
            else if (remainingMeters <= 0) StopMoving("Out of distance");
        }
        else
        {
            if (animator != null) animator.SafeSetFloat("Speed", 0f);
            if (isMoving) StopMoving("Movement Paused");
            lastPosition = transform.position; 
        }
    }

    public void OnDiceRolled(int totalValue)
    {
        rollsCount++;
        if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat)
        {
            CombatSystem.Instance.OnPlayerRoll(totalValue);
            return;
        }

        EnsureComponents();
        if (agent != null && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas)) agent.Warp(hit.position);
        }

        float gainedDistance = totalValue * metersPerDicePoint;
        remainingMeters += gainedDistance;
        totalDistanceForCurrentMove = gainedDistance;
        
        if (currentTarget == null) SelectNextPOI();

        if (currentTarget != null && agent != null && agent.isOnNavMesh)
        {
            if (!spawnedCoinsForCurrentTarget) SpawnCoinsAlongPath();
            StartMoving();
        }
    }

    private void SpawnCoinsAlongPath()
    {
        if (coinPrefab == null || agent == null || currentTarget == null) return;
        spawnedCoinsForCurrentTarget = true;
        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(currentTarget.position, path))
        {
            float currentDist = 0;
            float coinInterval = 2.5f; 
            float nextCoinDist = 2.0f; 
            int safetyBreak = 0;
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                Vector3 start = path.corners[i]; Vector3 end = path.corners[i + 1];
                float segmentLen = Vector3.Distance(start, end);
                while (currentDist + segmentLen >= nextCoinDist)
                {
                    if (safetyBreak++ > 1000) break;
                    float t = (nextCoinDist - currentDist) / segmentLen;
                    Vector3 spawnPos = Vector3.Lerp(start, end, t);
                    if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 3f, NavMesh.AllAreas)) spawnPos = hit.position + Vector3.up * 0.7f;
                    GameObject coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
                    coin.transform.localScale = Vector3.one * 6f; 
                    if (coin.GetComponent<Coin>() == null) coin.AddComponent<Coin>();
                    nextCoinDist += coinInterval;
                }
                currentDist += segmentLen;
                if (safetyBreak > 1000) break;
            }
        }
    }

    private void SelectNextPOI()
    {
        spawnedCoinsForCurrentTarget = false;
        if (availablePOIs.Count == 0) ResetPOIs();

        if (availablePOIs.Count > 0)
        {
            int index = -1;
            if (rollsCount <= 3) // FTUE: Prioritize chests for first 3 rolls
            {
                for (int i = 0; i < availablePOIs.Count; i++)
                {
                    PointOfInterest poi = availablePOIs[i].GetComponent<PointOfInterest>();
                    if (poi != null && poi.enemyType == EnemyType.TreasureChest) { index = i; break; }
                }
            }

            if (index == -1) index = Random.Range(0, availablePOIs.Count);

            currentTarget = availablePOIs[index];
            availablePOIs.RemoveAt(index);
            Debug.Log($"[HeroNavigation] Target POI: {currentTarget.name} (FTUE Filter: {rollsCount <= 3})");
        }
    }

    private void StartMoving()
    {
        if (currentTarget != null && agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
            isMoving = true;
            lastPosition = transform.position;
        }
    }

    public void StopMoving(string reason)
    {
        Debug.Log($"[HeroNavigation] Stopping: {reason}");
        isMoving = false;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        remainingMeters = Mathf.Max(0, remainingMeters);
    }

    public void ResumeAfterCombat()
    {
        if (remainingMeters > 0.1f) StartMoving();
        else StopMoving("Post-Combat Idle");
    }

    private void OnReachedPOI()
    {
        CharacterStats enemyStats = currentTarget.GetComponentInChildren<CharacterStats>();
        if (enemyStats != null && !enemyStats.isDead)
        {
            if (remainingMeters > 0.1f)
            {
                int impactDamage = Mathf.CeilToInt(remainingMeters * (stats.currentHP / 10f));
                enemyStats.TakeDamage(impactDamage);
                if (CombatSystem.Instance != null) { CombatSystem.Instance.SpawnDamageText(enemyStats.transform.position + Vector3.up * 2f, $"IMPACT! -{impactDamage}", Color.yellow); Camera.main.GetComponent<CameraFollow>()?.Shake(0.3f, 0.4f); }
                if (animator != null) animator.SafeSetTrigger("Attack");
                remainingMeters = 0;

                if (enemyStats.isDead)
                {
                    if (animator != null) animator.SafeSetTrigger("Victory");
                    Animator enemyAnim = enemyStats.GetComponent<Animator>();
                    bool isChest = enemyStats.name.Contains("Chest");
                    bool isDragon = enemyStats.name.Contains("DragonBob");
                    bool isOrc = enemyStats.name.Contains("Orc");
                    if (isOrc) GameSettings.Instance?.RegisterOrcKill();
                    if (enemyAnim != null) { if (isDragon) enemyAnim.CrossFade("Die", 0.1f); else enemyAnim.SafeSetTrigger("Die"); }
                    if (isChest) stats.AddGold(Random.Range(20, 51));
                    else if (isDragon) stats.AddGold(Random.Range(100, 201));
                    else stats.AddGold(Random.Range(5, 11));
                    GameObject.Destroy(enemyStats.gameObject, isDragon ? 3f : 2f);
                    StopMoving("Enemy Defeated by Impact");
                    if (isChest && TreasureUpgradeUI.Instance != null) TreasureUpgradeUI.Instance.ShowUpgrade(stats);
                    currentTarget = null;
                    return;
                }
            }
            StopMoving("Enemy Encountered");
            CombatSystem.Instance.StartCombat(enemyStats);
            return;
        }

        currentTarget = null;
        SelectNextPOI();
        if (remainingMeters > 0.1f) { if (!spawnedCoinsForCurrentTarget) SpawnCoinsAlongPath(); StartMoving(); }
        else StopMoving("Target Reached");
    }

    private void ResetPOIs()
    {
        availablePOIs.Clear();
        foreach (Transform child in poiRoot) availablePOIs.Add(child);
        Debug.Log($"POI List Reset. {availablePOIs.Count} points available.");
    }
}