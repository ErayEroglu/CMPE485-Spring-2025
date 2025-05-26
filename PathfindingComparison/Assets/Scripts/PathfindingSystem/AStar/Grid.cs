using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// skipcq: CS-R1035
namespace PathfindingSystem.AStar
{
    /// <summary>
    /// Represents a node in the A* grid
    /// </summary>
    public class Node
    {
        public bool walkable;
        public Vector3 worldPosition;
        public int gridX;
        public int gridY;
        
        public int gCost; // Distance from start node
        public int hCost; // Distance to target node
        public Node parent;
        
        // F cost is G cost + H cost
        public int fCost
        {
            get { return gCost + hCost; }
        }
        
        public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY)
        {
            this.walkable = walkable;
            this.worldPosition = worldPosition;
            this.gridX = gridX;
            this.gridY = gridY;
        }
    }
    
    /// <summary>
    /// Grid system used by A* pathfinding
    /// </summary>
    public class Grid : MonoBehaviour
    {
        [Header("Grid Settings")]
        public LayerMask unwalkableMask;
        public Vector2 gridWorldSize = new Vector2(100f, 100f);
        public float nodeRadius = 0.5f;
        public bool displayGridGizmos;
        
        [Header("Dynamic Updates")]
        public bool updateInRealtime = false;
        public float updateFrequency = 0.5f;
        
        private Node[,] grid;
        private float nodeDiameter;
        private int gridSizeX, gridSizeY;
        private float lastUpdateTime;
        
        private void Awake()
        {
            nodeDiameter = nodeRadius * 2f;
            gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
            gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
            
            CreateGrid();
        }
        
        private void Update()
        {
            if (updateInRealtime && Time.time > lastUpdateTime + updateFrequency)
            {
                UpdateGrid();
                lastUpdateTime = Time.time;
            }
        }
        
        /// <summary>
        /// Create the initial grid
        /// </summary>
        void CreateGrid()
        {
            grid = new Node[gridSizeX, gridSizeY];
            Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;
            
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                    bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask);
                    grid[x, y] = new Node(walkable, worldPoint, x, y);
                }
            }
        }
        
        /// <summary>
        /// Update the walkability of nodes in the grid
        /// </summary>
        public void UpdateGrid()
        {
            if (grid == null) return;
            
            Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;
            
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                    bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask);
                    grid[x, y].walkable = walkable;
                }
            }
        }
        
        /// <summary>
        /// Update a specific area in the grid (for dynamic obstacles)
        /// </summary>
        public void UpdateGridArea(Vector3 center, float radius)
        {
            if (grid == null) return;
            
            // Convert world position to grid coordinates
            List<Node> affectedNodes = GetNodesInRadius(center, radius);
            
            foreach (Node node in affectedNodes)
            {
                // Check if the node is walkable based on physics
                bool walkable = !Physics.CheckSphere(node.worldPosition, nodeRadius, unwalkableMask);
                node.walkable = walkable;
            }
        }
        
        /// <summary>
        /// Get all neighbors of a node
        /// </summary>
        public List<Node> GetNeighbors(Node node)
        {
            List<Node> neighbors = new List<Node>();
            
            // Check 8 surrounding nodes
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue; // Skip self
                    
                    int checkX = node.gridX + x;
                    int checkY = node.gridY + y;
                    
                    if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                    {
                        neighbors.Add(grid[checkX, checkY]);
                    }
                }
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// Convert a world position to a node in the grid
        /// </summary>
        public Node NodeFromWorldPoint(Vector3 worldPosition)
        {
            float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
            float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;
            
            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);
            
            int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
            int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
            
            return grid[x, y];
        }
        
        /// <summary>
        /// Get all nodes within a radius from a world position
        /// </summary>
        public List<Node> GetNodesInRadius(Vector3 center, float radius)
        {
            List<Node> inRangeNodes = new List<Node>();
            
            // Convert radius to grid coordinates
            int gridRadius = Mathf.CeilToInt(radius / nodeDiameter);
            Node centerNode = NodeFromWorldPoint(center);
            
            // Check all nodes in a square around the center, filtering by actual distance
            for (int x = -gridRadius; x <= gridRadius; x++)
            {
                for (int y = -gridRadius; y <= gridRadius; y++)
                {
                    int checkX = centerNode.gridX + x;
                    int checkY = centerNode.gridY + y;
                    
                    if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                    {
                        Node node = grid[checkX, checkY];
                        float distance = Vector3.Distance(center, node.worldPosition);
                        
                        if (distance <= radius)
                        {
                            inRangeNodes.Add(node);
                        }
                    }
                }
            }
            
            return inRangeNodes;
        }
        
        void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
            
            if (grid != null && displayGridGizmos)
            {
                foreach (Node n in grid)
                {
                    Gizmos.color = n.walkable ? Color.white : Color.red;
                    Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
                }
            }
        }
        
        public int MaxSize { get { return gridSizeX * gridSizeY; } }
    }
}