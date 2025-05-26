using System;
using UnityEngine;

namespace PathfindingSystem.Common
{
    /// <summary>
    /// Interface for pathfinding algorithms
    /// </summary>
    public interface IPathfinder
    {
        /// <summary>
        /// Initialize the pathfinder
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Get the name of the algorithm
        /// </summary>
        string GetAlgorithmName();
        
        /// <summary>
        /// Find a path between two points
        /// </summary>
        void FindPath(Vector3 startPos, Vector3 targetPos, Action<Path> callback);
        
        /// <summary>
        /// Handle a dynamic obstacle at the specified position
        /// </summary>
        void HandleDynamicObstacle(Vector3 obstaclePosition, float radius);
    }
} 