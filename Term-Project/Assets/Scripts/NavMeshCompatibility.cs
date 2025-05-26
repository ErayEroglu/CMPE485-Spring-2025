using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Compatibility helper for NavMesh operations
public static class NavMeshCompatibility
{
    public static void RebuildNavMesh()
    {
        try
        {
            // Try to find NavMeshSurface first (newer approach)
            var surface = Object.FindObjectOfType<Unity.AI.Navigation.NavMeshSurface>();
            if (surface != null)
            {
                surface.BuildNavMesh();
                return;
            }
        }
        catch (System.Exception)
        {
            // NavMeshSurface not available, fall back to legacy method
        }

#if UNITY_EDITOR
        // Fallback to legacy NavMesh baking (older Unity versions)
        try
        {
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not rebuild NavMesh: {e.Message}");
        }
#else
        Debug.LogWarning("NavMesh rebuild not available in build. Consider using NavMeshSurface component.");
#endif
    }

    public static bool HasNavMeshSurface()
    {
        try
        {
            return Object.FindObjectOfType<Unity.AI.Navigation.NavMeshSurface>() != null;
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    public static GameObject CreateNavMeshSurface(GameObject target)
    {
        try
        {
            var surface = target.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            
            // Use reflection to set properties safely
            var surfaceType = surface.GetType();
            
            // Set collectObjects
            var collectObjectsProperty = surfaceType.GetProperty("collectObjects");
            if (collectObjectsProperty != null)
            {
                var collectObjectsType = collectObjectsProperty.PropertyType;
                var allValue = System.Enum.Parse(collectObjectsType, "All");
                collectObjectsProperty.SetValue(surface, allValue);
            }
            
            // Set useGeometry
            var useGeometryProperty = surfaceType.GetProperty("useGeometry");
            if (useGeometryProperty != null)
            {
                var useGeometryType = useGeometryProperty.PropertyType;
                var renderMeshesValue = System.Enum.Parse(useGeometryType, "RenderMeshes");
                useGeometryProperty.SetValue(surface, renderMeshesValue);
            }
            
            return target;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"NavMeshSurface component not available: {e.Message}. Please install AI Navigation package from Package Manager.");
            return target;
        }
    }
} 