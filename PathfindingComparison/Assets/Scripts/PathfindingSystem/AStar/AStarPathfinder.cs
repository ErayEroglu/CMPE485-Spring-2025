using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathfindingSystem.Common;

namespace PathfindingSystem.AStar
{
    /// <summary>
    /// A* Pathfinding algorithm implementation
    /// </summary>
    public class AStarPathfinder : MonoBehaviour, IPathfinder
    {
        [Header("References")]
        public Grid grid;
        
        [Header("Performance Settings")]
        public bool useMultithreading = true;
        public int maxPathsPerFrame = 10;
        
        private PathRequestManager requestManager;
        
        private void Awake()
        {
            // Auto-create Grid if not assigned
            if (grid == null)
            {
                grid = GetComponent<Grid>();
                if (grid == null)
                {
                    grid = gameObject.AddComponent<Grid>();
                    Debug.Log("Grid component auto-created on AStarPathfinder GameObject");
                }
            }
            
            // Auto-create PathRequestManager if not found
            requestManager = GetComponent<PathRequestManager>();
            if (requestManager == null)
            {
                requestManager = gameObject.AddComponent<PathRequestManager>();
                Debug.Log("PathRequestManager component auto-created on AStarPathfinder GameObject");
            }
        }
        
        public void Initialize()
        {
            // Make sure Grid is created
            if (grid == null)
            {
                grid = GetComponent<Grid>();
                if (grid == null)
                {
                    grid = gameObject.AddComponent<Grid>();
                    Debug.Log("Grid component auto-created on AStarPathfinder GameObject during initialization");
                }
            }
        }
        
        public string GetAlgorithmName()
        {
            return "A*";
        }
        
        public void FindPath(Vector3 startPos, Vector3 targetPos, Action<Path> callback)
        {
            if (useMultithreading)
            {
                requestManager.RequestPath(startPos, targetPos, callback);
            }
            else
            {
                StartCoroutine(FindPathRoutine(startPos, targetPos, callback));
            }
        }
        
        public void HandleDynamicObstacle(Vector3 obstaclePosition, float radius)
        {
            grid.UpdateGridArea(obstaclePosition, radius);
        }
        
        /// <summary>
        /// Find path coroutine (non-threaded version)
        /// </summary>
        private IEnumerator FindPathRoutine(Vector3 startPos, Vector3 targetPos, Action<Path> callback)
        {
            Vector3[] waypoints = new Vector3[0];
            bool pathSuccess = false;
            
            Node startNode = grid.NodeFromWorldPoint(startPos);
            Node targetNode = grid.NodeFromWorldPoint(targetPos);
            
            if (startNode.walkable && targetNode.walkable)
            {
                List<Node> openSet = new List<Node>();
                HashSet<Node> closedSet = new HashSet<Node>();
                openSet.Add(startNode);
                
                while (openSet.Count > 0)
                {
                    Node currentNode = openSet[0];
                    for (int i = 1; i < openSet.Count; i++)
                    {
                        // Find node with lowest F cost, or lowest H cost if F costs are equal
                        if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                        {
                            currentNode = openSet[i];
                        }
                    }
                    
                    openSet.Remove(currentNode);
                    closedSet.Add(currentNode);
                    
                    // Path found
                    if (currentNode == targetNode)
                    {
                        pathSuccess = true;
                        break;
                    }
                    
                    // Check all neighbors
                    foreach (Node neighbor in grid.GetNeighbors(currentNode))
                    {
                        if (!neighbor.walkable || closedSet.Contains(neighbor))
                            continue;
                        
                        int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                        
                        // This path to neighbor is better than any previous path
                        if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                        {
                            neighbor.gCost = newMovementCostToNeighbor;
                            neighbor.hCost = GetDistance(neighbor, targetNode);
                            neighbor.parent = currentNode;
                            
                            if (!openSet.Contains(neighbor))
                                openSet.Add(neighbor);
                        }
                    }
                }
            }
            
            // Return the result
            if (pathSuccess)
            {
                waypoints = RetracePath(startNode, targetNode);
            }
            
            callback(new Path(waypoints, pathSuccess));
            yield return null;
        }
        
