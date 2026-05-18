using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class HeroNavigation : MonoBehaviour
{
    [Header("Navigation Settings")]
    public Transform poiRoot;
    public float metersPerDicePoint = 2.0f;
    public float arrivalDistance = 1.0f;

    private NavMeshAgent agent;
    private Animator animator;
    private List<Transform> availablePOIs = new List<Transform>();
    private Transform currentTarget;
    
    [Header("Status")]
    public float remainingMeters = 0f;
    public bool isMoving = false;
    private Vector3 lastPosition;

    private void EnsureComponents()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();
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
        EnsureComponents();
        if (agent == null) return;

        if (isMoving && remainingMeters > 0)
        {
            // Calculate distance moved this frame
            float distMoved = Vector3.Distance(transform.position, lastPosition);
            remainingMeters -= distMoved;
            lastPosition = transform.position;

            // Sync animation
            float speed = agent.velocity.magnitude / agent.speed;
            if (animator != null) animator.SetFloat("Speed", speed);

            // Check if arrived at POI
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                OnReachedPOI();
            }
            // Check if out of fuel
            else if (remainingMeters <= 0)
            {
                StopMoving("Out of distance");
            }
        }
        else
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
            if (isMoving) StopMoving("Idle/Stop");
            lastPosition = transform.position; 
        }
    }

    public void OnDiceRolled(int totalValue)
    {
        EnsureComponents();
        // Add to the pool
        remainingMeters += (totalValue * metersPerDicePoint);
        Debug.Log($"[HeroNavigation] Gained {totalValue * metersPerDicePoint}m. Total pool: {remainingMeters}m.");
        
        if (currentTarget == null)
        {
            SelectNextPOI();
        }

        if (currentTarget != null && agent != null)
        {
            StartMoving();
        }
    }

    private void SelectNextPOI()
    {
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

    private void OnReachedPOI()
    {
        Debug.Log("Reached POI target location!");
        currentTarget = null; // Clear so next roll picks a new one
        StopMoving("Target Reached");
        SelectNextPOI(); // Pre-select for next roll
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

