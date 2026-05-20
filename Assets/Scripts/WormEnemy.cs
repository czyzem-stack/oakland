using UnityEngine;
using System.Collections;

public class WormEnemy : MonoBehaviour
{
    public float detectionRadius = 8f;
    private Transform playerTransform;
    private Animator animator;
    private CharacterStats stats;
    private bool isSurfaced = false;
    private bool isTransitioning = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        stats = GetComponent<CharacterStats>();
        FindPlayer();

        // Start buried
        if (animator != null)
        {
            // We force them into a buried state or just hide them
            // The GroundDiveIn animation usually ends with them in the ground
            // For POI spawns, we might want them to start already under.
            // But let's just make them surface when close.
        }
    }

    private void FindPlayer()
    {
        var stats = PlayerReference.GetStats();
        if (stats != null) playerTransform = stats.transform;
    }

    void Update()
    {
        if (playerTransform == null)
        {
            if (Time.frameCount % 60 == 0) FindPlayer();
            return;
        }

        if (stats != null && stats.isDead) return;

        // Throttle checks
        if (Time.frameCount % 6 != 0) return;

        // During FTUE, only allow engagement if this is a forced FTUE object
        if (FTUEManager.Instance != null && FTUEManager.Instance.isFTUEActive)
        {
            PointOfInterest parentPOI = GetComponentInParent<PointOfInterest>();
            if (parentPOI == null || (!parentPOI.name.Contains("Forced") && !parentPOI.name.Contains("FTUE")))
                return;
        }

        // If global combat is active and we are the target, the combat system handles animations.
bool inCombat = CombatSystem.Instance != null && CombatSystem.Instance.isInCombat;
if (inCombat && CombatSystem.Instance.currentEnemyStats == stats)
        {
            isSurfaced = true; // Assume surfaced if in combat
            return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (!isSurfaced && !isTransitioning && dist < detectionRadius)
        {
            StartCoroutine(SurfaceRoutine());
        }

        // If surfaced and player is very close, initiate combat if not already in combat
        if (isSurfaced && !inCombat && dist < detectionRadius * 0.5f)
        {
            if (GenericPopup.IsOpen || EquipmentLootPopup.IsOpen) return;
            if (playerTransform != null)
            {
                CharacterStats pStats = playerTransform.GetComponent<CharacterStats>();
                if (pStats != null && pStats.isDead) return;
            }
            CombatSystem.Instance.StartCombat(stats);
        }
}

    private IEnumerator SurfaceRoutine()
    {
        isTransitioning = true;
        if (animator != null)
        {
            Debug.Log($"[WormEnemy] {name} surfacing!");
            animator.CrossFade("GroundBreakThrough", 0.1f);
            yield return new WaitForSeconds(1.0f);
            animator.CrossFade("IdleBattle", 0.2f);
        }
        isSurfaced = true;
        isTransitioning = false;
        
        // After surfacing, if Steve is still close, maybe it triggers combat via POI proximity?
        // PointOfInterest.cs has CheckProximityEngagement which handles the combat start.
    }
}