        /// <summary>
        /// Core A* algorithm implementation (for multithreaded use)
        /// </summary>
        public void FindPathImmediate(Vector3 startPos, Vector3 targetPos, Action<Path> callback)
        {
            Vector3[] waypoints = new Vector3[0];
            bool pathSuccess = false;
            
            Node startNode = grid.NodeFromWorldPoint(startPos);
            Node targetNode = grid.NodeFromWorldPoint(targetPos);
            
            if (startNode.walkable && targetNode.walkable)
            {
                List<Node> openSet = new List<Node>();
                HashSet<Node> closedSet = new HashSet<Node>();
                openSet.Add(startNode);
                
                while (openSet.Count > 0)
                {
                    Node currentNode = openSet[0];
                    for (int i = 1; i < openSet.Count; i++)
                    {
                        // Find node with lowest F cost, or lowest H cost if F costs are equal
                        if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                        {
                            currentNode = openSet[i];
                        }
                    }
                    
                    openSet.Remove(currentNode);
                    closedSet.Add(currentNode);
                    
                    // Path found
                    if (currentNode == targetNode)
                    {
                        pathSuccess = true;
                        break;
                    }
                    
                    // Check all neighbors
                    foreach (Node neighbor in grid.GetNeighbors(currentNode))
                    {
                        if (!neighbor.walkable || closedSet.Contains(neighbor))
                            continue;
                        
                        int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                        
                        // This path to neighbor is better than any previous path
                        if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                        {
                            neighbor.gCost = newMovementCostToNeighbor;
                            neighbor.hCost = GetDistance(neighbor, targetNode);
                            neighbor.parent = currentNode;
                            
                            if (!openSet.Contains(neighbor))
                                openSet.Add(neighbor);
                        }
                    }
                }
            }
            
            // Return the result
            if (pathSuccess)
            {
                waypoints = RetracePath(startNode, targetNode);
            }
            
            callback(new Path(waypoints, pathSuccess));
        }
        
        /// <summary>
        /// Create an array of waypoints from the path
        /// </summary>
        Vector3[] RetracePath(Node startNode, Node endNode)
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;
            
            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }
            
            // Simplify the path by removing unnecessary waypoints
            Vector3[] waypoints = SimplifyPath(path);
            Array.Reverse(waypoints);
            
            return waypoints;
        }
        
        /// <summary>
        /// Simplify the path by removing unnecessary waypoints
        /// </summary>
        Vector3[] SimplifyPath(List<Node> path)
        {
            List<Vector3> waypoints = new List<Vector3>();
            Vector2 directionOld = Vector2.zero;
            
            for (int i = 1; i < path.Count; i++)
            {
                // Calculate direction from the previous node to this one
                Vector2 directionNew = new Vector2(path[i-1].gridX - path[i].gridX, path[i-1].gridY - path[i].gridY);
                
                // If the direction has changed, we need a new waypoint
                if (directionNew != directionOld)
                {
                    waypoints.Add(path[i].worldPosition);
                }
                
                directionOld = directionNew;
            }
            
            // Always add the last point
            if (path.Count > 0)
                waypoints.Add(path[0].worldPosition);
                
            return waypoints.ToArray();
        }
        
        /// <summary>
        /// Get distance between two nodes
        /// </summary>
        int GetDistance(Node nodeA, Node nodeB)
        {
            int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
            int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
            
            // Calculate the cost - 14 for diagonal movement, 10 for straight movement
            if (dstX > dstY)
                return 14 * dstY + 10 * (dstX - dstY);
            return 14 * dstX + 10 * (dstY - dstX);
        }
    }
}