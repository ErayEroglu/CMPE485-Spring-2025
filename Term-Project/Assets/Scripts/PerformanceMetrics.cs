using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.IO;
using System;
using UnityEngine.Profiling;

public class PerformanceMetrics : MonoBehaviour
{
    [Header("Metrics Configuration")]
    public float sampleRate = 0.1f;
    public bool enableDetailedProfiling = true;
    public string exportPath = "PerformanceResults";
    
    private bool isCollecting = false;
    private string currentTestName;
    private List<PerformanceData> currentTestData = new List<PerformanceData>();
    private List<TestResult> allTestResults = new List<TestResult>();
    
    // Performance tracking variables
    private int frameCount = 0;
    private float deltaTimeSum = 0f;
    private float minFrameTime = float.MaxValue;
    private float maxFrameTime = 0f;
    
    [System.Serializable]
    public class PerformanceData
    {
        public float timestamp;
        public float fps;
        public float frameTime;
        public long memoryUsage;
        public int activeAgents;
        public int activeObstacles;
        public float cpuTime;
        public float renderTime;
        public int navMeshQueries;
        public float navMeshUpdateTime;
    }
    
    [System.Serializable]
    public class TestResult
    {
        public string testName;
        public float averageFPS;
        public float minFPS;
        public float maxFPS;
        public float averageFrameTime;
        public float minFrameTime;
        public float maxFrameTime;
        public long averageMemoryUsage;
        public long peakMemoryUsage;
        public int agentCount;
        public int obstacleCount;
        public float updateRate;
        public float testDuration;
        public List<PerformanceData> rawData;
        public DateTime testDateTime;
    }
    
    private void Start()
    {
        // Ensure export directory exists
        if (!Directory.Exists(exportPath))
        {
            Directory.CreateDirectory(exportPath);
        }
        
        // Set target frame rate for consistent testing
        Application.targetFrameRate = -1; // Unlimited for testing
        QualitySettings.vSyncCount = 0;
    }
    
    private void Update()
    {
        if (isCollecting)
        {
            CollectFrameData();
        }
    }
    
    public void StartCollection(string testName)
    {
        currentTestName = testName;
        currentTestData.Clear();
        isCollecting = true;
        
        // Reset frame tracking
        frameCount = 0;
        deltaTimeSum = 0f;
        minFrameTime = float.MaxValue;
        maxFrameTime = 0f;
        
        Debug.Log($"Started performance collection for: {testName}");
        
        if (enableDetailedProfiling)
        {
            StartCoroutine(DetailedProfilingCoroutine());
        }
    }
    
    public void StopCollection()
    {
        if (!isCollecting) return;
        
        isCollecting = false;
        
        // Calculate test results
        TestResult result = CalculateTestResult();
        allTestResults.Add(result);
        
        Debug.Log($"Stopped performance collection for: {currentTestName}");
        Debug.Log($"Average FPS: {result.averageFPS:F2}, Min FPS: {result.minFPS:F2}, Max FPS: {result.maxFPS:F2}");
    }
    
    private void CollectFrameData()
    {
        frameCount++;
        float currentFrameTime = Time.unscaledDeltaTime;
        deltaTimeSum += currentFrameTime;
        
        minFrameTime = Mathf.Min(minFrameTime, currentFrameTime);
        maxFrameTime = Mathf.Max(maxFrameTime, currentFrameTime);
        
        // Collect detailed data at specified sample rate
        if (frameCount % Mathf.RoundToInt(1f / sampleRate / Time.unscaledDeltaTime) == 0)
        {
            PerformanceData data = new PerformanceData
            {
                timestamp = Time.time,
                fps = 1f / currentFrameTime,
                frameTime = currentFrameTime * 1000f, // Convert to milliseconds
                memoryUsage = GetMemoryUsage(),
                activeAgents = FindObjectsOfType<EnemyMovement>().Length,
                activeObstacles = GameObject.FindGameObjectsWithTag("Obstacle").Length,
                cpuTime = GetCPUTime(),
                renderTime = GetRenderTime(),
                navMeshQueries = GetNavMeshQueries(),
                navMeshUpdateTime = GetNavMeshUpdateTime()
            };
            
            currentTestData.Add(data);
        }
    }
    
    private IEnumerator DetailedProfilingCoroutine()
    {
        while (isCollecting)
        {
            // Force garbage collection periodically to get accurate memory readings
            if (frameCount % 300 == 0) // Every ~5 seconds at 60fps
            {
                System.GC.Collect();
            }
            
            yield return new WaitForSeconds(sampleRate);
        }
    }
    
