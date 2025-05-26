using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathfindingSystem.Common
{
    /// <summary>
    /// Agent that follows paths created by pathfinding algorithms
    /// </summary>
    public class Agent : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float turnSpeed = 3f;
        public float arrivalDistance = 0.1f;
        public float pathUpdateRate = 0.5f; // How often to recalculate path
        
        [Header("Debug Settings")]
        public bool showPath = true;
        public Color pathColor = Color.green;
        
        // References
        private IPathfinder pathfinder;
        private Transform target;
        
        // Path following
        private Vector3[] path;
        private int currentWaypointIndex;
        private float lastPathRequestTime;
        private bool isFollowingPath = false;
        
        // Cached components
        private Rigidbody rb;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.freezeRotation = true;
            }
        }
        
        /// <summary>
        /// Set the pathfinding system to use
        /// </summary>
        public void SetPathfinder(IPathfinder pathfinder)
        {
            this.pathfinder = pathfinder;
        }
        
        /// <summary>
        /// Set the target to move towards
        /// </summary>
        public void SetTarget(Transform target)
        {
            this.target = target;
            RequestPath();
        }
        
        /// <summary>
        /// Set the target position to move towards
        /// </summary>
        public void SetTargetPosition(Vector3 position)
        {
            GameObject tempTarget = new GameObject("TempTarget");
            tempTarget.transform.position = position;
            this.target = tempTarget.transform;
            RequestPath();
        }
        
        private void Update()
        {
            if (target == null || pathfinder == null)
                return;
            
            // Request a new path periodically
            if (Time.time > lastPathRequestTime + pathUpdateRate)
            {
                RequestPath();
            }
            
            if (path != null && path.Length > 0)
            {
                FollowPath();
            }
        }
        
        private void RequestPath()
        {
            if (pathfinder != null && target != null)
            {
                lastPathRequestTime = Time.time;
                pathfinder.FindPath(transform.position, target.position, OnPathFound);
            }
        }
        
        private void OnPathFound(Path newPath)
        {
            if (newPath.success && newPath.waypoints.Length > 0)
            {
                path = newPath.waypoints;
                currentWaypointIndex = 0;
                isFollowingPath = true;
            }
            else
            {
                path = null;
                isFollowingPath = false;
            }
        }
        
        private void FollowPath()
        {
            if (currentWaypointIndex >= path.Length)
            {
                isFollowingPath = false;
                return;
            }
            
            Vector3 currentWaypoint = path[currentWaypointIndex];
            
            // Move towards current waypoint
            Vector3 direction = (currentWaypoint - transform.position).normalized;
            Vector3 targetVelocity = direction * moveSpeed;
            
            // If we're close to a waypoint, move to the next one
            if (Vector3.Distance(transform.position, currentWaypoint) < arrivalDistance)
            {
                currentWaypointIndex++;
                
                if (currentWaypointIndex >= path.Length)
                {
                    isFollowingPath = false;
                    return;
                }
            }
            
            // Apply movement
            MoveAgent(targetVelocity);
            
            // Rotate towards movement direction
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }
        
        private void MoveAgent(Vector3 velocity)
        {
            if (rb != null)
            {
                rb.velocity = new Vector3(velocity.x, rb.velocity.y, velocity.z);
            }
            else
            {
                transform.position += velocity * Time.deltaTime;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (showPath && path != null && path.Length > 0)
            {
                Gizmos.color = pathColor;
                
                for (int i = currentWaypointIndex; i < path.Length - 1; i++)
                {
                    Gizmos.DrawLine(path[i], path[i + 1]);
                    Gizmos.DrawSphere(path[i], 0.1f);
                }
                
                Gizmos.DrawSphere(path[path.Length - 1], 0.1f);
            }
        }
    }
} 