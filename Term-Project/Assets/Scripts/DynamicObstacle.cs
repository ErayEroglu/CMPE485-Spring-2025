using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshObstacle))]
public class DynamicObstacle : MonoBehaviour
{
    [Header("Movement Configuration")]
    public ObstacleMovementType movementType = ObstacleMovementType.Static;
    public float moveSpeed = 2f;
    public float moveRadius = 10f;
    public float rotationSpeed = 30f;
    
    [Header("Dynamic Behavior")]
    public float changeInterval = 5f;
    public bool randomizeMovement = true;
    public bool affectsNavMesh = true;
    
    [Header("Size Variation")]
    public bool enableSizeVariation = false;
    public Vector3 minScale = Vector3.one * 3f;
    public Vector3 maxScale = Vector3.one * 6f;
    public float scaleChangeSpeed = 1f;
    
    [Header("Performance Settings")]
    public bool enableOptimizations = true;
    public float cullingDistance = 50f;
    
    private NavMeshObstacle obstacle;
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private float nextChangeTime;
    private bool isMoving = false;
    private Camera mainCamera;
    private bool isVisible = true;
    private Coroutine movementCoroutine;
    
    public enum ObstacleMovementType
    {
        Static,
        Linear,
        Circular,
        Random,
        Oscillating,
        Teleporting
    }
    
    private void Awake()
    {
        obstacle = GetComponent<NavMeshObstacle>();
        mainCamera = Camera.main;
        originalPosition = transform.position;
        targetPosition = originalPosition;
        
        // Configure obstacle for performance testing
        if (affectsNavMesh)
        {
            obstacle.carving = true;
            obstacle.carvingMoveThreshold = 0.1f;
            obstacle.carvingTimeToStationary = 0.5f;
        }
        
        // Add tag for identification
        if (!gameObject.CompareTag("Obstacle"))
        {
            gameObject.tag = "Obstacle";
        }
    }
    
    private void Start()
    {
        nextChangeTime = Time.time + changeInterval;
        
        if (movementType != ObstacleMovementType.Static)
        {
            movementCoroutine = StartCoroutine(MovementUpdateCoroutine());
        }
        
        if (enableSizeVariation)
        {
            StartCoroutine(SizeVariationCoroutine());
        }
    }
    
    private void Update()
    {
        if (enableOptimizations && mainCamera != null)
        {
            UpdateVisibility();
        }
        
        if (randomizeMovement && Time.time >= nextChangeTime)
        {
            RandomizeMovementType();
            nextChangeTime = Time.time + changeInterval;
        }
    }
    
    private void UpdateVisibility()
    {
        float distanceToCamera = Vector3.Distance(transform.position, mainCamera.transform.position);
        bool shouldBeVisible = distanceToCamera <= cullingDistance;
        
        if (shouldBeVisible != isVisible)
        {
            isVisible = shouldBeVisible;
            
            // Enable/disable obstacle based on visibility
            obstacle.enabled = isVisible;
            
            if (!isVisible && movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
            }
            else if (isVisible && movementCoroutine == null && movementType != ObstacleMovementType.Static)
            {
                movementCoroutine = StartCoroutine(MovementUpdateCoroutine());
            }
        }
    }
    
    private IEnumerator MovementUpdateCoroutine()
    {
        while (enabled && obstacle != null)
        {
            UpdateMovement();
            yield return new WaitForSeconds(0.1f); // Update 10 times per second
        }
    }
    
    private void UpdateMovement()
    {
        switch (movementType)
        {
            case ObstacleMovementType.Linear:
                UpdateLinearMovement();
                break;
            case ObstacleMovementType.Circular:
                UpdateCircularMovement();
                break;
            case ObstacleMovementType.Random:
                UpdateRandomMovement();
                break;
            case ObstacleMovementType.Oscillating:
                UpdateOscillatingMovement();
                break;
            case ObstacleMovementType.Teleporting:
                UpdateTeleportingMovement();
                break;
        }
    }
    
