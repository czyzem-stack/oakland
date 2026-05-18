using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class OrcPatrol : MonoBehaviour
{
    public float patrolRadius = 2.0f; // Shorter walk
    public float waitTime = 3.0f; // Longer pause
    
    private NavMeshAgent agent;
    private Animator animator;
    private CharacterStats characterStats;
    private Vector3 startPosition;
    private bool isPatrolling = true;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        characterStats = GetComponent<CharacterStats>();
if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }
        
        // Settings for 'buttery' movement
        agent.speed = 1.0f; // Slow walk
        agent.stoppingDistance = 0.3f;
        agent.acceleration = 2f;
        agent.angularSpeed = 180f;

        animator = GetComponent<Animator>();
        startPosition = transform.position;
        
        // Stagger startup to prevent frame drops
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        // Wait between 0.1 and 1.5 seconds
        yield return new WaitForSeconds(Random.Range(0.1f, 1.5f));
        StartCoroutine(PatrolRoutine());
    }

    void Update()
    {
        if (characterStats != null && characterStats.isDead)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            return;
        }

        // Engage/Disengage logic
        bool isInActiveCombat = CombatSystem.Instance != null && CombatSystem.Instance.isInCombat && CombatSystem.Instance.currentEnemyStats == characterStats;

        if (isInActiveCombat)
        {
            if (isPatrolling)
            {
                isPatrolling = false;
                if (agent.isOnNavMesh) agent.isStopped = true;
                // Don't set Speed to 0 here, let CombatSystem handle it
            }
            return; // Exit Update - let CombatSystem control the orc
        }
        else if (!isPatrolling && (CombatSystem.Instance == null || !CombatSystem.Instance.isInCombat))
        {
            isPatrolling = true;
            if (agent.isOnNavMesh) agent.isStopped = false;
        }

        if (animator != null && agent != null && agent.enabled && isPatrolling)
        {
            float speed = agent.velocity.magnitude / agent.speed;
            animator.SafeSetFloat("Speed", speed);
        }
}

    private IEnumerator PatrolRoutine()
    {
        while (true)
        {
            if (isPatrolling)
            {
                // Choose random point within radius
                Vector2 randomPoint = Random.insideUnitCircle * patrolRadius;
                Vector3 targetPos = startPosition + new Vector3(randomPoint.x, 0, randomPoint.y);

                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }

                // Wait until reached
                yield return new WaitUntil(() => agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance);
                
                // Idle pause
                yield return new WaitForSeconds(waitTime);
            }
            yield return null;
        }
    }
}
