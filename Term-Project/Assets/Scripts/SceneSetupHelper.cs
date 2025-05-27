using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneSetupHelper : MonoBehaviour
{
    [Header("Auto Setup Configuration")]
    public bool autoSetupOnStart = false;
    public Vector3 groundSize = new Vector3(30, 1, 30);
    public Material groundMaterial;
    
    [Header("Prefab Creation")]
    public bool createPrefabs = true;
    public Material agentMaterial;
    public Material obstacleMaterial;
    
    [Header("Test Manager Setup")]
    public bool setupTestManager = true;
    
    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupScene();
        }
    }
    
        [ContextMenu("Setup Complete Scene")]
    public void SetupScene()
    {
        Debug.Log("Setting up NavMesh Performance Test Scene...");

        // Create required tags
        CreateRequiredTags();

        // Create ground plane
        CreateGround();
        
        // Create prefabs if needed
        if (createPrefabs)
        {
            CreateAgentPrefab();
            CreateObstaclePrefab();
        }
        
                // Create player controller (needed as target for agents)
        CreatePlayerController();

        // Setup test manager
        if (setupTestManager)
        {
            SetupTestManager();
        }

        // Setup NavMesh
        SetupNavMesh();
        
        // Setup camera
        SetupCamera();
        
        Debug.Log("Scene setup complete! You can now run performance tests.");
    }
    
    private void CreateGround()
    {
        // Check if ground already exists
        GameObject existingGround = GameObject.Find("Ground");
        if (existingGround != null)
        {
            Debug.Log("Ground already exists, skipping creation.");
            return;
        }
        
        // Create ground plane
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = groundSize;
        ground.transform.position = Vector3.zero;
        
        // Apply material if provided
        if (groundMaterial != null)
        {
            ground.GetComponent<Renderer>().material = groundMaterial;
        }
        
        // Add NavMesh Surface (if available)
        NavMeshCompatibility.CreateNavMeshSurface(ground);
        
        Debug.Log("Ground plane created with NavMesh Surface.");
    }
    
    private GameObject CreateAgentPrefab()
    {
        // Create agent GameObject
        GameObject agent = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        agent.name = "Agent";
        
        // Add NavMeshAgent
        NavMeshAgent navAgent = agent.AddComponent<NavMeshAgent>();
        navAgent.speed = 3.5f;
        navAgent.acceleration = 8f;
        navAgent.angularSpeed = 120f;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        
        // Add EnemyMovement script (used by PerformanceTestManager)
        EnemyMovement enemyMovement = agent.AddComponent<EnemyMovement>();
        enemyMovement.UpdateRate = 0.1f;
        
        // Apply material
        if (agentMaterial != null)
        {
            agent.GetComponent<Renderer>().material = agentMaterial;
        }
        else
        {
            // Create a simple colored material
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.blue;
            agent.GetComponent<Renderer>().material = mat;
        }
        
#if UNITY_EDITOR
        // Save as prefab
        string prefabPath = "Assets/Agent.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(agent, prefabPath);
        Debug.Log($"Agent prefab created at: {prefabPath}");
        
        // Clean up scene object
        DestroyImmediate(agent);
        return prefab;
#else
        return agent;
#endif
    }
    
    private GameObject CreateObstaclePrefab()
    {
        // Create obstacle GameObject
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = "DynamicObstacle";
        obstacle.tag = "Obstacle";
        Vector3 obstacleSize = new Vector3(1, 4, 6);
        obstacle.transform.localScale = obstacleSize;
        
        // Position obstacle properly on ground
        obstacle.transform.position = new Vector3(0, obstacleSize.y / 2.0f, 0);
        
        // Add NavMeshObstacle
        NavMeshObstacle navObstacle = obstacle.AddComponent<NavMeshObstacle>();
        navObstacle.carving = true;
        navObstacle.carvingMoveThreshold = 0.1f;
        navObstacle.carvingTimeToStationary = 0.5f;
        navObstacle.enabled = true; // Ensure it's enabled by default
        navObstacle.shape = NavMeshObstacleShape.Box;
        navObstacle.center = Vector3.zero;
        navObstacle.size = Vector3.one; // Default size, will be adjusted when spawned
        
        // Add DynamicObstacle script
        DynamicObstacle dynamicObstacle = obstacle.AddComponent<DynamicObstacle>();
        dynamicObstacle.movementType = DynamicObstacle.ObstacleMovementType.Random;
        dynamicObstacle.enableOptimizations = true;
        
        // Apply material
        if (obstacleMaterial != null)
        {
            obstacle.GetComponent<Renderer>().material = obstacleMaterial;
        }
        else
        {
            // Create a simple colored material
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.red;
            obstacle.GetComponent<Renderer>().material = mat;
        }
        
#if UNITY_EDITOR
        // Save as prefab
        string prefabPath = "Assets/DynamicObstacle.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obstacle, prefabPath);
        Debug.Log($"Obstacle prefab created at: {prefabPath}");
        
        // Clean up scene object
        DestroyImmediate(obstacle);
        return prefab;
#else
        return obstacle;
#endif
    }
    
    private void SetupTestManager()
    {
        // Check if test manager already exists
        PerformanceTestManager existingManager = FindObjectOfType<PerformanceTestManager>();
        if (existingManager != null)
        {
            Debug.Log("Test Manager already exists, skipping creation.");
            return;
        }
        
        // Create test manager GameObject
        GameObject testManager = new GameObject("TestManager");
        testManager.transform.position = Vector3.zero;
        
        // Add PerformanceTestManager
        PerformanceTestManager manager = testManager.AddComponent<PerformanceTestManager>();
        
        // Add PerformanceMetrics
        PerformanceMetrics metrics = testManager.AddComponent<PerformanceMetrics>();
        
        // Add TestUIController for keyboard input handling
        TestUIController uiController = testManager.AddComponent<TestUIController>();
        
        // Try to assign prefabs
#if UNITY_EDITOR
        string agentPrefabPath = "Assets/Agent.prefab";
        string obstaclePrefabPath = "Assets/DynamicObstacle.prefab";
        
        GameObject agentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(agentPrefabPath);
        GameObject obstaclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(obstaclePrefabPath);
        
        if (agentPrefab != null)
        {
            manager.agentPrefab = agentPrefab;
            Debug.Log("Agent prefab assigned to Test Manager.");
        }
        
        if (obstaclePrefab != null)
        {
            manager.obstaclePrefab = obstaclePrefab;
            Debug.Log("Obstacle prefab assigned to Test Manager.");
        }
#endif
        
        // Set spawn area to test manager transform
        manager.spawnArea = testManager.transform;
        manager.spawnRadius = 40f;
        
        Debug.Log("Test Manager created and configured with UI Controller for keyboard shortcuts.");
    }
    
    private void CreateRequiredTags()
    {
#if UNITY_EDITOR
        // Create "Obstacle" tag if it doesn't exist
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        bool obstacleTagExists = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == "Obstacle")
            {
                obstacleTagExists = true;
                break;
            }
        }
        
        if (!obstacleTagExists)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
            newTagProp.stringValue = "Obstacle";
            tagManager.ApplyModifiedProperties();
            Debug.Log("Created 'Obstacle' tag.");
        }
