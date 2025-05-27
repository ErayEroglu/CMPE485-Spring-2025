using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.IO;
using System;
using UnityEngine.Profiling;
using System.Diagnostics;

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
    
    // CPU tracking variables
    private Process currentProcess;
    private float lastCpuTime = 0f;
    private DateTime lastCpuCheck = DateTime.UtcNow;
    
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
        public float cpuUsagePercent;
        public float renderTime;
        public int navMeshQueries;
        public float navMeshUpdateTime;
        
        // Enhanced profiler data
        public float mainThreadTime;
        public float renderThreadTime;
        public float gpuTime;
        public long gfxMemoryUsage;
        public int drawCalls;
        public int triangles;
        public int vertices;
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
        public float averageCpuUsage;
        public float maxCpuUsage;
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
        
        // Enable Unity Profiler for detailed analysis
        if (enableDetailedProfiling)
        {
            Profiler.enabled = true;
            Profiler.enableBinaryLog = false; // Disable binary log for performance
            UnityEngine.Debug.Log("Unity Profiler enabled for detailed performance analysis");
        }
        
        // Initialize CPU tracking
        try
        {
            currentProcess = Process.GetCurrentProcess();
            lastCpuTime = (float)currentProcess.TotalProcessorTime.TotalMilliseconds;
            lastCpuCheck = DateTime.UtcNow;
        }
        catch (System.Exception e)
        {
         UnityEngine.Debug.LogError("Error getting current process: " + e.Message);
        }
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
        
        UnityEngine.Debug.Log($"Started performance collection for: {testName}");
        
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
        
        UnityEngine.Debug.Log($"Stopped performance collection for: {currentTestName}");
        UnityEngine.Debug.Log($"Average FPS: {result.averageFPS:F2}, Min FPS: {result.minFPS:F2}, Max FPS: {result.maxFPS:F2}");
    }
    
    private void CollectFrameData()
    {
        Profiler.BeginSample("PerformanceMetrics.CollectFrameData");
        
        frameCount++;
        float currentFrameTime = Time.unscaledDeltaTime;
        deltaTimeSum += currentFrameTime;
        
        minFrameTime = Mathf.Min(minFrameTime, currentFrameTime);
        maxFrameTime = Mathf.Max(maxFrameTime, currentFrameTime);
        
        // Collect detailed data at specified sample rate (every X frames)
        if (frameCount % Mathf.Max(1, Mathf.RoundToInt(sampleRate / Time.unscaledDeltaTime)) == 0)
        {
            Profiler.BeginSample("PerformanceMetrics.DetailedDataCollection");
            
            PerformanceData data = new PerformanceData
            {
                timestamp = Time.time,
                fps = 1f / currentFrameTime,
                frameTime = currentFrameTime * 1000f, // Convert to milliseconds
                memoryUsage = GetMemoryUsage(),
                activeAgents = FindObjectsOfType<EnemyMovement>().Length,
                activeObstacles = GameObject.FindGameObjectsWithTag("Obstacle").Length,
                cpuTime = GetCPUTime(),
                cpuUsagePercent = GetCPUUsagePercent(),
                renderTime = GetRenderTime(),
                navMeshQueries = GetNavMeshQueries(),
                navMeshUpdateTime = GetNavMeshUpdateTime(),
                
                // Enhanced profiler data
                mainThreadTime = GetMainThreadTime(),
                renderThreadTime = GetRenderThreadTime(),
                gpuTime = GetGPUTime(),
                gfxMemoryUsage = GetGraphicsMemoryUsage(),
                drawCalls = GetDrawCalls(),
                triangles = GetTriangles(),
                vertices = GetVertices()
            };
            
            currentTestData.Add(data);
            
            Profiler.EndSample(); // DetailedDataCollection
        }
        
        Profiler.EndSample(); // CollectFrameData
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
        
        // Calculate CPU statistics
        float cpuSum = 0f;
        float maxCpu = 0f;
        
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
            
            cpuSum += data.cpuUsagePercent;
            maxCpu = Mathf.Max(maxCpu, data.cpuUsagePercent);
            
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
        result.averageCpuUsage = cpuSum / dataCount;
        result.maxCpuUsage = maxCpu;
        result.testDuration = currentTestData[dataCount - 1].timestamp - currentTestData[0].timestamp;
        
        return result;
    }
    
    public void ExportResults()
    {
        if (allTestResults.Count == 0)
        {
            UnityEngine.Debug.LogWarning("No test results to export!");
            return;
        }
        
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string csvPath = Path.Combine(exportPath, $"NavMesh_Performance_Results_{timestamp}.csv");
        string jsonPath = Path.Combine(exportPath, $"NavMesh_Performance_Results_{timestamp}.json");
        
        // Export CSV summary
        ExportCSVSummary(csvPath);
        
        // Export detailed JSON
        ExportJSONDetailed(jsonPath);
        
        UnityEngine.Debug.Log($"Performance results exported to: {csvPath} and {jsonPath}");
    }
    
    private void ExportCSVSummary(string path)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            // Write header
            writer.WriteLine("TestName,AgentCount,ObstacleCount,UpdateRate,TestDuration,AverageFPS,MinFPS,MaxFPS,AverageFrameTime,MinFrameTime,MaxFrameTime,AverageMemoryMB,PeakMemoryMB,AverageCpuUsage,MaxCpuUsage,AverageMainThreadTime,AverageRenderThreadTime,AverageGPUTime,AverageGfxMemoryMB,AverageDrawCalls,AverageTriangles,AverageVertices");
            
            // Write data
            foreach (var result in allTestResults)
            {
                // Calculate averages for new metrics
                float avgMainThread = 0f, avgRenderThread = 0f, avgGPU = 0f;
                long avgGfxMemory = 0L;
                int avgDrawCalls = 0, avgTriangles = 0, avgVertices = 0;
                
                if (result.rawData != null && result.rawData.Count > 0)
                {
                    foreach (var data in result.rawData)
                    {
                        avgMainThread += data.mainThreadTime;
                        avgRenderThread += data.renderThreadTime;
                        avgGPU += data.gpuTime;
                        avgGfxMemory += data.gfxMemoryUsage;
                        avgDrawCalls += data.drawCalls;
                        avgTriangles += data.triangles;
                        avgVertices += data.vertices;
                    }
                    
                    int count = result.rawData.Count;
                    avgMainThread /= count;
                    avgRenderThread /= count;
                    avgGPU /= count;
                    avgGfxMemory /= count;
                    avgDrawCalls /= count;
                    avgTriangles /= count;
                    avgVertices /= count;
                }
                
                writer.WriteLine($"{result.testName},{result.agentCount},{result.obstacleCount},{result.updateRate:F3},{result.testDuration:F2}," +
                               $"{result.averageFPS:F2},{result.minFPS:F2},{result.maxFPS:F2}," +
                               $"{result.averageFrameTime:F2},{result.minFrameTime:F2},{result.maxFrameTime:F2}," +
                               $"{result.averageMemoryUsage / (1024 * 1024):F2},{result.peakMemoryUsage / (1024 * 1024):F2}," +
                               $"{result.averageCpuUsage:F2},{result.maxCpuUsage:F2}," +
                               $"{avgMainThread:F2},{avgRenderThread:F2},{avgGPU:F2}," +
                               $"{avgGfxMemory / (1024 * 1024):F2},{avgDrawCalls},{avgTriangles},{avgVertices}");
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
    
    private float GetCPUUsagePercent()
    {
        try
        {
            if (currentProcess == null)
                return 0f;
                
            DateTime currentTime = DateTime.UtcNow;
            float currentCpuTime = (float)currentProcess.TotalProcessorTime.TotalMilliseconds;
            
            // Calculate time differences
            float timeDiff = (float)(currentTime - lastCpuCheck).TotalMilliseconds;
            float cpuTimeDiff = currentCpuTime - lastCpuTime;
            
            // Calculate CPU usage percentage
            float cpuUsage = 0f;
            if (timeDiff > 0)
            {
                cpuUsage = (cpuTimeDiff / timeDiff) * 100f / Environment.ProcessorCount;
                cpuUsage = Mathf.Clamp(cpuUsage, 0f, 100f);
            }
            
            // Update for next calculation
            lastCpuTime = currentCpuTime;
            lastCpuCheck = currentTime;
            
            return cpuUsage;
        }
        catch (System.Exception)
        {
            // Fallback to Unity's profiler-based estimation
            return Profiler.GetTotalAllocatedMemoryLong() > 0 ? Time.unscaledDeltaTime * 1000f : 0f;
        }
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
    
    // Enhanced profiler methods using Unity's Profiler API
    private float GetMainThreadTime()
    {
        try
        {
            // Use frame time as approximation for main thread time
            return Time.unscaledDeltaTime * 1000f; // Convert to milliseconds
        }
        catch (System.Exception)
        {
            return 0f;
        }
    }
    
    private float GetRenderThreadTime()
    {
        try
        {
            // Estimate render thread time based on frame time and complexity
            // This is an approximation - actual render thread time would need platform-specific APIs
            float baseRenderTime = Time.unscaledDeltaTime * 0.3f * 1000f; // Assume ~30% of frame time
            int drawCalls = GetDrawCalls();
            return baseRenderTime + (drawCalls * 0.01f); // Add small overhead per draw call
        }
        catch (System.Exception)
        {
            return 0f;
        }
    }
    
    private float GetGPUTime()
    {
        try
        {
            // Estimate GPU time based on rendering complexity
            // This is an approximation - actual GPU timing would need platform-specific APIs
            float baseGpuTime = Time.unscaledDeltaTime * 0.4f * 1000f; // Assume ~40% of frame time
            int triangles = GetTriangles();
            return baseGpuTime + (triangles * 0.0001f); // Add small overhead per triangle
        }
        catch (System.Exception)
        {
            return 0f;
        }
    }
    
    private long GetGraphicsMemoryUsage()
    {
        try
        {
            return Profiler.GetAllocatedMemoryForGraphicsDriver();
        }
        catch (System.Exception)
        {
            // Fallback estimation based on scene complexity
            int agents = FindObjectsOfType<EnemyMovement>().Length;
            int obstacles = GameObject.FindGameObjectsWithTag("Obstacle").Length;
            return (agents + obstacles) * 1024 * 50; // Rough estimate: 50KB per object
        }
    }
    
    private int GetDrawCalls()
    {
        try
        {
            // Estimate draw calls based on active objects
            int agents = FindObjectsOfType<EnemyMovement>().Length;
            int obstacles = GameObject.FindGameObjectsWithTag("Obstacle").Length;
            return agents + obstacles + 10; // Base draw calls + objects
        }
        catch (System.Exception)
        {
            return 0;
        }
    }
    
    private int GetTriangles()
    {
        try
        {
            // Estimate triangles based on primitive objects
            int agents = FindObjectsOfType<EnemyMovement>().Length;
            int obstacles = GameObject.FindGameObjectsWithTag("Obstacle").Length;
            return (agents * 384) + (obstacles * 12); // Capsule ~384 tris, Cube ~12 tris
        }
        catch (System.Exception)
        {
            return 0;
        }
    }
    
    private int GetVertices()
    {
        try
        {
            // Estimate vertices based on primitive objects
            int agents = FindObjectsOfType<EnemyMovement>().Length;
            int obstacles = GameObject.FindGameObjectsWithTag("Obstacle").Length;
            return (agents * 194) + (obstacles * 24); // Capsule ~194 verts, Cube ~24 verts
        }
        catch (System.Exception)
        {
            return 0;
        }
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
    
    public float GetCurrentCPUUsage()
    {
        return GetCPUUsagePercent();
    }
    
    private void OnGUI()
    {
        if (!isCollecting) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 300, 10, 290, 200));
        GUILayout.Label("Performance Metrics", GUI.skin.box);
        GUILayout.Label($"FPS: {GetCurrentFPS():F1}");
        GUILayout.Label($"Frame Time: {Time.unscaledDeltaTime * 1000f:F1}ms");
        GUILayout.Label($"Memory: {GetCurrentMemoryUsage() / (1024 * 1024):F1}MB");
        GUILayout.Label($"CPU Usage: {GetCurrentCPUUsage():F1}%");
        GUILayout.Label($"Main Thread: {GetMainThreadTime():F1}ms");
        GUILayout.Label($"GPU Time: {GetGPUTime():F1}ms");
        GUILayout.Label($"Draw Calls: {GetDrawCalls()}");
        GUILayout.Label($"Agents: {GetActiveAgentCount()}");
        GUILayout.Label($"Samples: {currentTestData.Count}");
        GUILayout.EndArea();
    }
} 