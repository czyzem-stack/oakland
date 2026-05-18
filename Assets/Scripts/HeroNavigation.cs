using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class HeroNavigation : MonoBehaviour
{
    [Header("Navigation Settings")]
    public Transform poiRoot;
    public float metersPerDicePoint = 1.0f;
    public float arrivalDistance = 1.0f;
    public GameObject coinPrefab;

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

    public float DistanceToTarget
    {
        get
        {
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
            agent.autoBraking = true;
            agent.stoppingDistance = arrivalDistance;
        }
        
        if (poiRoot != null)
        {
            ResetPOIs();
        }
        lastPosition = transform.position;
    }

    void Update()
    {
        if (agent == null) return;

        // Skip animation syncing if in combat - let CombatSystem control it
        if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat) return;

        if (isMoving && remainingMeters > 0)
        {
            // Calculate distance moved this frame
            float distMoved = Vector3.Distance(transform.position, lastPosition);
            remainingMeters -= distMoved;
            metersTraveledSinceLastCoin += distMoved;

            if (metersTraveledSinceLastCoin >= 3f)
            {
                // We could reward directly, but the user wants spinning coins to collect
                // Spawning them ahead of time is better.
                metersTraveledSinceLastCoin = 0f;
            }

            lastPosition = transform.position;

            // Sync animation
            float speed = agent.velocity.magnitude / agent.speed;
            if (animator != null) animator.SafeSetFloat("Speed", speed);

            // Check if arrived at POI
            if (currentTarget != null && !agent.pathPending && agent.isOnNavMesh && agent.remainingDistance <= agent.stoppingDistance)
            {
                Debug.Log($"[HeroNavigation] Reached POI: {currentTarget.name}. Total meters remaining: {remainingMeters:F2}");
                OnReachedPOI();
            }
            // Check if out of fuel
            else if (remainingMeters <= 0)
            {
                float dist = agent.isOnNavMesh ? agent.remainingDistance : Vector3.Distance(transform.position, currentTarget.position);
                Debug.Log($"[HeroNavigation] Stopped: Out of distance ({totalDistanceForCurrentMove:F2}m target met). Distance to POI: {dist:F2}m");
                StopMoving("Out of distance");
            }
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
        if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat)
        {
            CombatSystem.Instance.OnPlayerRoll(totalValue);
            return;
        }

        EnsureComponents();

        // Ensure agent is on NavMesh before calculating path
        if (agent != null && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }

        float gainedDistance = totalValue * metersPerDicePoint;
        remainingMeters += gainedDistance;
        totalDistanceForCurrentMove = gainedDistance;
        Debug.Log($"[HeroNavigation] Dice Result: {totalValue}. Gained {gainedDistance:F2}m. Total pool: {remainingMeters:F2}m.");
        
        if (currentTarget == null)
        {
            SelectNextPOI();
        }

        if (currentTarget != null && agent != null && agent.isOnNavMesh)
        {
            if (!spawnedCoinsForCurrentTarget)
            {
                SpawnCoinsAlongPath();
            }
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

            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                Vector3 start = path.corners[i];
                Vector3 end = path.corners[i + 1];
                float segmentLen = Vector3.Distance(start, end);

                while (currentDist + segmentLen >= nextCoinDist)
                {
                    float t = (nextCoinDist - currentDist) / segmentLen;
                    Vector3 spawnPos = Vector3.Lerp(start, end, t);
                    
                    if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                    {
                        spawnPos = hit.position + Vector3.up * 0.7f; 
                    }

                    GameObject coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
                    coin.transform.localScale = Vector3.one * 6f; 
                    if (coin.GetComponent<Coin>() == null) coin.AddComponent<Coin>();
                    
                    nextCoinDist += coinInterval;
                }
                currentDist += segmentLen;
            }
        }
    }

    private void SelectNextPOI()
    {
        spawnedCoinsForCurrentTarget = false;
        if (availablePOIs.Count == 0)
        {
            ResetPOIs();
        }

        if (availablePOIs.Count > 0)
        {
            int index = Random.Range(0, availablePOIs.Count);
            currentTarget = availablePOIs[index];
            availablePOIs.RemoveAt(index);
            Debug.Log($"[HeroNavigation] Target POI: {currentTarget.name}");
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

    private void StopMoving(string reason)
    {
        Debug.Log($"[HeroNavigation] Stopping: {reason}");
        isMoving = false;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        remainingMeters = Mathf.Max(0, remainingMeters);
    }

    public void ResumeAfterCombat()
    {
        Debug.Log("[HeroNavigation] Resuming movement after combat.");
        if (remainingMeters > 0.1f)
        {
            StartMoving();
        }
        else
        {
            StopMoving("Post-Combat Idle");
        }
    }

    private void OnReachedPOI()
    {
        Debug.Log($"[HeroNavigation] Reached target: {currentTarget.name}. Remaining pool: {remainingMeters:F2}m.");
        
        // Check for Enemy at POI
        CharacterStats enemyStats = currentTarget.GetComponentInChildren<CharacterStats>();
        if (enemyStats != null && !enemyStats.isDead)
        {
            // IMPACT DAMAGE: If we have moves left, we "charge" into them
            if (remainingMeters > 0.1f)
            {
                // Damage = remaining meters * (current HP / 10)
                // Proportional to speed (meters) and fitness (remaining HP)
                int impactDamage = Mathf.CeilToInt(remainingMeters * (stats.currentHP / 10f));
                enemyStats.TakeDamage(impactDamage);
                
                Debug.Log($"[HeroNavigation] IMPACT! Dealt {impactDamage} damage to {enemyStats.name}.");
                
                if (CombatSystem.Instance != null)
                {
                    CombatSystem.Instance.SpawnDamageText(enemyStats.transform.position + Vector3.up * 2f, $"IMPACT! -{impactDamage}", Color.yellow);
                    Camera.main.GetComponent<CameraFollow>()?.Shake(0.3f, 0.4f);
                }

                if (animator != null) animator.SafeSetTrigger("Attack");
                
                remainingMeters = 0; // Move pool consumed by the impact

                if (enemyStats.isDead)
                {
                    if (animator != null) animator.SafeSetTrigger("Victory");
                    Animator enemyAnim = enemyStats.GetComponent<Animator>();
                    
                    bool isChest = enemyStats.name.Contains("TreasureChest") || enemyStats.name.Contains("Chest");
                    bool isDragon = enemyStats.name.Contains("DragonBob");

                    if (enemyAnim != null)
                    {
                        if (isDragon) enemyAnim.CrossFade("Die", 0.1f);
                        else enemyAnim.SafeSetTrigger("Die");
                    }
                    
                    if (isChest) stats.AddGold(Random.Range(20, 51));
                    else if (isDragon) stats.AddGold(Random.Range(100, 201));
                    else stats.AddGold(Random.Range(5, 11)); // Orcs/Mushrooms

                    GameObject.Destroy(enemyStats.gameObject, isDragon ? 3f : 2f);
                    StopMoving("Enemy Defeated by Impact");
                    
                    if (isChest && TreasureUpgradeUI.Instance != null)
                    {
                        TreasureUpgradeUI.Instance.ShowUpgrade(stats);
                    }

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

        if (remainingMeters > 0.1f)
        {
            Debug.Log("[HeroNavigation] Distance remains, continuing to next POI.");
            if (!spawnedCoinsForCurrentTarget)
            {
                SpawnCoinsAlongPath();
            }
            StartMoving();
        }
        else
        {
            StopMoving("Target Reached");
        }
    }

    private void ResetPOIs()
    {
        availablePOIs.Clear();
        foreach (Transform child in poiRoot)
        {
            availablePOIs.Add(child);
        }
        Debug.Log($"POI List Reset. {availablePOIs.Count} points available.");
    }
}

