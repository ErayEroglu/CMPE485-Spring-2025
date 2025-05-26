using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TestAgent : MonoBehaviour
{
    [Header("Movement Configuration")]
    public float updateRate = 0.1f;
    public MovementPattern movementPattern = MovementPattern.FollowTarget;
    public float wanderRadius = 20f;
    public float targetChangeInterval = 5f;
    
    [Header("Performance Settings")]
    public bool enableOptimizations = true;
    public float cullingDistance = 100f;
    public bool useObjectPooling = true;
    
    [Header("Target References")]
    public Transform primaryTarget;
    public Transform[] alternativeTargets;
    
    private NavMeshAgent agent;
    private Camera mainCamera;
    private Vector3 lastKnownTargetPosition;
    private float lastUpdateTime;
    private float nextTargetChangeTime;
    private int currentTargetIndex = 0;
    private bool isVisible = true;
    private Coroutine movementCoroutine;
    
    public enum MovementPattern
    {
        FollowTarget,
        Wander,
        Patrol,
        CircleTarget,
        RandomTargets
    }
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        mainCamera = Camera.main;
        
        // Optimize agent settings for performance testing
        if (enableOptimizations)
        {
            agent.acceleration = 8f;
            agent.angularSpeed = 120f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        }
    }
    
    private void Start()
    {
        // Find primary target if not assigned
        if (primaryTarget == null)
        {
            PlayerMovement player = FindObjectOfType<PlayerMovement>();
            if (player != null)
                primaryTarget = player.transform;
        }
        
        // Start movement coroutine
        movementCoroutine = StartCoroutine(MovementUpdateCoroutine());
        
        // Initialize target change timer
        nextTargetChangeTime = Time.time + targetChangeInterval;
    }
    
    private void Update()
    {
        // Performance optimization: Check visibility
        if (enableOptimizations && mainCamera != null)
        {
            UpdateVisibility();
        }
    }
    
    private void UpdateVisibility()
    {
        float distanceToCamera = Vector3.Distance(transform.position, mainCamera.transform.position);
        bool shouldBeVisible = distanceToCamera <= cullingDistance;
        
        if (shouldBeVisible != isVisible)
        {
            isVisible = shouldBeVisible;
            
            // Disable/enable components based on visibility
            agent.enabled = isVisible;
            
            if (!isVisible && movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
            }
            else if (isVisible && movementCoroutine == null)
            {
                movementCoroutine = StartCoroutine(MovementUpdateCoroutine());
            }
        }
    }
    
    private IEnumerator MovementUpdateCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(updateRate);
        
        while (enabled && agent != null && agent.enabled)
        {
            UpdateMovement();
            yield return wait;
        }
    }
    
    private void UpdateMovement()
    {
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return;
        
        switch (movementPattern)
        {
            case MovementPattern.FollowTarget:
                UpdateFollowTarget();
                break;
            case MovementPattern.Wander:
                UpdateWander();
                break;
            case MovementPattern.Patrol:
                UpdatePatrol();
                break;
            case MovementPattern.CircleTarget:
                UpdateCircleTarget();
                break;
            case MovementPattern.RandomTargets:
                UpdateRandomTargets();
                break;
        }
    }
    
    private void UpdateFollowTarget()
    {
        if (primaryTarget == null) return;
        
        Vector3 targetPosition = primaryTarget.position;
        
        // Optimization: Only update if target moved significantly
        if (Vector3.Distance(targetPosition, lastKnownTargetPosition) > 1f)
        {
            agent.SetDestination(targetPosition);
            lastKnownTargetPosition = targetPosition;
        }
    }
    
    private void UpdateWander()
    {
        // Check if agent reached destination or needs new target
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }
    
    private void UpdatePatrol()
    {
        if (alternativeTargets == null || alternativeTargets.Length == 0)
        {
            UpdateWander();
            return;
        }
        
        // Check if reached current target
        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            currentTargetIndex = (currentTargetIndex + 1) % alternativeTargets.Length;
            if (alternativeTargets[currentTargetIndex] != null)
            {
                agent.SetDestination(alternativeTargets[currentTargetIndex].position);
            }
        }
    }
    
    private void UpdateCircleTarget()
    {
        if (primaryTarget == null) return;
        
        // Create circular movement around target
        float angle = Time.time * 30f; // Degrees per second
        Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * 10f;
        Vector3 circlePosition = primaryTarget.position + offset;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(circlePosition, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
    
    private void UpdateRandomTargets()
    {
        if (Time.time >= nextTargetChangeTime)
        {
            if (alternativeTargets != null && alternativeTargets.Length > 0)
            {
                int randomIndex = Random.Range(0, alternativeTargets.Length);
                if (alternativeTargets[randomIndex] != null)
                {
                    agent.SetDestination(alternativeTargets[randomIndex].position);
                }
            }
            else
            {
                UpdateWander();
            }
            
            nextTargetChangeTime = Time.time + targetChangeInterval;
        }
    }
    
    // Public methods for runtime configuration
    public void SetUpdateRate(float newRate)
    {
        updateRate = newRate;
        
        // Restart coroutine with new rate
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = StartCoroutine(MovementUpdateCoroutine());
        }
    }
    
    public void SetMovementPattern(MovementPattern pattern)
    {
        movementPattern = pattern;
        nextTargetChangeTime = Time.time + targetChangeInterval;
    }
    
    public void SetPrimaryTarget(Transform target)
    {
        primaryTarget = target;
        lastKnownTargetPosition = Vector3.zero;
    }
    
    public void SetAlternativeTargets(Transform[] targets)
    {
        alternativeTargets = targets;
        currentTargetIndex = 0;
    }
    
    // Performance monitoring
    public bool IsMoving()
    {
        return agent != null && agent.velocity.magnitude > 0.1f;
    }
    
    public float GetDistanceToTarget()
    {
        if (primaryTarget == null) return float.MaxValue;
        return Vector3.Distance(transform.position, primaryTarget.position);
    }
    
    public bool HasPath()
    {
        return agent != null && agent.hasPath;
    }
    
    public bool IsPathPending()
    {
        return agent != null && agent.pathPending;
    }
    
    // Cleanup
    private void OnDestroy()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
    }
    
    private void OnDisable()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
    }
    
    private void OnEnable()
    {
        if (agent != null && agent.enabled && movementCoroutine == null)
        {
            movementCoroutine = StartCoroutine(MovementUpdateCoroutine());
        }
    }
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.yellow;
            Vector3[] path = agent.path.corners;
            for (int i = 0; i < path.Length - 1; i++)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
        }
        
        if (movementPattern == MovementPattern.Wander)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, wanderRadius);
        }
    }
} 