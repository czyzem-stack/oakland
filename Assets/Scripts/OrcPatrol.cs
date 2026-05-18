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

    private DragonBob bob;
    private float fearRadius = 15f;
    private bool isFleeing = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        characterStats = GetComponent<CharacterStats>();
        bob = Object.FindAnyObjectByType<DragonBob>();

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
                if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            }
            return; 
        }
        
        // Check for Bob
        if (bob != null && !isFleeing)
        {
            float distToBob = Vector3.Distance(transform.position, bob.transform.position);
            if (distToBob < fearRadius)
            {
                StartCoroutine(FleeFromBob());
            }
        }

        if (!isPatrolling && !isFleeing && (CombatSystem.Instance == null || !CombatSystem.Instance.isInCombat))
        {
            isPatrolling = true;
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = false;
        }

        if (animator != null && agent != null && agent.enabled && (isPatrolling || isFleeing))
        {
            float speed = agent.velocity.magnitude / agent.speed;
            animator.SafeSetFloat("Speed", speed);
        }
    }

    private IEnumerator FleeFromBob()
    {
        isFleeing = true;
        isPatrolling = false;
        agent.speed = 3.5f; // Run!
        
        Debug.Log($"[OrcPatrol] {name} is fleeing from Bob!");

        while (bob != null && Vector3.Distance(transform.position, bob.transform.position) < fearRadius + 5f)
        {
            Vector3 fleeDir = (transform.position - bob.transform.position).normalized;
            Vector3 fleeTarget = transform.position + fleeDir * 10f;

            if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            yield return new WaitForSeconds(0.5f);
        }

        agent.speed = 1.0f;
        isFleeing = false;
        isPatrolling = true;
    }

    private IEnumerator PatrolRoutine()
    {
        while (true)
        {
            if (isPatrolling && !isFleeing)
            {
                // Choose random point within radius
                Vector2 randomPoint = Random.insideUnitCircle * patrolRadius;
                Vector3 targetPos = startPosition + new Vector3(randomPoint.x, 0, randomPoint.y);

                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }

                // Wait until reached with safety timeout
                float timeout = 5f;
                while (timeout > 0)
                {
                    if (agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                        break;
                    
                    if (!agent.isOnNavMesh) break;

                    timeout -= Time.deltaTime;
                    yield return null;
                }
                
                // Idle pause
                yield return new WaitForSeconds(waitTime);
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
}
