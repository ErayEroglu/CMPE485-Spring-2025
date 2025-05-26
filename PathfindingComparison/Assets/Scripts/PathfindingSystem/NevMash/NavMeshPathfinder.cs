using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using PathfindingSystem.Common;

namespace PathfindingSystem.NavMesh
{
    /// <summary>
    /// Unity NavMesh pathfinding implementation
    /// </summary>
    public class NavMeshPathfinder : MonoBehaviour, IPathfinder
    {
        [Header("References")]
        public NavMeshSurface navMeshSurface;
        
        [Header("Performance Settings")]
        public int maxPathsPerFrame = 10;
        public float rebakeInterval = 0.5f;
        
        private Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
        private bool isProcessingPath = false;
        private float lastBakeTime;
        
        private struct PathRequest
        {
            public Vector3 pathStart;
            public Vector3 pathEnd;
            public Action<Path> callback;
            
            public PathRequest(Vector3 start, Vector3 end, Action<Path> callback)
            {
                this.pathStart = start;
                this.pathEnd = end;
                this.callback = callback;
            }
        }
        
        private void Awake()
        {
            // Auto-create NavMeshSurface if not assigned
            if (navMeshSurface == null)
            {
                navMeshSurface = GetComponent<NavMeshSurface>();
                if (navMeshSurface == null)
                {
                    navMeshSurface = FindObjectOfType<NavMeshSurface>();
                    if (navMeshSurface == null)
                    {
                        Debug.LogError("NavMeshPathfinder requires a NavMeshSurface component in the scene");
                    }
                }
            }
        }
        
        public void Initialize()
        {
            // Make sure NavMeshSurface is assigned
            if (navMeshSurface == null)
            {
                navMeshSurface = GetComponent<NavMeshSurface>();
                if (navMeshSurface == null)
                {
                    navMeshSurface = FindObjectOfType<NavMeshSurface>();
                    if (navMeshSurface == null)
                    {
                        Debug.LogError("NavMeshPathfinder requires a NavMeshSurface component in the scene");
                        return;
                    }
                }
            }
            
            // Build the initial NavMesh
            navMeshSurface.BuildNavMesh();
            lastBakeTime = Time.time;
        }
        
        public string GetAlgorithmName()
        {
            return "NavMesh";
        }
        
        public void FindPath(Vector3 startPos, Vector3 targetPos, Action<Path> callback)
        {
            PathRequest newRequest = new PathRequest(startPos, targetPos, callback);
            pathRequestQueue.Enqueue(newRequest);
            
            if (!isProcessingPath)
            {
                ProcessNextPathRequest();
            }
        }
        
        public void HandleDynamicObstacle(Vector3 obstaclePosition, float radius)
        {
            // Check if enough time has passed since the last bake
            if (Time.time > lastBakeTime + rebakeInterval)
            {
                // Rebake the NavMesh to account for the changes
                navMeshSurface.BuildNavMesh();
                lastBakeTime = Time.time;
            }
        }
        
        private void ProcessNextPathRequest()
        {
            if (pathRequestQueue.Count == 0)
            {
                isProcessingPath = false;
                return;
            }
            
            isProcessingPath = true;
            PathRequest request = pathRequestQueue.Dequeue();
            StartCoroutine(FindPathCoroutine(request.pathStart, request.pathEnd, request.callback));
        }
        
        private IEnumerator FindPathCoroutine(Vector3 startPos, Vector3 targetPos, Action<Path> callback)
        {
            NavMeshPath navMeshPath = new NavMeshPath();
            bool pathSuccess = UnityEngine.AI.NavMesh.CalculatePath(startPos, targetPos, UnityEngine.AI.NavMesh.AllAreas, navMeshPath);
            
            // Convert Unity's NavMesh path to our own Path format
            Vector3[] waypoints = new Vector3[0];
            if (pathSuccess)
            {
                waypoints = SimplifyPath(navMeshPath.corners);
            }
            
            callback(new Path(waypoints, pathSuccess));
            
            isProcessingPath = false;
            ProcessNextPathRequest();
            
            yield return null;
        }
        
        private Vector3[] SimplifyPath(Vector3[] path)
        {
            // NavMesh paths are already simplified, so we just return them as is
            return path;
        }
        
        private void Update()
        {
            // Process a batch of paths each frame
            int pathsProcessed = 0;
            
            while (pathRequestQueue.Count > 0 && pathsProcessed < maxPathsPerFrame && !isProcessingPath)
            {
                ProcessNextPathRequest();
                pathsProcessed++;
            }
        }
    }
} 