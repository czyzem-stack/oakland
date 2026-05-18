using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class OrcPatrol : MonoBehaviour
{
    public float patrolRadius = 2.0f; // Shorter walk
    public float waitTime = 3.0f; // Longer pause
    
    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 startPosition;
    private bool isPatrolling = true;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
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
        var stats = GetComponent<CharacterStats>();
        if (stats != null && stats.isDead)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            return;
        }

        if (animator != null && agent != null && agent.enabled)
{
            float speed = agent.velocity.magnitude / agent.speed;
            animator.SetFloat("Speed", speed);
        }

        // Engage/Disengage logic
        if (CombatSystem.Instance != null && CombatSystem.Instance.isInCombat && CombatSystem.Instance.currentEnemyStats == GetComponent<CharacterStats>())
        {
            if (isPatrolling)
            {
                isPatrolling = false;
                if (agent.isOnNavMesh) agent.isStopped = true;
                animator.SetFloat("Speed", 0f);
            }
        }
        else if (!isPatrolling && (CombatSystem.Instance == null || !CombatSystem.Instance.isInCombat))
        {
            isPatrolling = true;
            if (agent.isOnNavMesh) agent.isStopped = false;
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
                yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance);
                
                // Idle pause
                yield return new WaitForSeconds(waitTime);
            }
            yield return null;
        }
    }
}
