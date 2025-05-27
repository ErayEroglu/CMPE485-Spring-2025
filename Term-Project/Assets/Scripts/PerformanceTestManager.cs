using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.IO;
using System;

public class PerformanceTestManager : MonoBehaviour
{
    [Header("Test Configuration")]
    public GameObject agentPrefab;
    public GameObject obstaclePrefab;
    public Transform spawnArea;
    public float spawnRadius = 50f;
    
    [Header("Test Parameters")]
    public int maxAgents = 1200;
    public int agentIncrement = 200;
    public float[] updateRates = { 0.1f, 0.05f, 0.02f, 0.01f };
    public int[] obstacleCounts = { 0, 10, 25, 50, 100 };
    
    [Header("Test Control")]
    public bool autoRunTests = false;
    public float testDuration = 6f;
    public float warmupTime = 1f;
    
    [Header("Performance Monitoring")]
    public bool enableProfiling = true;
    public int frameRateTarget = 60;
    
    public List<GameObject> activeAgents = new List<GameObject>();
    public List<GameObject> activeObstacles = new List<GameObject>();
    private PerformanceMetrics metrics;
    public TestState currentState = TestState.Idle;
    public int currentTestIndex = 0;
    public List<TestConfiguration> testConfigurations = new List<TestConfiguration>();
    
    // Skip test functionality
    private bool skipCurrentTest = false;
    
    public enum TestState
    {
        Idle,
        Running,
        Collecting,
        Completed
    }
    
    [System.Serializable]
    public class TestConfiguration
    {
        public string testName;
        public int agentCount;
        public float updateRate;
        public int obstacleCount;
        public TestType testType;
    }
    
    public enum TestType
    {
        AgentScaling,
        UpdateRateScaling,
        ObstacleScaling,
        DynamicObstacleScaling,
        AgentWithDynamicObstacles
    }
    
    private void Start()
    {
        metrics = GetComponent<PerformanceMetrics>();
        if (metrics == null)
        {
            metrics = gameObject.AddComponent<PerformanceMetrics>();
        }
        
        GenerateTestConfigurations();
        
        if (autoRunTests)
        {
            StartCoroutine(RunAllTests());
        }
    }
    
    private void GenerateTestConfigurations()
    {
        testConfigurations.Clear();
        
        // 1) Test agent counts from 10, 100, 200, 400, 600, 1000, 1500, 2000, 5000, 10000
        int[] agentCounts = { 10, 100, 200, 400, 600, 1000, 1500, 2000, 5000, 10000 };
        foreach (int agents in agentCounts)
        {
            testConfigurations.Add(new TestConfiguration
            {
                testName = $"AgentScaling_{agents}",
                agentCount = agents,
                updateRate = 0.1f,
                obstacleCount = 0,
                testType = TestType.AgentScaling
            });
        }
        
        // 2) Test 5 times with increased number of static obstacles (with 600 agents)
        int[] staticObstacleCounts = { 10, 25, 50, 100, 200 };
        foreach (int obstacles in staticObstacleCounts)
        {
            testConfigurations.Add(new TestConfiguration
            {
                testName = $"StaticObstacles_{obstacles}_Agents600",
                agentCount = 600,
                updateRate = 0.1f,
                obstacleCount = obstacles,
                testType = TestType.ObstacleScaling
            });
        }
        
        // 3) Test 5 times with moving and increased number of obstacles (with 600 agents)
        int[] dynamicObstacleCounts = { 10, 25, 50, 100, 200 };
        foreach (int obstacles in dynamicObstacleCounts)
        {
            testConfigurations.Add(new TestConfiguration
            {
                testName = $"DynamicObstacles_{obstacles}_Agents600",
                agentCount = 600,
                updateRate = 0.1f,
                obstacleCount = obstacles,
                testType = TestType.DynamicObstacleScaling
            });
        }
        
        // 4) Test with moving obstacles (at highest obstacle count from previous stage) with varying agent counts
        int[] agentCountsWithMovingObstacles = { 200, 500, 1000, 2000, 5000 };
        foreach (int agents in agentCountsWithMovingObstacles)
        {
            testConfigurations.Add(new TestConfiguration
            {
                testName = $"AgentsWithMovingObstacles_{agents}",
                agentCount = agents,
                updateRate = 0.1f,
                obstacleCount = 50, // Highest obstacle count from previous stage
                testType = TestType.AgentWithDynamicObstacles
            });
        }
    }
    