#endif
    }

    private void SetupNavMesh()
    {
        // Bake NavMesh using compatibility helper
        NavMeshCompatibility.RebuildNavMesh();
        Debug.Log("NavMesh baked successfully.");
    }
    
    [ContextMenu("Create Player Controller")]
    public void CreatePlayerController()
    {
        // Check if player already exists
        PlayerMovement existingPlayer = FindObjectOfType<PlayerMovement>();
        if (existingPlayer != null)
        {
            Debug.Log("Player controller already exists.");
            return;
        }
        
        // Create player GameObject
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        player.name = "Player";
        player.transform.position = new Vector3(0, 0.5f, 0);
        
        // Add NavMeshAgent
        NavMeshAgent navAgent = player.AddComponent<NavMeshAgent>();
        
        // Add PlayerMovement script
        PlayerMovement playerMovement = player.AddComponent<PlayerMovement>();
        
        // Apply material
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.green;
        player.GetComponent<Renderer>().material = mat;
        
        Debug.Log("Player controller created. Click to move around the scene.");
    }
    
    [ContextMenu("Setup Camera")]
    public void SetupCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("No main camera found.");
            return;
        }
        
        // Position camera for top-down view
        mainCamera.transform.position = new Vector3(0, 30, 0);
        mainCamera.transform.rotation = Quaternion.Euler(90, 0, 0); // Look straight down
        
        // Add camera follow script
        CameraFollow cameraFollow = mainCamera.GetComponent<CameraFollow>();
        if (cameraFollow == null)
        {
            cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow>();
        }
        
        Debug.Log("Camera positioned for top-down view with follow script.");
    }
    
#if UNITY_EDITOR
    [MenuItem("NavMesh Performance/Setup Scene")]
    public static void SetupSceneFromMenu()
    {
        SceneSetupHelper helper = FindObjectOfType<SceneSetupHelper>();
        if (helper == null)
        {
            GameObject helperObj = new GameObject("SceneSetupHelper");
            helper = helperObj.AddComponent<SceneSetupHelper>();
        }
        
        helper.SetupScene();
    }
    
    [MenuItem("NavMesh Performance/Create Test Prefabs")]
    public static void CreateTestPrefabsFromMenu()
    {
        SceneSetupHelper helper = FindObjectOfType<SceneSetupHelper>();
        if (helper == null)
        {
            GameObject helperObj = new GameObject("SceneSetupHelper");
            helper = helperObj.AddComponent<SceneSetupHelper>();
        }
        
        helper.CreateAgentPrefab();
        helper.CreateObstaclePrefab();
    }
#endif
} 