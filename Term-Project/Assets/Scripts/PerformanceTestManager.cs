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
    public int maxAgents = 1000;
    public int agentIncrement = 50;
    public float[] updateRates = { 0.1f, 0.05f, 0.02f, 0.01f };
    public int[] obstacleCounts = { 0, 10, 25, 50, 100 };
    
    [Header("Test Control")]
    public bool autoRunTests = false;
    public float testDuration = 30f;
    public float warmupTime = 5f;
    
    [Header("Performance Monitoring")]
    public bool enableProfiling = true;
    public int frameRateTarget = 60;
    
    public List<GameObject> activeAgents = new List<GameObject>();
    public List<GameObject> activeObstacles = new List<GameObject>();
    private PerformanceMetrics metrics;
    public TestState currentState = TestState.Idle;
    public int currentTestIndex = 0;
    public List<TestConfiguration> testConfigurations = new List<TestConfiguration>();
    
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
        ObstacleScaling
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
        
        // Agent scaling tests
        for (int agents = agentIncrement; agents <= maxAgents; agents += agentIncrement)
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
        
        // Update rate scaling tests
        foreach (float rate in updateRates)
        {
            testConfigurations.Add(new TestConfiguration
            {
                testName = $"UpdateRate_{rate:F3}",
                agentCount = 200,
                updateRate = rate,
                obstacleCount = 0,
                testType = TestType.UpdateRateScaling
            });
        }
        
        // Obstacle scaling tests
        foreach (int obstacles in obstacleCounts)
        {
            testConfigurations.Add(new TestConfiguration
            {
                testName = $"Obstacles_{obstacles}",
                agentCount = 200,
                updateRate = 0.1f,
                obstacleCount = obstacles,
                testType = TestType.ObstacleScaling
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
        // Setup test environment
        SpawnAgents(config.agentCount, config.updateRate);
        SpawnObstacles(config.obstacleCount);
        
        // Warmup period
        yield return new WaitForSeconds(warmupTime);
        
        // Start metrics collection
        metrics.StartCollection(config.testName);
        
        // Run test
        float testStartTime = Time.time;
        while (Time.time - testStartTime < testDuration)
        {
            // Update dynamic obstacles during test
            if (config.obstacleCount > 0)
            {
                UpdateDynamicObstacles();
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        // Stop metrics collection
        metrics.StopCollection();
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
    
    public void SpawnObstacles(int count)
    {
        ClearAllObstacles();
        
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            GameObject obstacle = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
            activeObstacles.Add(obstacle);
        }
        
        // Rebuild NavMesh after spawning obstacles
        if (count > 0)
        {
            NavMeshCompatibility.RebuildNavMesh();
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
            return hit.position;
        }
        
        return spawnArea.position;
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