    public IEnumerator RunAllTests()
    {
        currentState = TestState.Running;
        
        for (currentTestIndex = 0; currentTestIndex < testConfigurations.Count; currentTestIndex++)
        {
            var config = testConfigurations[currentTestIndex];
            Debug.Log($"Starting test: {config.testName}");
            
            yield return StartCoroutine(RunSingleTest(config));
            
            // Clean up between tests
            ClearAllAgents();
            ClearAllObstacles();
            
            // Wait a bit between tests
            yield return new WaitForSeconds(2f);
        }
        
        currentState = TestState.Completed;
        Debug.Log("All tests completed!");
        
        // Export results
        metrics.ExportResults();
    }
    
    private IEnumerator RunSingleTest(TestConfiguration config)
    {
        Debug.Log($"Starting test: {config.testName}");
        float testStartTime = Time.realtimeSinceStartup;
        skipCurrentTest = false; // Reset skip flag for this test
        
        // Setup test environment
        SpawnAgents(config.agentCount, config.updateRate);
        yield return StartCoroutine(SpawnObstacles(config.obstacleCount));
        
        // Check for skip during setup
        if (skipCurrentTest)
        {
            Debug.Log($"Test skipped during setup: {config.testName}");
            yield break;
        }
        
        // Configure obstacles based on test type
        if (config.testType == TestType.DynamicObstacleScaling || config.testType == TestType.AgentWithDynamicObstacles)
        {
            ConfigureDynamicObstacles(true, 2.0f + (config.obstacleCount * 0.1f)); // Increase speed with count
        }
        else
        {
            ConfigureDynamicObstacles(false, 1.0f); // Static obstacles
        }
        
        // Warmup period - use realtime to ensure exact timing
        Debug.Log($"Starting warmup for {warmupTime}s");
        float warmupEndTime = Time.realtimeSinceStartup + warmupTime;
        while (Time.realtimeSinceStartup < warmupEndTime && !skipCurrentTest)
        {
            yield return null;
        }
        
        // Check for skip during warmup
        if (skipCurrentTest)
        {
            Debug.Log($"Test skipped during warmup: {config.testName}");
            yield break;
        }
        
        // Start metrics collection
        Debug.Log($"Starting data collection for {testDuration}s");
        metrics.StartCollection(config.testName);
        float dataStartTime = Time.realtimeSinceStartup;
        
        // Start obstacle update coroutine for dynamic tests
        Coroutine obstacleUpdateCoroutine = null;
        if ((config.testType == TestType.DynamicObstacleScaling || config.testType == TestType.AgentWithDynamicObstacles) && config.obstacleCount > 0)
        {
            obstacleUpdateCoroutine = StartCoroutine(UpdateDynamicObstaclesCoroutine());
        }
        
        // Run test for exact duration using realtime, but check for skip
        float testEndTime = Time.realtimeSinceStartup + testDuration;
        while (Time.realtimeSinceStartup < testEndTime && !skipCurrentTest)
        {
            yield return null;
        }
        
        // Stop obstacle updates
        if (obstacleUpdateCoroutine != null)
        {
            StopCoroutine(obstacleUpdateCoroutine);
        }
        
        // Stop metrics collection
        metrics.StopCollection();
        
        float totalTestTime = Time.realtimeSinceStartup - testStartTime;
        float dataCollectionTime = Time.realtimeSinceStartup - dataStartTime;
        
        if (skipCurrentTest)
        {
            Debug.Log($"Test skipped: {config.testName} | Time before skip: {totalTestTime:F2}s");
        }
        else
        {
            Debug.Log($"Test completed: {config.testName} | Total time: {totalTestTime:F2}s | Data collection time: {dataCollectionTime:F2}s");
        }
    }
    
    private IEnumerator UpdateDynamicObstaclesCoroutine()
    {
        while (true)
        {
            UpdateDynamicObstacles();
            yield return new WaitForSecondsRealtime(1f); // Update every 1 second realtime
        }
    }
    
    public void SpawnAgents(int count, float updateRate)
    {
        ClearAllAgents();
        
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            GameObject agent = Instantiate(agentPrefab, spawnPos, Quaternion.identity);
            
            // Configure agent
            var movement = agent.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.UpdateRate = updateRate;
                movement.Player = FindObjectOfType<PlayerMovement>()?.transform;
            }
            
