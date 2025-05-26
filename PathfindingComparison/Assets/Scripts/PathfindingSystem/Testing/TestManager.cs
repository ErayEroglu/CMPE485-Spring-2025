using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PathfindingSystem.Common;
using PathfindingSystem.AStar;
using PathfindingSystem.NavMesh;

namespace PathfindingSystem.Testing
{
    /// <summary>
    /// Manager for running pathfinding performance tests
    /// </summary>
    public class TestManager : MonoBehaviour
    {
        [System.Serializable]
        public enum PathfindingAlgorithm
        {
            AStar,
            NavMesh
        }
        
        [Header("Test Settings")]
        public PathfindingAlgorithm currentAlgorithm = PathfindingAlgorithm.AStar;
        public int currentAgentCount = 10;
        public bool autoRunTests = false;
        
        [Header("Agent Settings")]
        public GameObject agentPrefab;
        public Vector3 spawnAreaCenter = Vector3.zero;
        public Vector3 spawnAreaSize = new Vector3(20f, 0f, 20f);
        public Vector3 targetAreaCenter = new Vector3(0f, 0f, 20f);
        public Vector3 targetAreaSize = new Vector3(20f, 0f, 20f);
        
        [Header("Testing Progression")]
        public int[] agentCountsToTest = new int[] { 10, 50, 100, 200, 500, 1000 };
        public float testDuration = 20f;
        public float delayBetweenTests = 5f;
        public int currentTestIndex = 0;
        
        [Header("Obstacles")]
        public GameObject obstaclePrefab;
        public int staticObstacleCount = 20;
        public int dynamicObstacleCount = 10;
        public float obstacleSpawnInterval = 2f;
        
        [Header("UI")]
        public TextMeshProUGUI algorithmText;
        public TextMeshProUGUI agentCountText;
        public TextMeshProUGUI fpsText;
        public TextMeshProUGUI cpuText;
        public TextMeshProUGUI memoryText;
        
        // References
        private PerformanceMonitor performanceMonitor;
        private AStarPathfinder astarPathfinder;
        private NavMeshPathfinder navMeshPathfinder;
        private IPathfinder currentPathfinder;
        
        // Runtime data
        private List<GameObject> activeAgents = new List<GameObject>();
        private List<GameObject> obstacles = new List<GameObject>();
        private bool isTestRunning = false;
        private float testStartTime;
        private float nextObstacleTime;
        
        private void Start()
        {
            // Find or create required components
            FindOrCreatePathfinders();
            FindOrCreatePerformanceMonitor();
            
            // Initialize UI
            UpdateUI();
            
            // Start with first test if auto run is enabled
            if (autoRunTests)
            {
                StartCoroutine(RunTestSequence());
            }
        }
        
        private void FindOrCreatePathfinders()
        {
            // Find A* pathfinder
            astarPathfinder = FindObjectOfType<AStarPathfinder>();
            if (astarPathfinder == null)
            {
                GameObject astarObj = new GameObject("AStarPathfinder");
                astarPathfinder = astarObj.AddComponent<AStarPathfinder>();
                Debug.Log("Created AStarPathfinder GameObject");
            }
            
            // Find NavMesh pathfinder
            navMeshPathfinder = FindObjectOfType<NavMeshPathfinder>();
            if (navMeshPathfinder == null)
            {
                GameObject navMeshObj = new GameObject("NavMeshPathfinder");
                navMeshPathfinder = navMeshObj.AddComponent<NavMeshPathfinder>();
                Debug.Log("Created NavMeshPathfinder GameObject");
            }
            
            // Initialize pathfinders
            astarPathfinder.Initialize();
            navMeshPathfinder.Initialize();
            
            // Set current pathfinder based on selected algorithm
            SetCurrentPathfinder();
        }
        
        private void FindOrCreatePerformanceMonitor()
        {
            performanceMonitor = FindObjectOfType<PerformanceMonitor>();
            if (performanceMonitor == null)
            {
                GameObject monitorObj = new GameObject("PerformanceMonitor");
                performanceMonitor = monitorObj.AddComponent<PerformanceMonitor>();
                Debug.Log("Created PerformanceMonitor GameObject");
            }
        }
        
        private void Update()
        {
            if (isTestRunning)
            {
                // Update UI
                UpdateUI();
                
                // Spawn dynamic obstacles periodically
                if (Time.time >= nextObstacleTime)
                {
                    SpawnDynamicObstacle();
                    nextObstacleTime = Time.time + obstacleSpawnInterval;
                }
                
                // Check if test duration has elapsed
                if (Time.time >= testStartTime + testDuration)
                {
                    StopTest();
                    
                    if (autoRunTests)
                    {
                        StartCoroutine(MoveToNextTest());
                    }
                }
            }
        }
        
