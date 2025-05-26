using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using PathfindingSystem.Common;

namespace PathfindingSystem.AStar
{
    /// <summary>
    /// Manages path requests for A* pathfinding with optional multithreading
    /// </summary>
    public class PathRequestManager : MonoBehaviour
    {
        [System.Serializable]
        public struct PathRequest
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
        
        private Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
        private PathRequest currentPathRequest;
        private AStarPathfinder pathfinder;
        private bool isProcessingPath;
        
        [Header("Threading Settings")]
        public bool useMultithreading = true;
        public int maxPathsPerFrame = 10;
        
        private void Awake()
        {
            pathfinder = GetComponent<AStarPathfinder>();
            if (pathfinder == null)
            {
                pathfinder = GetComponent<AStarPathfinder>();
                if (pathfinder == null)
                {
                    Debug.LogError("PathRequestManager requires an AStarPathfinder component on the same GameObject");
                }
            }
        }
        
        /// <summary>
        /// Request a path calculation
        /// </summary>
        public void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Path> callback)
        {
            PathRequest newRequest = new PathRequest(pathStart, pathEnd, callback);
            
            pathRequestQueue.Enqueue(newRequest);
            TryProcessNext();
        }
        
        /// <summary>
        /// Try to process the next path request in the queue
        /// </summary>
        private void TryProcessNext()
        {
            if (!isProcessingPath && pathRequestQueue.Count > 0)
            {
                currentPathRequest = pathRequestQueue.Dequeue();
                isProcessingPath = true;
                
                if (useMultithreading)
                {
                    // Start a new thread for path calculation
                    ThreadStart threadStart = delegate { ProcessPath(); };
                    new Thread(threadStart).Start();
                }
                else
                {
                    // Use coroutine for single-threaded path calculation
                    StartCoroutine(ProcessPathCoroutine());
                }
            }
        }
        
        /// <summary>
        /// Process a path request in a separate thread
        /// </summary>
        private void ProcessPath()
        {
            // Calculate path
            pathfinder.FindPathImmediate(currentPathRequest.pathStart, currentPathRequest.pathEnd, OnPathFound);
        }
        
        /// <summary>
        /// Process a path request in the main thread using a coroutine
        /// </summary>
        private IEnumerator ProcessPathCoroutine()
        {
            // Calculate path
            pathfinder.FindPathImmediate(currentPathRequest.pathStart, currentPathRequest.pathEnd, OnPathFound);
            
            yield return null;
            
            isProcessingPath = false;
            TryProcessNext();
        }
        
        /// <summary>
        /// Called when a path is found
        /// </summary>
        private void OnPathFound(Path path)
        {
            // Execute callback on main thread
            lock (this)
            {
                // Queue the result to be processed in the main thread
                Loom.QueueOnMainThread(() => {
                    currentPathRequest.callback(path);
                    
                    isProcessingPath = false;
                    TryProcessNext();
                });
            }
        }
        
        /// <summary>
        /// Process multiple path requests per frame
        /// </summary>
        private void Update()
        {
            if (!useMultithreading && pathRequestQueue.Count > 0)
            {
                // Process a batch of paths in a single frame
                int pathsProcessed = 0;
                
                while (!isProcessingPath && pathRequestQueue.Count > 0 && pathsProcessed < maxPathsPerFrame)
                {
                    currentPathRequest = pathRequestQueue.Dequeue();
                    isProcessingPath = true;
                    
                    // Calculate path immediately
                    pathfinder.FindPathImmediate(currentPathRequest.pathStart, currentPathRequest.pathEnd, (Path path) => {
                        currentPathRequest.callback(path);
                        isProcessingPath = false;
                    });
                    
                    pathsProcessed++;
                }
            }
        }
    }
    
    /// <summary>
    /// Helper class to run actions on the main thread
    /// </summary>
    public class Loom : MonoBehaviour
    {
        private static Loom _instance;
        
        private static readonly object _lock = new object();
        private readonly List<Action> _actions = new List<Action>();
        
        /// <summary>
        /// Initialize the Loom instance
        /// </summary>
        public static void Initialize()
        {
            if (_instance == null)
            {
                GameObject loomGameObject = new GameObject("Loom");
                _instance = loomGameObject.AddComponent<Loom>();
                DontDestroyOnLoad(loomGameObject);
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Queue an action to be executed on the main thread
        /// </summary>
        public static void QueueOnMainThread(Action action)
        {
            if (_instance == null)
            {
                Initialize();
            }
            
            lock (_lock)
            {
                _instance._actions.Add(action);
            }
        }
        
        private void Update()
        {
            lock (_lock)
            {
                foreach (Action action in _actions)
                {
                    action();
                }
                
                _actions.Clear();
            }
        }
    }
}