using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DragonBob : MonoBehaviour
{
    public enum BobState { Flying, Landing, Resting, TakingOff, InCombat }

    [Header("Flight Settings")]
    public float flyHeight = 8f; // Lowered for more presence and better shadows
    public float flySpeed = 8f; // Faster movement
    public float rotateSpeed = 3f;
    public float arrivalDistance = 3.0f;

    [Header("Behavior")]
    public int minRestTurns = 0; 
    public int maxRestTurns = 2;
    public float combatEngagementChance = 0.4f;
    public float flyOverPlayerChance = 0.6f; // High chance to target area near player

    private List<Transform> poiList = new List<Transform>();
    private Transform targetPOI;
    private int restTurnsRemaining = 0;
    private BobState currentState = BobState.Flying;

    private Animator animator;
    private CharacterStats stats;
    private HeroNavigation playerNav;
    private float stateTimer = 0f;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        stats = GetComponent<CharacterStats>();
        playerNav = Object.FindAnyObjectByType<HeroNavigation>();

        // Increase scale for BOSS presence
        transform.localScale = Vector3.one * 2.2f;

        // Find all POIs
        var pois = Object.FindObjectsByType<PointOfInterest>(FindObjectsInactive.Include);
        foreach (var poi in pois)
        {
            poiList.Add(poi.transform);
        }

        if (stats != null)
        {
            stats.brawn = 40;
            stats.grit = 30;
            stats.finesse = 15;
            stats.ResetStats();
        }

        DiceRollSystem.OnAnyDiceRolled += HandleDiceRoll;
        
        // Start high up
        transform.position = new Vector3(transform.position.x, flyHeight, transform.position.z);

        PickNewTargetPOI();
        SetState(BobState.Flying);
    }

    private void OnDestroy()
    {
        DiceRollSystem.OnAnyDiceRolled -= HandleDiceRoll;
    }

    void Update()
    {
        if (stats != null && stats.isDead) return;

        bool inCombat = CombatSystem.Instance != null && CombatSystem.Instance.isInCombat && CombatSystem.Instance.currentEnemyStats == stats;

        if (inCombat)
        {
            if (currentState != BobState.InCombat)
            {
                SetState(BobState.InCombat);
            }
            return;
        }
        else if (currentState == BobState.InCombat)
        {
            SetState(BobState.TakingOff);
            return;
        }

        stateTimer += Time.deltaTime;

        switch (currentState)
        {
            case BobState.Flying:
                FlyToPOI();
                break;
            case BobState.Landing:
                LandingLogic();
                break;
            case BobState.Resting:
                RestingLogic();
                break;
            case BobState.TakingOff:
                TakingOffLogic();
                break;
        }
    }

    private void SetState(BobState newState)
    {
        if (currentState == newState && newState != BobState.Resting) return;
        currentState = newState;
        stateTimer = 0f;

        if (animator == null) return;

        switch (newState)
        {
            case BobState.Flying:
                animator.CrossFade("Fly Forward", 0.7f);
                break;
            case BobState.Landing:
                animator.CrossFade("Land", 0.3f);
                break;
            case BobState.Resting:
                float r = Random.value;
                string idleAnim = r > 0.6f ? "Idle01" : (r > 0.3f ? "Idle02" : "Sleep");
                animator.CrossFade(idleAnim, 1.0f);
                break;
            case BobState.TakingOff:
                animator.CrossFade("Take Off", 0.2f);
                break;
            case BobState.InCombat:
                StartCoroutine(CombatTransition());
                break;
        }
    }

    private void FlyToPOI()
    {
        if (targetPOI == null) 
        {
            PickNewTargetPOI();
            return;
        }

        Vector3 targetPos = targetPOI.position + Vector3.up * flyHeight;
        Vector3 direction = (targetPos - transform.position).normalized;
        
        transform.position += direction * flySpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);
        }

        // Randomly glide or scream during flight for DEPTH
        if (stateTimer > 3f)
        {
            float r = Random.value;
            if (r < 0.01f) animator.CrossFade("Fly Glide", 1.5f);
            else if (r < 0.015f) animator.CrossFade("Scream", 0.5f);
        }

        if (Vector3.Distance(transform.position, targetPos) < arrivalDistance)
        {
            SetState(BobState.Landing);
        }
    }

    private void LandingLogic()
    {
        if (targetPOI == null) 
        {
            SetState(BobState.TakingOff);
            return;
        }

        Vector3 targetPos = targetPOI.position;
        targetPos.y += 0.5f; 

        transform.position = Vector3.MoveTowards(transform.position, targetPos, flySpeed * 0.6f * Time.deltaTime);
        
        if (Vector3.Distance(transform.position, targetPos) < 1.0f)
        {
            SetState(BobState.Resting);
            restTurnsRemaining = Random.Range(minRestTurns, maxRestTurns + 1);
            Debug.Log($"[DragonBob] Landed at {targetPOI.name}. Resting for {restTurnsRemaining} turns.");
            CheckForCombat();
        }
    }

    private void RestingLogic()
    {
        if (stateTimer > 12f)
        {
            stateTimer = 0f;
            float r = Random.value;
            if (r > 0.8f) animator.CrossFade("Scream", 0.5f);
            else
            {
                string idleAnim = r > 0.4f ? "Idle01" : "Idle02";
                animator.CrossFade(idleAnim, 2.0f);
            }
        }
    }

    private void TakingOffLogic()
    {
        Vector3 targetPos = transform.position + Vector3.up * flyHeight;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, flySpeed * 0.5f * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 1f)
        {
            PickNewTargetPOI();
            SetState(BobState.Flying);
        }
    }

    private void HandleDiceRoll(int rollTotal)
    {
        if (stats.isDead || currentState == BobState.InCombat) return;

        if (currentState == BobState.Resting)
        {
            restTurnsRemaining--;
            if (restTurnsRemaining <= 0)
            {
                SetState(BobState.TakingOff);
            }
            else
            {
                CheckForCombat();
            }
        }
    }

    private void CheckForCombat()
    {
        if (playerNav == null || targetPOI == null) return;

        float distToPlayer = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), 
                                            new Vector3(playerNav.transform.position.x, 0, playerNav.transform.position.z));
        
        if (distToPlayer < 12.0f) 
        {
            if (Random.value < combatEngagementChance)
            {
                Debug.Log("[DragonBob] Engaging Player!");
                CombatSystem.Instance.StartCombat(stats);
            }
        }
    }

    private void PickNewTargetPOI()
    {
        if (poiList.Count == 0) return;
        
        Transform oldTarget = targetPOI;

        // Strategy: 60% chance to pick a POI near the player to "fly over" them
        if (playerNav != null && Random.value < flyOverPlayerChance)
        {
            List<Transform> nearbyPOIs = new List<Transform>();
            foreach (var poi in poiList)
            {
                if (Vector3.Distance(poi.position, playerNav.transform.position) < 35f)
                {
                    nearbyPOIs.Add(poi);
                }
            }

            if (nearbyPOIs.Count > 0)
            {
                targetPOI = nearbyPOIs[Random.Range(0, nearbyPOIs.Count)];
                return;
            }
        }

        // Default random pick
        int safety = 0;
        while ((targetPOI == oldTarget || targetPOI == null) && poiList.Count > 1 && safety < 10)
        {
            targetPOI = poiList[Random.Range(0, poiList.Count)];
            safety++;
        }
    }

    private IEnumerator CombatTransition()
    {
        Vector3 combatPos = transform.position;
        combatPos.y = 1.5f; 
        
        float elapsed = 0;
        float duration = 1.0f;
        Vector3 start = transform.position;

        animator.CrossFade("Fly Float", 0.5f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, combatPos, elapsed / duration);
            
            if (playerNav != null)
            {
                Vector3 playerPos = playerNav.transform.position;
                Vector3 lookDir = (playerPos - transform.position).normalized;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
                }
            }
            yield return null;
        }
    }
}