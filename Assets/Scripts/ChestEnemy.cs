using UnityEngine;

public class ChestEnemy : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 25f;
    public float rotationSpeed = 8f;
    
    private Transform playerTransform;
    private Animator animator;
    private CharacterStats stats;
    private bool hasNoticedPlayer = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        stats = GetComponent<CharacterStats>();
        FindPlayer();
    }

    private void FindPlayer()
    {
        var pStats = PlayerReference.GetStats();
        if (pStats != null) playerTransform = pStats.transform;
    }

    void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        if (stats != null && stats.isDead) return;
        
        // Stop tracking if player is dead
        CharacterStats pStats = playerTransform.GetComponent<CharacterStats>();
        if (pStats != null && pStats.isDead) return;

        // Throttled distance check
        if (Time.frameCount % 5 != 0) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        
        // Rotation logic: always face Steve if he is close enough, even if in global combat
        if (dist < detectionRadius)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            direction.y = 0;
            
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed * 5f); // Compensate for throttle
            }

            // Animation logic: only trigger if the global combat isn't active 
            // to avoid state conflicts during combat sequences
            bool globalCombatActive = CombatSystem.Instance != null && CombatSystem.Instance.isInCombat;
            if (!globalCombatActive)
            {
                if (!hasNoticedPlayer)
                {
                    hasNoticedPlayer = true;
                    if (animator != null) animator.CrossFade("SenseSomethingST", 0.2f);
                }
                else
                {
                    if (animator != null && dist < detectionRadius * 0.8f)
                    {
                        var state = animator.GetCurrentAnimatorStateInfo(0);
                        if (!state.IsName("SenseSomethingST") && !state.IsName("IdleBattle") && !animator.IsInTransition(0))
                        {
                            animator.CrossFade("IdleBattle", 0.5f);
                        }
                    }
                }
            }
        }
        else
        {
            if (hasNoticedPlayer)
            {
                hasNoticedPlayer = false;
                if (animator != null && !animator.IsInTransition(0))
                {
                    animator.CrossFade("IdleChest", 0.5f);
                }
            }
        }
    }
}
