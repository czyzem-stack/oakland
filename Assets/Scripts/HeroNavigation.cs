using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class HeroNavigation : MonoBehaviour
{
    private const int NavSampleMask = 1 << 0; // Walkable area only

    [Header("Navigation Settings")]
    public Transform poiRoot;
    public float metersPerDicePoint = 2.5f;
    public float arrivalDistance = 1.0f;
    public GameObject coinPrefab;
    public GameObject wormPrefab;

    private NavMeshAgent agent;
    private Animator animator;
    private List<Transform> availablePOIs = new List<Transform>();
    private List<GameObject> activeCoins = new List<GameObject>();
    private Transform currentTarget;
    private CharacterStats stats;
    private Camera mainCam;

    [Header("Status")]
    public float remainingMeters = 0f;
    public bool isMoving = false;
    private Vector3 lastPosition;
    private float metersTraveledSinceLastCoin = 0f;
    private bool spawnedCoinsForCurrentTarget = false;
    private float pathDistanceToTarget = 0f;

    private int rollsCount = 0;
    private int lastDiceTotal = 0;
    private float lastRollMeters = 0f;

    public bool IsStuck => isMoving && stuckTimer > 1.5f;
    public int LastDiceTotal => lastDiceTotal;
    public float LastRollMeters => lastRollMeters;
    public float PathDistanceToTarget => pathDistanceToTarget;

    public void TrySpawnWorm(Vector3 position)
    {
        if (wormPrefab == null) return;
        if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat) return;
        
        // Prevent random worms during the tutorial
        if (FTUEManager.Instance != null && FTUEManager.Instance.isFTUEActive) return;

        if (GameSettings.Instance != null && !GameSettings.Instance.AreWormsUnlocked()) return;

        if (Random.value < 0.1f)
        {
            Vector3 spawnPos = position;
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 3.0f, NavSampleMask))
                spawnPos = hit.position;
            spawnPos.y -= 0.25f;

            GameObject worm = Instantiate(wormPrefab, spawnPos, Quaternion.identity);
            worm.name = "WormMonster_" + System.Guid.NewGuid().ToString().Substring(0, 5);
            CharacterStats wormStats = worm.GetComponent<CharacterStats>();
            if (wormStats == null) wormStats = worm.AddComponent<CharacterStats>();
            wormStats.brawn = 15;
            wormStats.grit = 10;
            wormStats.ResetStats();

            if (worm.GetComponent<WormEnemy>() == null) worm.AddComponent<WormEnemy>();

            PointOfInterest poi = Object.FindAnyObjectByType<PointOfInterest>();
            if (poi != null && poi.healthCanvasPrefab != null)
            {
                GameObject canvas = Instantiate(poi.healthCanvasPrefab, worm.transform);
                canvas.name = "HealthCanvas";
                canvas.transform.localPosition = new Vector3(0, 3.0f, 0);
                var bar = canvas.GetComponentInChildren<HealthBar>();
                if (bar != null) bar.stats = wormStats;
            }

            if (CombatSystem.Instance != null)
                CombatSystem.Instance.StartCombat(wormStats);
        }
    }

    public float DistanceToTarget
    {
        get
        {
            if (currentTarget == null || agent == null || !agent.isOnNavMesh) return 0f;
            if (agent.pathPending) return pathDistanceToTarget;
            return agent.remainingDistance;
        }
    }

    public string TargetName => currentTarget != null ? currentTarget.name : "None";

    private void EnsureComponents()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
        if (stats == null) stats = GetComponent<CharacterStats>();
        if (mainCam == null) mainCam = Camera.main;
    }

    private bool TryWarpToNavMesh(float maxDistance = 10f)
    {
        if (agent == null) return false;
        
        // Ensure agent is enabled before checking or warping
        if (!agent.enabled) agent.enabled = true;

        // 1. If we are currently traversing a link (like a bridge or jump), DO NOT WARP.
        if (agent.isOnOffMeshLink) return true;

        // 2. Significant drift recovery or grounding check
        // We use a larger search radius if we are currently "off mesh"
        float searchRadius = agent.isOnNavMesh ? maxDistance : maxDistance * 2f;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, searchRadius, NavSampleMask))
        {
            float dist = Vector3.Distance(transform.position, hit.position);
            
            // CRITICAL: If agent.isOnNavMesh is false, we MUST warp to get back on it,
            // even if the distance is 0. This fixes the "False but near" state.
            if (!agent.isOnNavMesh || dist > 1.25f) 
            {
                Debug.Log($"[HeroNavigation] Grounding Steve. Pos: {transform.position}, Hit: {hit.position}, Dist: {dist:F2}, WasOnMesh: {agent.isOnNavMesh}");
                agent.Warp(hit.position);
                
                if (CombatSystem.Instance != null && dist > 1.0f)
                {
                    CombatSystem.Instance.SpawnDamageText(hit.position + Vector3.up * 1.5f, "GROUNDED", Color.white);
                }
            }
            return true;
        }

        return agent.isOnNavMesh;
    }

    private bool CanControlAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private float currentAnimSpeed = 0f;

    void Start()
    {
        EnsureComponents();
        rollsCount = 0;
        if (GameSettings.Instance != null)
            metersPerDicePoint = GameSettings.Instance.metersPerDicePoint;

        // Warp to spawn point if available, unless FTUE is active and will handle it
        bool isFTUE = FTUEManager.Instance != null && FTUEManager.Instance.isFTUEActive;
        PlayerStart spawn = Object.FindAnyObjectByType<PlayerStart>();
        
        if (spawn != null && !isFTUE)
        {
            // Disable agent briefly to warp
            if (agent != null) agent.enabled = false;
            transform.position = spawn.transform.position;
            transform.rotation = spawn.transform.rotation;
            if (agent != null) agent.enabled = true;
            
            Debug.Log($"[HeroNavigation] Warped Steve to spawn point: {spawn.name} at {spawn.transform.position}");
        }
        else if (isFTUE)
        {
            Debug.Log("[HeroNavigation] Skipping default spawn warp: FTUE is active.");
        }
        else
        {
            Debug.LogWarning("[HeroNavigation] No PlayerStart found in scene.");
        }

        if (agent != null)
        {
            agent.autoBraking = true;
            agent.stoppingDistance = arrivalDistance;
            agent.acceleration = 25f;
            agent.angularSpeed = 600f;
            TryWarpToNavMesh();
        }
        if (animator != null) animator.applyRootMotion = false;
        if (poiRoot != null) ResetPOIs();
        lastPosition = transform.position;
    }

    private float lastTargetSearchTime = 0f;
    private const float targetSearchCooldown = 1.0f;
    private float stuckTimer = 0f;
    private bool tutorialTransitionFlag = false;

    public void NotifyTutorialCompleted()
    {
        tutorialTransitionFlag = true;
        StopMoving("Tutorial End");
        ClearTarget();
        remainingMeters = 0f;
        StartCoroutine(ResetTransitionFlag());
    }

    private IEnumerator ResetTransitionFlag()
    {
        yield return new WaitForSeconds(0.5f);
        tutorialTransitionFlag = false;
    }

    void Update()
    {
        if (agent == null || stats.isDead || tutorialTransitionFlag) return;

        // 1. Popup Lock
        if (GenericPopup.IsOpen || EquipmentLootPopup.IsOpen)
        {
            if (isMoving) StopMoving("Popup open");
            UpdateAnimation(0f);
            return;
        }

        // 2. Combat Lock - More robust check
        bool inCombatTransition = CombatSystem.Instance != null && (CombatSystem.Instance.isInCombat || CombatSystem.Instance.isCombatEnding);
        if (inCombatTransition)
        {
            if (isMoving) StopMoving("Combat active");
            UpdateAnimation(0f);
            return;
        }

        // 3. Movement Management
        if (remainingMeters > 0.01f)
        {
            if (!isMoving)
            {
                if (currentTarget == null && Time.time > lastTargetSearchTime + targetSearchCooldown)
                {
                    lastTargetSearchTime = Time.time;
                    SelectNextPOI();
                }
                
                if (currentTarget != null) StartMoving();
            }
            
            if (isMoving) HandleActiveMovement();
        }
else if (isMoving)
        {
            StopMoving("Out of distance");
        }
        else
        {
            UpdateAnimation(0f);
            lastPosition = transform.position;
        }
    }

    private void HandleActiveMovement()
    {
        // Force grounding every frame if we are supposed to be moving
        if (!agent.isOnNavMesh)
        {
            if (!TryWarpToNavMesh(20f)) 
            {
                StopMoving("Lost NavMesh");
                return;
            }
        }

        if (agent.isStopped) agent.isStopped = false;

        float distMoved = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        // Stuck detection
        if (distMoved < 0.005f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 1.5f) // Faster recovery
            {
                Debug.Log("[HeroNavigation] Steve is stuck. Grounding.");
                TryWarpToNavMesh(5f);
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        // Distance tracking
        if (distMoved < 5f) 
        {
            remainingMeters -= distMoved;
            metersTraveledSinceLastCoin += distMoved;
        }

        // ANTI-GLIDE ANIMATION: Check intent vs reality
        float moveIntent = agent.desiredVelocity.magnitude / (agent.speed > 0 ? agent.speed : 1f);
        float actualMotion = (distMoved / Time.deltaTime) / (agent.speed > 0 ? agent.speed : 1f);
        
        // targetAnimSpeed should favor intent so he "runs in place" if blocked
        float targetAnimSpeed = Mathf.Max(moveIntent * 0.8f, Mathf.Clamp01(actualMotion));
        if (remainingMeters > 0.1f && targetAnimSpeed < 0.3f) targetAnimSpeed = 0.3f; // Force movement look

        UpdateAnimation(targetAnimSpeed);

        // Arrival Logic: Increased tolerance for complex POIs
        float directDist = currentTarget != null ? Vector3.Distance(transform.position, currentTarget.position) : float.MaxValue;
        bool reached = currentTarget != null && !agent.pathPending && 
                       (agent.remainingDistance <= agent.stoppingDistance || 
                       (agent.remainingDistance < agent.stoppingDistance + 1.5f && stuckTimer > 0.5f) ||
                       (directDist < agent.stoppingDistance + 1.2f && stuckTimer > 0.3f));

        if (reached)
        {
            OnReachedPOI();
        }
    }

    private void UpdateAnimation(float speed)
    {
        if (animator == null) return;
        currentAnimSpeed = Mathf.Lerp(currentAnimSpeed, speed, Time.deltaTime * 12f);
        animator.SafeSetFloat("Speed", currentAnimSpeed);
    }

    public void OnDiceRolled(int totalValue)
    {
        if (stats == null) EnsureComponents();
        if (stats.isDead) return;

        rollsCount++;
        if (POIManager.Instance != null) POIManager.Instance.OnTurnPassed();

        if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat)
        {
            CombatSystem.Instance.OnPlayerRoll(totalValue);
            return;
        }

        lastDiceTotal = totalValue;
        float gainedDistance = totalValue * metersPerDicePoint;
        lastRollMeters = gainedDistance;
        remainingMeters += gainedDistance;

        // Pick a POI once; all subsequent rolls accumulate toward the same target
        if (currentTarget == null)
            SelectNextPOI();

        if (currentTarget != null && agent != null)
        {
            // Only warp if we're not already on the NavMesh
            if (!agent.isOnNavMesh) TryWarpToNavMesh();
            
            if (agent.isOnNavMesh)
            {
                StartMoving();
            }
        }
        }


    private IEnumerator SpawnCoinsCoroutine()
    {
        if (coinPrefab == null || agent == null || currentTarget == null || stats.isDead) yield break;
        spawnedCoinsForCurrentTarget = true;

        if (!TryBuildPath(currentTarget.position, out NavMeshPath path, out _))
            yield break;

        float coinInterval = 6.0f;
        float nextCoinDist = 6.0f;
        int totalIterations = 0;
        List<Vector3> spawnedPositions = new List<Vector3>();
        float currentDist = 0f;

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Vector3 start = path.corners[i];
            Vector3 end = path.corners[i + 1];
            float segmentLen = Vector3.Distance(start, end);

            while (currentDist + segmentLen >= nextCoinDist)
            {
                totalIterations++;
                if (totalIterations > 100) yield break;

                float t = (nextCoinDist - currentDist) / segmentLen;
                Vector3 spawnPos = Vector3.Lerp(start, end, Mathf.Clamp01(t));

                if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 2f, NavSampleMask))
                    spawnPos = hit.position + Vector3.up * 0.7f;
                else
                    spawnPos.y += 0.7f;

                if (Vector3.Distance(spawnPos, transform.position) < 3.0f)
                {
                    nextCoinDist += coinInterval;
                    continue;
                }

                bool tooClose = false;
                foreach (var pos in spawnedPositions)
                {
                    if (Vector3.Distance(spawnPos, pos) < 4.0f) { tooClose = true; break; }
                }

                if (!tooClose)
                {
                    Collider[] existing = Physics.OverlapSphere(spawnPos, 1.0f);
                    foreach (var col in existing)
                    {
                        if (col.GetComponent<Coin>() != null) { tooClose = true; break; }
                    }
                }

                if (!tooClose)
                {
                    GameObject coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
                    coin.transform.localScale = Vector3.one * 6f;
                    if (coin.GetComponent<Coin>() == null) coin.AddComponent<Coin>();
                    spawnedPositions.Add(spawnPos);
                    activeCoins.Add(coin);
                    if (activeCoins.Count % 3 == 0) yield return null;
                }

                nextCoinDist += coinInterval;
            }

            currentDist += segmentLen;
            if (totalIterations > 100) break;
        }
    }

    private void ClearActiveCoins()
    {
        foreach (var coin in activeCoins) if (coin != null) Destroy(coin);
        activeCoins.Clear();
    }

    private void SelectNextPOI()
    {
        ClearActiveCoins();
        spawnedCoinsForCurrentTarget = false;
        ResetPOIs();

        var ftue = FTUEManager.Instance;
        if (ftue == null) ftue = Object.FindAnyObjectByType<FTUEManager>();

        // Check FTUE first
        if (ftue != null && ftue.isFTUEActive)
        {
            currentTarget = ftue.GetNextTarget(transform.position);
            if (currentTarget != null)
            {
                RefreshPathDistanceToTarget();
            }
            else
            {
                // If FTUE is active but no target found, don't pick a random one
                pathDistanceToTarget = 0f;
            }
            return;
        }

        if (availablePOIs.Count == 0)
{
            currentTarget = null;
            pathDistanceToTarget = 0f;
            return;
        }

        int index = Random.Range(0, availablePOIs.Count);
        currentTarget = availablePOIs[index];
        RefreshPathDistanceToTarget();
    }

    private void RefreshPathDistanceToTarget()
    {
        if (currentTarget == null || agent == null || !agent.isOnNavMesh)
        {
            pathDistanceToTarget = 0f;
            return;
        }

        if (TryBuildPath(currentTarget.position, out _, out float len))
            pathDistanceToTarget = len;
    }

    private void StartMoving()
    {
        if (currentTarget == null || agent == null) return;
        
        // Ensure grounded before setting destination
        if (!TryWarpToNavMesh() || !agent.enabled) 
        { 
            Debug.LogWarning("[HeroNavigation] StartMoving failed: Not grounded or agent disabled.");
            isMoving = false; 
            return; 
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("[HeroNavigation] FATAL: Agent says it is NOT on NavMesh despite warp. Destination setting will fail.");
            isMoving = false;
            return;
        }

        RefreshPathDistanceToTarget();

        float stopDist = arrivalDistance;
        CharacterStats enemyStats = currentTarget.GetComponentInChildren<CharacterStats>();
        if (enemyStats != null && (enemyStats.name.Contains("Chest") || enemyStats.name.Contains("TreasureChest")))
            stopDist = 2.5f; // Forgiving distance for chests
        agent.stoppingDistance = stopDist;

        if (animator != null) animator.ResetToLocomotion(0.2f);

        agent.isStopped = false;
        if (!agent.SetDestination(currentTarget.position))
        {
            Debug.LogWarning($"[HeroNavigation] agent.SetDestination to {currentTarget.name} at {currentTarget.position} FAILED. Re-calculating path...");
            // Force a slight nudge and retry next frame
            isMoving = false;
            return;
        }

        isMoving = true;
        lastPosition = transform.position;
        stuckTimer = 0f;
    }

    private bool TryBuildPath(Vector3 targetPos, out NavMeshPath path, out float pathLen)
    {
        path = new NavMeshPath();
        pathLen = 0f;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit endHit, 5.0f, NavSampleMask))
            targetPos = endHit.position;

        if (!agent.CalculatePath(targetPos, path) || path.status != NavMeshPathStatus.PathComplete)
            return false;

        pathLen = GetPathLength(path);
        return pathLen > 0.01f;
    }

    private static float GetPathLength(NavMeshPath path)
    {
        float len = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
            len += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        return len;
    }

    public bool EnsureOnNavMesh(float maxDistance = 10f) => TryWarpToNavMesh(maxDistance);

    public void ClearTarget()
    {
        currentTarget = null;
        pathDistanceToTarget = 0f;
    }

    public void StopMoving(string reason)
    {
        isMoving = false;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }
        remainingMeters = Mathf.Max(0, remainingMeters);
        UpdateAnimation(0f);
    }

    public void ResumeAfterCombat(string completedTargetName = null)
    {
        if (stats.isDead || tutorialTransitionFlag) return;

        // Clear current target to force a fresh search/next tutorial step
        currentTarget = null;

        // Notify FTUE if a target was completed
        if (!string.IsNullOrEmpty(completedTargetName) && FTUEManager.Instance != null && FTUEManager.Instance.isFTUEActive)
        {
            FTUEManager.Instance.OnStageCompleted(completedTargetName);
        }

        EnsureComponents();
        TryWarpToNavMesh(10f); // More aggressive grounding
        
        if (remainingMeters > 0.1f)
        {
            SelectNextPOI();
            if (currentTarget != null) StartMoving();
            else StopMoving("Post-Combat Idle (No Target)");
        }
        else
        {
            StopMoving("Post-Combat Idle");
        }
    }

    private void OnReachedPOI()
    {
        CharacterStats enemyStats = currentTarget.GetComponentInChildren<CharacterStats>();
        if (enemyStats != null && !enemyStats.isDead)
        {
            if (remainingMeters > 0.1f)
            {
                bool isChestTarget = enemyStats.name.Contains("Chest");
                int impactDamage = Mathf.CeilToInt(remainingMeters * (stats.currentHP / 10f));

                if (!isChestTarget)
                {
                    enemyStats.TakeDamage(impactDamage);
                    if (CombatSystem.Instance != null)
                    {
                        CombatSystem.Instance.SpawnDamageText(enemyStats.transform.position + Vector3.up * 2f, $"IMPACT! -{impactDamage}", Color.yellow);
                        (mainCam ?? (mainCam = Camera.main))?.GetComponent<CameraFollow>()?.Shake(0.3f, 0.4f);
                    }
                    if (animator != null) animator.SafeSetTrigger("Attack");
                }
                else if (CombatSystem.Instance != null)
                {
                    (mainCam ?? (mainCam = Camera.main))?.GetComponent<CameraFollow>()?.Shake(0.2f, 0.3f);
                }

                remainingMeters = 0;

                if (enemyStats.isDead)
                {
                    HandleDefeatedTarget(enemyStats);
                    return;
                }
            }

            StopMoving("Enemy Encountered");
            if (CombatSystem.Instance != null) CombatSystem.Instance.StartCombat(enemyStats);
            return;
        }

        currentTarget = null;
        SelectNextPOI();
        if (remainingMeters > 0.1f && currentTarget != null)
        {
            if (!spawnedCoinsForCurrentTarget) StartCoroutine(SpawnCoinsCoroutine());
            StartMoving();
        }
        else
        {
            StopMoving("Target Reached");
        }
    }

    private void HandleDefeatedTarget(CharacterStats enemyStats)
    {
        if (animator != null) animator.SafeSetTrigger("Victory");
        Animator enemyAnim = enemyStats.GetComponent<Animator>();
        bool isDragon = enemyStats.name.Contains("DragonBob");
        if (enemyAnim != null) { if (isDragon) enemyAnim.CrossFade("Die", 0.1f); else enemyAnim.SafeSetTrigger("Die"); }

        bool isChest = enemyStats.name.Contains("Chest") || enemyStats.name.Contains("TreasureChest");

        if (isChest) stats.AddGold(Random.Range(20, 51));
        else if (isDragon) stats.AddGold(Random.Range(100, 201));
        else stats.AddGold(Random.Range(5, 11));

        GameObject.Destroy(enemyStats.gameObject, isDragon ? 3f : 2f);
        StopMoving("Enemy Defeated");

        // Notify FTUE for NON-CHESTS here. Chests advance in ResumeAfterChest/Combat.
        if (!isChest && FTUEManager.Instance != null && FTUEManager.Instance.isFTUEActive)
            FTUEManager.Instance.OnStageCompleted(enemyStats.name);

        if (isChest && CombatSystem.Instance != null)
            CombatSystem.Instance.ShowChestUpgradePopup(enemyStats.name);

        currentTarget = null;
    }

    private void ResetPOIs()
    {
        availablePOIs.Clear();
        if (poiRoot == null) return;
        foreach (Transform child in poiRoot)
        {
            if (child.gameObject.activeInHierarchy) availablePOIs.Add(child);
        }
    }
}