    private TestResult CalculateTestResult()
    {
        if (currentTestData.Count == 0)
        {
            return new TestResult { testName = currentTestName };
        }
        
        TestResult result = new TestResult
        {
            testName = currentTestName,
            testDateTime = DateTime.Now,
            rawData = new List<PerformanceData>(currentTestData)
        };
        
        // Calculate FPS statistics
        float fpsSum = 0f;
        float minFPS = float.MaxValue;
        float maxFPS = 0f;
        
        // Calculate frame time statistics
        float frameTimeSum = 0f;
        float minFrameTimeMs = float.MaxValue;
        float maxFrameTimeMs = 0f;
        
        // Calculate memory statistics
        long memorySum = 0;
        long peakMemory = 0;
        
        foreach (var data in currentTestData)
        {
            fpsSum += data.fps;
            minFPS = Mathf.Min(minFPS, data.fps);
            maxFPS = Mathf.Max(maxFPS, data.fps);
            
            frameTimeSum += data.frameTime;
            minFrameTimeMs = Mathf.Min(minFrameTimeMs, data.frameTime);
            maxFrameTimeMs = Mathf.Max(maxFrameTimeMs, data.frameTime);
            
            memorySum += data.memoryUsage;
            peakMemory = Math.Max(peakMemory, data.memoryUsage);
            
            result.agentCount = data.activeAgents;
            result.obstacleCount = data.activeObstacles;
        }
        
        int dataCount = currentTestData.Count;
        result.averageFPS = fpsSum / dataCount;
        result.minFPS = minFPS;
        result.maxFPS = maxFPS;
        result.averageFrameTime = frameTimeSum / dataCount;
        result.minFrameTime = minFrameTimeMs;
        result.maxFrameTime = maxFrameTimeMs;
        result.averageMemoryUsage = memorySum / dataCount;
        result.peakMemoryUsage = peakMemory;
        result.testDuration = currentTestData[dataCount - 1].timestamp - currentTestData[0].timestamp;
        
        return result;
    }
    
    public void ExportResults()
    {
        if (allTestResults.Count == 0)
        {
            Debug.LogWarning("No test results to export!");
            return;
        }
        
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string csvPath = Path.Combine(exportPath, $"NavMesh_Performance_Results_{timestamp}.csv");
        string jsonPath = Path.Combine(exportPath, $"NavMesh_Performance_Results_{timestamp}.json");
        
        // Export CSV summary
        ExportCSVSummary(csvPath);
        
        // Export detailed JSON
        ExportJSONDetailed(jsonPath);
        
        Debug.Log($"Performance results exported to: {csvPath} and {jsonPath}");
    }
    
    private void ExportCSVSummary(string path)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            // Write header
            writer.WriteLine("TestName,AgentCount,ObstacleCount,UpdateRate,TestDuration,AverageFPS,MinFPS,MaxFPS,AverageFrameTime,MinFrameTime,MaxFrameTime,AverageMemoryMB,PeakMemoryMB");
            
            // Write data
            foreach (var result in allTestResults)
            {
                writer.WriteLine($"{result.testName},{result.agentCount},{result.obstacleCount},{result.updateRate:F3},{result.testDuration:F2}," +
                               $"{result.averageFPS:F2},{result.minFPS:F2},{result.maxFPS:F2}," +
                               $"{result.averageFrameTime:F2},{result.minFrameTime:F2},{result.maxFrameTime:F2}," +
                               $"{result.averageMemoryUsage / (1024 * 1024):F2},{result.peakMemoryUsage / (1024 * 1024):F2}");
            }
        }
    }
    
    private void ExportJSONDetailed(string path)
    {
        string json = JsonUtility.ToJson(new SerializableTestResults { results = allTestResults }, true);
        File.WriteAllText(path, json);
    }
    
    [System.Serializable]
    private class SerializableTestResults
    {
        public List<TestResult> results;
    }
    
    // Helper methods for detailed profiling
    private long GetMemoryUsage()
    {
        // Use the newer method if available, otherwise fall back to the older one
        try
        {
            return Profiler.GetTotalAllocatedMemoryLong();
        }
        catch (System.Exception)
        {
            // Fallback to older method and cast to long
            #pragma warning disable CS0618 // Disable obsolete warning
            return (long)Profiler.GetTotalAllocatedMemory();
            #pragma warning restore CS0618
        }
    }
    
    private float GetCPUTime()
    {
        // This is a simplified CPU time measurement
        // In a real implementation, you might use Unity's Profiler API
        return Time.unscaledDeltaTime * 1000f;
    }
    
    private float GetRenderTime()
    {
        // Simplified render time - would need more sophisticated measurement in production
        return 0f; // Placeholder
    }
    
    private int GetNavMeshQueries()
    {
        // Count active NavMesh agents making queries
        NavMeshAgent[] agents = FindObjectsOfType<NavMeshAgent>();
        int activeQueries = 0;
        
        foreach (var agent in agents)
        {
            if (agent.enabled && agent.isActiveAndEnabled && agent.pathPending)
            {
                activeQueries++;
            }
        }
        
        return activeQueries;
    }
    
    private float GetNavMeshUpdateTime()
    {
        // This would require more sophisticated measurement in a real implementation
        return 0f; // Placeholder
    }
    
    // Public methods for real-time monitoring
    public float GetCurrentFPS()
    {
        return frameCount > 0 ? frameCount / deltaTimeSum : 0f;
    }
    
    public long GetCurrentMemoryUsage()
    {
        return GetMemoryUsage();
    }
    
    public int GetActiveAgentCount()
    {
        return FindObjectsOfType<EnemyMovement>().Length;
    }
    
    private void OnGUI()
    {
        if (!isCollecting) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 150));
        GUILayout.Label("Performance Metrics", GUI.skin.box);
        GUILayout.Label($"FPS: {GetCurrentFPS():F1}");
        GUILayout.Label($"Frame Time: {Time.unscaledDeltaTime * 1000f:F1}ms");
        GUILayout.Label($"Memory: {GetCurrentMemoryUsage() / (1024 * 1024):F1}MB");
        GUILayout.Label($"Agents: {GetActiveAgentCount()}");
        GUILayout.Label($"Samples: {currentTestData.Count}");
        GUILayout.EndArea();
    }
} 