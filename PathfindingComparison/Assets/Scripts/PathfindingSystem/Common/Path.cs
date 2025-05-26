using UnityEngine;

namespace PathfindingSystem.Common
{
    /// <summary>
    /// Represents a calculated path between two points
    /// </summary>
    public class Path
    {
        public Vector3[] waypoints;
        public bool success;
        
        public Path(Vector3[] waypoints, bool success)
        {
            this.waypoints = waypoints;
            this.success = success;
        }
    }
} 