        private void SetCurrentPathfinder()
        {
            switch (currentAlgorithm)
            {
                case PathfindingAlgorithm.AStar:
                    currentPathfinder = astarPathfinder;
                    break;
                case PathfindingAlgorithm.NavMesh:
                    currentPathfinder = navMeshPathfinder;
                    break;
            }
            
            if (performanceMonitor != null)
            {
                performanceMonitor.SetPathfindingMethod(currentPathfinder.GetAlgorithmName());
            }
        }
        
        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
                0,
                Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
            );
            
            return spawnAreaCenter + randomPos;
        }
        
        private Vector3 GetRandomTargetPosition()
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-targetAreaSize.x / 2, targetAreaSize.x / 2),
                0,
                Random.Range(-targetAreaSize.z / 2, targetAreaSize.z / 2)
            );
            
            return targetAreaCenter + randomPos;
        }
        
        private void SpawnAgent()
        {
            // Instantiate agent
            GameObject agent = Instantiate(agentPrefab, GetRandomSpawnPosition(), Quaternion.identity);
            
            // Get agent component
            Agent agentComponent = agent.GetComponent<Agent>();
            if (agentComponent == null)
            {
                agentComponent = agent.AddComponent<Agent>();
            }
            
            // Set pathfinder and target
            agentComponent.SetPathfinder(currentPathfinder);
            agentComponent.SetTargetPosition(GetRandomTargetPosition());
            
            // Add to active agents list
            activeAgents.Add(agent);
        }
        
        private void SpawnStaticObstacles()
        {
            for (int i = 0; i < staticObstacleCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-spawnAreaSize.x, spawnAreaSize.x),
                    0,
                    Random.Range(-spawnAreaSize.z, spawnAreaSize.z)
                );
                
                GameObject obstacle = Instantiate(obstaclePrefab, position, Quaternion.identity);
                obstacles.Add(obstacle);
            }
        }
        
        private void SpawnDynamicObstacle()
        {
            if (obstacles.Count >= staticObstacleCount + dynamicObstacleCount)
                return;
                
            Vector3 position = new Vector3(
                Random.Range(-spawnAreaSize.x, spawnAreaSize.x),
                0,
                Random.Range(-spawnAreaSize.z, spawnAreaSize.z)
            );
            
            GameObject obstacle = Instantiate(obstaclePrefab, position, Quaternion.identity);
            obstacles.Add(obstacle);
            
            // Notify pathfinder of obstacle
            currentPathfinder.HandleDynamicObstacle(position, 1f);
        }
        
        private void ClearAllAgents()
        {
            foreach (GameObject agent in activeAgents)
            {
                Destroy(agent);
            }
            
            activeAgents.Clear();
        }
        
        private void ClearAllObstacles()
        {
            foreach (GameObject obstacle in obstacles)
            {
                Destroy(obstacle);
            }
            
            obstacles.Clear();
        }
        
        private void UpdateUI()
        {
            if (algorithmText != null)
                algorithmText.text = "Algorithm: " + currentPathfinder.GetAlgorithmName();
                
            if (agentCountText != null)
                agentCountText.text = "Agents: " + activeAgents.Count;
                
            if (performanceMonitor != null)
            {
                if (fpsText != null)
                    fpsText.text = "FPS: " + performanceMonitor.currentFPS.ToString("F1");
                    
                if (cpuText != null)
                    cpuText.text = "CPU: " + performanceMonitor.currentCPUTime.ToString("F1") + " ms";
                    
                if (memoryText != null)
                    memoryText.text = "Memory: " + performanceMonitor.currentMemoryUsageMB.ToString("F1") + " MB";
            }
        }
        
        #region Public API for UI Buttons
        
        /// <summary>
        /// Start a test with the current settings
        /// </summary>
        public void StartTest()
        {
            if (isTestRunning)
                StopTest();
                
            isTestRunning = true;
            testStartTime = Time.time;
            nextObstacleTime = Time.time + obstacleSpawnInterval;
            
            // Set current pathfinder
            SetCurrentPathfinder();
            
            // Clear any existing agents and obstacles
            ClearAllAgents();
            ClearAllObstacles();
            
            // Spawn static obstacles
            SpawnStaticObstacles();
            
            // Spawn agents based on current count
            for (int i = 0; i < currentAgentCount; i++)
            {
                SpawnAgent();
            }
            
            // Update performance monitor
            performanceMonitor.SetAgentCount(currentAgentCount);
            performanceMonitor.StartRecording();
            
            Debug.Log("Test started: " + currentPathfinder.GetAlgorithmName() + " with " + currentAgentCount + " agents");
        }
        
        /// <summary>
        /// Stop the current test
        /// </summary>
        public void StopTest()
        {
            isTestRunning = false;
            
            // Stop performance monitoring
            performanceMonitor.StopRecording();
            
            Debug.Log("Test stopped: " + currentPathfinder.GetAlgorithmName() + " with " + currentAgentCount + " agents");
        }
        
        /// <summary>
        /// Switch to A* pathfinding
        /// </summary>
        public void SwitchToAStar()
        {
            currentAlgorithm = PathfindingAlgorithm.AStar;
            SetCurrentPathfinder();
            UpdateUI();
        }
        
        /// <summary>
        /// Switch to NavMesh pathfinding
        /// </summary>
        public void SwitchToNavMesh()
        {
            currentAlgorithm = PathfindingAlgorithm.NavMesh;
            SetCurrentPathfinder();
            UpdateUI();
        }
        
        /// <summary>
        /// Set the agent count
        /// </summary>
        public void SetAgentCount(int count)
        {
            currentAgentCount = count;
            UpdateUI();
        }
        
        /// <summary>
        /// Export performance data to CSV
        /// </summary>
        public void ExportData()
        {
            string fileName = "PathfindingPerformanceData.csv";
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filePath))
            {
                // Write header
                writer.WriteLine("TimeStamp,Algorithm,AgentCount,FPS,CPUTime,MemoryMB");
                
                // Write data
                foreach (PerformanceMonitor.PerformanceMetrics metrics in performanceMonitor.recordedMetrics)
                {
                    writer.WriteLine(string.Format("{0},{1},{2},{3},{4},{5}",
                        metrics.timeStamp,
                        metrics.pathfindingMethod,
                        metrics.activeAgentCount,
                        metrics.fps,
                        metrics.cpuUsage,
                        metrics.memoryUsageMB
                    ));
                }
            }
            
            Debug.Log("Performance data exported to: " + filePath);
        }
        
        /// <summary>
        /// Run all configured tests in sequence
        /// </summary>
        public void RunAllTests()
        {
            if (isTestRunning)
                StopTest();
                
            currentTestIndex = 0;
            StartCoroutine(RunTestSequence());
        }
        
        #endregion
        
        private IEnumerator RunTestSequence()
        {
            // Run A* tests first
            currentAlgorithm = PathfindingAlgorithm.AStar;
            SetCurrentPathfinder();
            
            for (int i = 0; i < agentCountsToTest.Length; i++)
            {
                currentAgentCount = agentCountsToTest[i];
                StartTest();
                
                yield return new WaitForSeconds(testDuration);
                
                StopTest();
                yield return new WaitForSeconds(delayBetweenTests);
            }
            
            // Then run NavMesh tests
            currentAlgorithm = PathfindingAlgorithm.NavMesh;
            SetCurrentPathfinder();
            
            for (int i = 0; i < agentCountsToTest.Length; i++)
            {
                currentAgentCount = agentCountsToTest[i];
                StartTest();
                
                yield return new WaitForSeconds(testDuration);
                
                StopTest();
                yield return new WaitForSeconds(delayBetweenTests);
            }
            
            // Export data once all tests are complete
            ExportData();
            
            Debug.Log("All tests completed");
        }
        
        private IEnumerator MoveToNextTest()
        {
            yield return new WaitForSeconds(delayBetweenTests);
            
            currentTestIndex++;
            
            // If we've tested all agent counts with the current algorithm
            if (currentTestIndex >= agentCountsToTest.Length)
            {
                currentTestIndex = 0;
                
                // Switch algorithm
                if (currentAlgorithm == PathfindingAlgorithm.AStar)
                {
                    currentAlgorithm = PathfindingAlgorithm.NavMesh;
                    SetCurrentPathfinder();
                }
                else
                {
                    // Both algorithms have been tested with all agent counts
                    // Export data and end tests
                    ExportData();
                    Debug.Log("All tests completed");
                    yield break;
                }
            }
            
            // Update agent count for the next test
            currentAgentCount = agentCountsToTest[currentTestIndex];
            
            // Start the next test
            StartTest();
        }
    }
} 