            activeAgents.Add(agent);
        }
    }
    
    public IEnumerator SpawnObstacles(int count)
    {
        ClearAllObstacles();
        
        if (count == 0) 
        {
            yield break;
        }
        
        Debug.Log($"Spawning {count} obstacles...");
        
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            // Ensure obstacles are positioned at Y = 0.5 (on ground level)
            spawnPos.y = obstaclePrefab.transform.position.y;
            
            GameObject obstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
            
            // Ensure NavMeshObstacle is properly configured and enabled
            NavMeshObstacle navObstacle = obstacle.GetComponent<NavMeshObstacle>();
            if (navObstacle != null)
            {
                // Make sure it's enabled from the start
                navObstacle.enabled = true;
                navObstacle.carving = true;
                navObstacle.carvingMoveThreshold = 0.1f;
                navObstacle.carvingTimeToStationary = 0.1f; // Faster carving response
                navObstacle.shape = NavMeshObstacleShape.Box;
                navObstacle.center = Vector3.zero;
                
                // Set size based on renderer bounds
                Renderer renderer = obstacle.GetComponent<Renderer>();
                if (renderer != null)
                {
                    navObstacle.size = renderer.bounds.size;
                }
                else
                {
                    navObstacle.size = Vector3.one; // Default size
                }
                
                Debug.Log($"Obstacle {i}: NavMeshObstacle enabled={navObstacle.enabled}, carving={navObstacle.carving}, size={navObstacle.size}");
            }
            else
            {
                Debug.LogError($"Obstacle {i} is missing NavMeshObstacle component!");
            }
            
            activeObstacles.Add(obstacle);
            
            // Wait a frame between spawning obstacles to let NavMesh update
            yield return null;
        }
        
        // Force multiple NavMesh rebuilds to ensure obstacles are properly integrated
        Debug.Log("Rebuilding NavMesh for obstacles...");
        yield return new WaitForEndOfFrame();
        
        NavMeshCompatibility.RebuildNavMesh();
        yield return new WaitForEndOfFrame();
        
        // Second rebuild to ensure all obstacles are accounted for
        NavMeshCompatibility.RebuildNavMesh();
        yield return new WaitForEndOfFrame();
        
        Debug.Log($"Spawned {count} obstacles and rebuilt NavMesh. Checking obstacle status...");
        
        // Verify obstacles are working
        int workingObstacles = 0;
        int enabledObstacles = 0;
        int carvingObstacles = 0;
        
        foreach (var obstacle in activeObstacles)
        {
            if (obstacle != null)
            {
                NavMeshObstacle navObs = obstacle.GetComponent<NavMeshObstacle>();
                if (navObs != null)
                {
                    if (navObs.enabled) enabledObstacles++;
                    if (navObs.carving) carvingObstacles++;
                    if (navObs.enabled && navObs.carving)
                    {
                        workingObstacles++;
                    }
                    
                    Debug.Log($"Obstacle at {obstacle.transform.position}: enabled={navObs.enabled}, carving={navObs.carving}, size={navObs.size}");
                }
            }
        }
        
        Debug.Log($"Obstacle Summary: Total={count}, Enabled={enabledObstacles}, Carving={carvingObstacles}, Working={workingObstacles}");
        
        // Check NavMesh status
        if (NavMesh.CalculateTriangulation().vertices.Length > 0)
        {
            Debug.Log("NavMesh is properly built and has walkable areas.");
        }
        else
        {
            Debug.LogError("NavMesh appears to be empty or not built!");
        }
        
        // Force all agents to recalculate their paths
        ForceAgentPathRecalculation();
    }
    
    private void ConfigureDynamicObstacles(bool enableMovement, float speed)
    {
        foreach (var obstacle in activeObstacles)
        {
            if (obstacle != null)
            {
                var dynamicObstacle = obstacle.GetComponent<DynamicObstacle>();
                if (dynamicObstacle != null)
                {
                    if (enableMovement)
                    {
                        dynamicObstacle.movementType = DynamicObstacle.ObstacleMovementType.Random;
                        dynamicObstacle.SetMoveSpeed(speed);
                        dynamicObstacle.moveRadius = 15f;
                        dynamicObstacle.randomizeMovement = true; // Allow randomization for dynamic tests
                    }
                    else
                    {
                        dynamicObstacle.movementType = DynamicObstacle.ObstacleMovementType.Static;
                        dynamicObstacle.randomizeMovement = false; // Prevent automatic movement changes
                    }
                }
            }
        }
    }

    private void UpdateDynamicObstacles()
    {
        foreach (var obstacle in activeObstacles)
        {
            if (obstacle != null && UnityEngine.Random.Range(0f, 1f) < 0.1f) // 10% chance to move
            {
                Vector3 newPos = GetRandomSpawnPosition();
                obstacle.transform.position = newPos;
            }
        }
        
        // Rebuild NavMesh periodically for dynamic obstacles
        if (UnityEngine.Random.Range(0f, 1f) < 0.05f) // 5% chance to rebuild
        {
            NavMeshCompatibility.RebuildNavMesh();
        }
    }
    
    private Vector3 GetRandomSpawnPosition()
    {
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = spawnArea.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // Ensure position is on NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPos, out hit, 10f, NavMesh.AllAreas))
        {
            // Return hit position but ensure Y is at ground level (0)
            return new Vector3(hit.position.x, 0, hit.position.z);
        }
        
        // Fallback to spawn area position at ground level
        return new Vector3(spawnArea.position.x, 0, spawnArea.position.z);
    }
    
    public void ClearAllAgents()
    {
        foreach (var agent in activeAgents)
        {
            if (agent != null)
                DestroyImmediate(agent);
        }
        activeAgents.Clear();
    }
    
    public void ClearAllObstacles()
    {
        foreach (var obstacle in activeObstacles)
        {
            if (obstacle != null)
                DestroyImmediate(obstacle);
        }
        activeObstacles.Clear();
    }
    
    // Public methods for manual testing
    public void RunAgentScalingTest(int agentCount)
    {
        StartCoroutine(RunSingleTest(new TestConfiguration
        {
            testName = $"Manual_AgentScaling_{agentCount}",
            agentCount = agentCount,
            updateRate = 0.1f,
            obstacleCount = 0,
            testType = TestType.AgentScaling
        }));
    }
    
    public void RunUpdateRateTest(float updateRate)
    {
        StartCoroutine(RunSingleTest(new TestConfiguration
        {
            testName = $"Manual_UpdateRate_{updateRate:F3}",
            agentCount = 200,
            updateRate = updateRate,
            obstacleCount = 0,
            testType = TestType.UpdateRateScaling
        }));
    }
    
    public void RunObstacleTest(int obstacleCount)
    {
        StartCoroutine(RunSingleTest(new TestConfiguration
        {
            testName = $"Manual_Obstacles_{obstacleCount}",
            agentCount = 200,
            updateRate = 0.1f,
            obstacleCount = obstacleCount,
            testType = TestType.ObstacleScaling
        }));
    }
    
    public void SkipCurrentTest()
    {
        if (currentState == TestState.Running)
        {
            skipCurrentTest = true;
            Debug.Log("Skipping current test...");
        }
        else
        {
            Debug.Log("No test is currently running to skip.");
        }
    }
    
    private void ForceAgentPathRecalculation()
    {
        Debug.Log("Forcing agent path recalculation...");
        int recalculatedAgents = 0;
        
        foreach (var agent in activeAgents)
        {
            if (agent != null)
            {
                NavMeshAgent navAgent = agent.GetComponent<NavMeshAgent>();
                if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
                {
                    // Force recalculation by setting a new destination
                    Vector3 currentDestination = navAgent.destination;
                    navAgent.ResetPath();
                    navAgent.SetDestination(currentDestination);
                    recalculatedAgents++;
                }
            }
        }
        
        Debug.Log($"Recalculated paths for {recalculatedAgents} agents");
    }
    
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"Test State: {currentState}");
        GUILayout.Label($"Active Agents: {activeAgents.Count}");
        GUILayout.Label($"Active Obstacles: {activeObstacles.Count}");
        
        if (currentState == TestState.Running)
        {
            GUILayout.Label($"Current Test: {currentTestIndex + 1}/{testConfigurations.Count}");
            if (currentTestIndex < testConfigurations.Count)
            {
                GUILayout.Label($"Test Name: {testConfigurations[currentTestIndex].testName}");
            }
        }
        
        GUILayout.EndArea();
    }
} 