    private void UpdateLinearMovement()
    {
        if (!isMoving)
        {
            // Set new target position
            Vector3 direction = Random.insideUnitSphere;
            direction.y = 0; // Keep on ground
            targetPosition = originalPosition + direction.normalized * moveRadius;
            isMoving = true;
        }
        
        // Move towards target
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        
        // Check if reached target
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            isMoving = false;
        }
    }
    
    private void UpdateCircularMovement()
    {
        float angle = Time.time * moveSpeed * 10f; // Convert speed to angular velocity
        Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0, Mathf.Sin(angle * Mathf.Deg2Rad)) * moveRadius;
        transform.position = originalPosition + offset;
    }
    
    private void UpdateRandomMovement()
    {
        if (!isMoving || Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            // Generate new random target within radius
            Vector2 randomCircle = Random.insideUnitCircle * moveRadius;
            targetPosition = originalPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Ensure target is on NavMesh or valid ground
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, 5f, NavMesh.AllAreas))
            {
                targetPosition = hit.position;
            }
            
            isMoving = true;
        }
        
        // Move towards target
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }
    
    private void UpdateOscillatingMovement()
    {
        float oscillation = Mathf.Sin(Time.time * moveSpeed) * moveRadius;
        Vector3 direction = transform.right; // Oscillate along local X axis
        transform.position = originalPosition + direction * oscillation;
    }
    
    private void UpdateTeleportingMovement()
    {
        if (Time.time >= nextChangeTime - changeInterval + 1f) // Teleport 1 second before next change
        {
            // Teleport to random position within radius
            Vector2 randomCircle = Random.insideUnitCircle * moveRadius;
            Vector3 newPosition = originalPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Ensure position is valid
            NavMeshHit hit;
            if (NavMesh.SamplePosition(newPosition, out hit, 10f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }
            else
            {
                transform.position = newPosition;
            }
        }
    }
    
    private IEnumerator SizeVariationCoroutine()
    {
        while (enabled)
        {
            // Smoothly change scale
            Vector3 targetScale = Vector3.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * scaleChangeSpeed) + 1f) / 2f);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleChangeSpeed);
            
            // Update obstacle size
            if (obstacle != null)
            {
                obstacle.size = transform.localScale;
            }
            
            yield return null;
        }
    }
    
    private void RandomizeMovementType()
    {
        if (!randomizeMovement) return;
        
        // Randomly select new movement type (excluding Static)
        ObstacleMovementType[] types = { 
            ObstacleMovementType.Linear, 
            ObstacleMovementType.Circular, 
            ObstacleMovementType.Random, 
            ObstacleMovementType.Oscillating,
            ObstacleMovementType.Teleporting 
        };
        
        ObstacleMovementType newType = types[Random.Range(0, types.Length)];
        SetMovementType(newType);
    }
    
    // Public methods for runtime configuration
    public void SetMovementType(ObstacleMovementType type)
    {
        movementType = type;
        isMoving = false;
        
        // Restart movement coroutine if needed
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
        
        if (type != ObstacleMovementType.Static && isVisible)
        {
            movementCoroutine = StartCoroutine(MovementUpdateCoroutine());
        }
    }
    
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    public void SetMoveRadius(float radius)
    {
        moveRadius = radius;
    }
    
    public void SetChangeInterval(float interval)
    {
        changeInterval = interval;
        nextChangeTime = Time.time + interval;
    }
    
    public void EnableNavMeshCarving(bool enable)
    {
        affectsNavMesh = enable;
        if (obstacle != null)
        {
            obstacle.carving = enable;
        }
    }
    
    // Performance monitoring
    public bool IsCurrentlyMoving()
    {
        return movementType != ObstacleMovementType.Static && isMoving;
    }
    
    public float GetDistanceFromOriginal()
    {
        return Vector3.Distance(transform.position, originalPosition);
    }
    
    public Vector3 GetVelocity()
    {
        // Approximate velocity based on movement type
        switch (movementType)
        {
            case ObstacleMovementType.Circular:
                float angularVel = moveSpeed * 10f * Mathf.Deg2Rad;
                return new Vector3(-Mathf.Sin(Time.time * angularVel), 0, Mathf.Cos(Time.time * angularVel)) * moveRadius * angularVel;
            case ObstacleMovementType.Linear:
            case ObstacleMovementType.Random:
                return (targetPosition - transform.position).normalized * moveSpeed;
            default:
                return Vector3.zero;
        }
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
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(originalPosition, moveRadius);
        
        if (movementType != ObstacleMovementType.Static)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.5f);
        }
    }
} 