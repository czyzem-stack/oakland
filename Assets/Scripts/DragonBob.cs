using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class DragonBob : MonoBehaviour
{
    public enum BobState { Flying, Landing, Resting, TakingOff, InCombat }

    [Header("Flight Settings")]
    public float flyHeight = 12f; 
    public float flySpeed = 12f; 
    public float rotateSpeed = 3f;
public float arrivalDistance = 3.0f;

    [Header("Behavior")]
    public float combatEngagementChance = 0.10f; 
    public float flyOverPlayerChance = 0.8f; 
    public int gracePeriodRolls = 4;
    public bool isFTUECombat = false;

    private int rollsSinceStart = 0;
    private List<Transform> poiList = new List<Transform>();
    private Transform targetPOI;
    private BobState currentState = BobState.Flying;
private bool hasRoaredOverPlayer = false;

    private Animator animator;
    private CharacterStats stats;
    private HeroNavigation playerNav;
    private float stateTimer = 0f;
    private NavMeshObstacle obstacle;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        stats = GetComponent<CharacterStats>();
        playerNav = Object.FindAnyObjectByType<HeroNavigation>();
        transform.localScale = Vector3.one * 2.5f;

        // Ensure Bob has a NavMeshObstacle to make Steve walk around him when landed
        obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null) obstacle = gameObject.AddComponent<NavMeshObstacle>();
        obstacle.carving = true;
        obstacle.shape = NavMeshObstacleShape.Capsule;
        obstacle.center = Vector3.zero;
        obstacle.radius = 1.5f; // Reduced from 2.5 to prevent blocking narrow paths
        obstacle.height = 4.0f;
        obstacle.enabled = false;

        foreach (var col in GetComponentsInChildren<Collider>()) { col.isTrigger = true; }

        var pois = Object.FindObjectsByType<PointOfInterest>(FindObjectsInactive.Include);
        foreach (var poi in pois) { poiList.Add(poi.transform); }

        if (stats != null) { stats.brawn = 40; stats.grit = 30; stats.finesse = 15; stats.ResetStats(); }
        DiceRollSystem.OnAnyDiceRolled += HandleDiceRoll;
        
        // FTUE: Position Bob to cast a visible shadow in front of Steve immediately
        PositionForInitialShadow();

        PickNewTargetPOI();
        SetState(BobState.Flying);
    }

    private void PositionForInitialShadow()
    {
        if (playerNav == null) { transform.position = new Vector3(-100, flyHeight + 30, -100); return; }

        // Find the main directional light
        Light mainLight = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude)) {
            if (l.type == LightType.Directional && l.shadows != LightShadows.None) { mainLight = l; break; }
        }

        float targetBobHeight = flyHeight + 20f;
        Vector3 playerPos = playerNav.transform.position;

        if (mainLight != null) {
            Vector3 lightDir = mainLight.transform.forward;
            if (lightDir.y < -0.1f) {
                // Place Bob so his shadow is 5m in front of Steve's forward direction
                Vector3 shadowPos = playerPos + playerNav.transform.forward * 5f;
                float scalar = targetBobHeight / -lightDir.y;
                transform.position = shadowPos - lightDir * scalar;
                Debug.Log($"[DragonBob] FTUE Shadow-casting start: {transform.position}");
                return;
            }
        }
        transform.position = playerPos + Vector3.back * 40f + Vector3.up * targetBobHeight;
    }

    private void OnDestroy() { DiceRollSystem.OnAnyDiceRolled -= HandleDiceRoll; }

    void Update()
    {
        if (stats != null && stats.isDead) return;
        bool inCombat = CombatSystem.Instance != null && CombatSystem.Instance.isInCombat && CombatSystem.Instance.currentEnemyStats == stats;
        if (inCombat) { if (currentState != BobState.InCombat) SetState(BobState.InCombat); return; }
        else if (currentState == BobState.InCombat) { SetState(BobState.Flying); return; }

        if (Time.frameCount % 300 == 0) Debug.Log($"[DragonBob] Current State: {currentState}. Pos: {transform.position}");

        stateTimer += Time.deltaTime;
        
        // Force Flying state if we somehow dropped out of it
        if (currentState != BobState.Flying && currentState != BobState.InCombat)
        {
            SetState(BobState.Flying);
        }

        FlyToPOI();
    }

    private void SetState(BobState newState)
    {
        if (currentState == newState && newState != BobState.Resting) return;
        
        // Prevent landing/resting states
        if (newState == BobState.Landing || newState == BobState.Resting || newState == BobState.TakingOff)
        {
            newState = BobState.Flying;
        }

        Debug.Log($"[DragonBob] STATE CHANGE: {currentState} -> {newState}");
        currentState = newState;
        stateTimer = 0f;

        if (obstacle != null) {
            obstacle.enabled = false;
        }

        if (animator == null) return;
        switch (newState)
        {
            case BobState.Flying: 
                Debug.Log("[DragonBob] Playing: Fly Forward");
                animator.CrossFade("Fly Forward", 0.7f); 
                hasRoaredOverPlayer = false; 
                break;
            case BobState.InCombat: 
                StartCoroutine(CombatTransition()); 
                break;
        }
    }

    private void FlyToPOI()
    {
        if (targetPOI == null) { PickNewTargetPOI(); return; }
        Vector3 targetPos = targetPOI.position + Vector3.up * flyHeight;

        // Smooth movement to target
        Vector3 nextPos = Vector3.MoveTowards(transform.position, targetPos, flySpeed * Time.deltaTime);
        
        if (nextPos == transform.position && flySpeed > 0)
        {
            PickNewTargetPOI();
            return;
        }
        
        transform.position = nextPos;

        // Smooth rotation to direction
        Vector3 direction = (targetPos - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);
        }

        if (playerNav != null && !hasRoaredOverPlayer) {
            float horizontalDist = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(playerNav.transform.position.x, playerNav.transform.position.z));
            if (horizontalDist < 12f) { animator.CrossFade("Scream", 0.4f); hasRoaredOverPlayer = true; }
        }
        
        if (Vector3.Distance(transform.position, targetPos) < arrivalDistance) PickNewTargetPOI();
    }

    private void LandingLogic() { }
    private void RestingLogic() { }
    private void TakingOffLogic() { }

    private void HandleDiceRoll(int rollTotal)
    {
        if (stats.isDead || currentState == BobState.InCombat) return;
        rollsSinceStart++;
        
        // Bob just flies around more actively on rolls
        flyHeight = Random.Range(10f, 15f);
        PickFlyOverTarget();
        SetState(BobState.Flying);
    }

    private void PickFlyOverTarget()
    {
        if (playerNav == null || poiList.Count == 0) return;
        Vector3 farPoint = playerNav.transform.position + (playerNav.transform.position - transform.position).normalized * 60f;
        targetPOI = poiList[0]; float minDist = float.MaxValue;
        foreach(var poi in poiList) { float d = Vector3.Distance(poi.position, farPoint); if (d < minDist) { minDist = d; targetPOI = poi; } }
    }

    private void PickNearbyLandingSpot()
    {
        if (playerNav == null || poiList.Count == 0) return;
        targetPOI = poiList[0]; float minDist = float.MaxValue;
        foreach(var poi in poiList) { float d = Vector3.Distance(poi.position, playerNav.transform.position); if (d < minDist) { minDist = d; targetPOI = poi; } }
    }

    private void CheckForCombat()
    {
        if (playerNav == null || targetPOI == null) return;
        float distToPlayer = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(playerNav.transform.position.x, playerNav.transform.position.z));
        if (distToPlayer < 12.0f) 
        {
            if (rollsSinceStart == 3 && currentState == BobState.Resting) { isFTUECombat = true; CombatSystem.Instance.StartCombat(stats); return; }
            if (rollsSinceStart <= gracePeriodRolls) { if (Random.value < 0.6f) animator.CrossFade("Scream", 0.5f); return; }
            if (Random.value < combatEngagementChance) CombatSystem.Instance.StartCombat(stats);
            else if (Random.value < 0.7f) animator.CrossFade("Scream", 0.5f);
        }
    }

    public void FTUEFlyAway() { isFTUECombat = false; flyHeight = 12f; SetState(BobState.TakingOff); }

    private void PickNewTargetPOI()
    {
        if (poiList.Count == 0) return;
        Transform oldTarget = targetPOI;
        if (playerNav != null && Random.value < flyOverPlayerChance) {
            List<Transform> nearbyPOIs = new List<Transform>();
            foreach (var poi in poiList) { float dist = Vector3.Distance(poi.position, playerNav.transform.position); if (dist > 5f && dist < 35f) nearbyPOIs.Add(poi); }
            if (nearbyPOIs.Count > 0) { targetPOI = nearbyPOIs[Random.Range(0, nearbyPOIs.Count)]; return; }
        }
        int safety = 0;
        while ((targetPOI == oldTarget || targetPOI == null) && poiList.Count > 1 && safety < 10) { targetPOI = poiList[Random.Range(0, poiList.Count)]; safety++; }
    }

    private IEnumerator CombatTransition()
    {
        Vector3 combatPos = transform.position; combatPos.y = 1.5f; 
        float elapsed = 0; float duration = 1.0f; Vector3 start = transform.position;
        animator.CrossFade("Fly Float", 0.5f);
        while (elapsed < duration) {
            elapsed += Time.deltaTime; transform.position = Vector3.Lerp(start, combatPos, elapsed / duration);
            if (playerNav != null) { Vector3 lookDir = (playerNav.transform.position - transform.position).normalized; lookDir.y = 0; if (lookDir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f); }
            yield return null;
        }
    }
}