using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System;

public class DataAnalyzer : MonoBehaviour
{
    [Header("Analysis Configuration")]
    public string dataDirectory = "PerformanceResults";
    public bool autoAnalyzeOnStart = false;
    public bool generateReports = true;
    
    [Header("Analysis Thresholds")]
    public float targetFPS = 60f;
    public float acceptableFPS = 30f;
    public long maxMemoryMB = 1024; // 1GB
    
    private List<PerformanceMetrics.TestResult> loadedResults = new List<PerformanceMetrics.TestResult>();
    
    private void Start()
    {
        if (autoAnalyzeOnStart)
        {
            AnalyzeLatestResults();
        }
    }
    
    [ContextMenu("Analyze Latest Results")]
    public void AnalyzeLatestResults()
    {
        LoadLatestResults();
        if (loadedResults.Count > 0)
        {
            PerformAnalysis();
            if (generateReports)
            {
                GenerateReports();
            }
        }
        else
        {
            Debug.LogWarning("No performance results found to analyze.");
        }
    }
    
    private void LoadLatestResults()
    {
        loadedResults.Clear();
        
        if (!Directory.Exists(dataDirectory))
        {
            Debug.LogWarning($"Data directory not found: {dataDirectory}");
            return;
        }
        
        // Find the latest JSON file
        string[] jsonFiles = Directory.GetFiles(dataDirectory, "*.json");
        if (jsonFiles.Length == 0)
        {
            Debug.LogWarning("No JSON result files found.");
            return;
        }
        
        // Get the most recent file
        string latestFile = jsonFiles.OrderByDescending(f => File.GetCreationTime(f)).First();
        
        try
        {
            string jsonContent = File.ReadAllText(latestFile);
            var wrapper = JsonUtility.FromJson<SerializableTestResults>(jsonContent);
            loadedResults = wrapper.results;
            
            Debug.Log($"Loaded {loadedResults.Count} test results from {Path.GetFileName(latestFile)}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load results: {e.Message}");
        }
    }
    
    private void PerformAnalysis()
    {
        Debug.Log("=== PERFORMANCE ANALYSIS REPORT ===");
        
        AnalyzeAgentScaling();
        AnalyzeUpdateRateImpact();
        AnalyzeObstacleImpact();
        AnalyzeOverallPerformance();
        
        Debug.Log("=== END OF ANALYSIS ===");
    }
    
    private void AnalyzeAgentScaling()
    {
        var agentTests = loadedResults.Where(r => r.testName.Contains("AgentScaling")).OrderBy(r => r.agentCount).ToList();
        
        if (agentTests.Count == 0) return;
        
        Debug.Log("\n--- AGENT SCALING ANALYSIS ---");
        
        // Find performance breaking points
        var acceptableTests = agentTests.Where(r => r.averageFPS >= acceptableFPS).ToList();
        var targetTests = agentTests.Where(r => r.averageFPS >= targetFPS).ToList();
        
        int maxAcceptableAgents = acceptableTests.Count > 0 ? acceptableTests.Max(r => r.agentCount) : 0;
        int maxTargetAgents = targetTests.Count > 0 ? targetTests.Max(r => r.agentCount) : 0;
        
        Debug.Log($"Maximum agents for {targetFPS} FPS: {maxTargetAgents}");
        Debug.Log($"Maximum agents for {acceptableFPS} FPS: {maxAcceptableAgents}");
        
        // Calculate performance degradation rate
        if (agentTests.Count >= 2)
        {
            float fpsPerAgent = CalculateFPSPerAgent(agentTests);
            Debug.Log($"Average FPS loss per agent: {fpsPerAgent:F3}");
        }
        
        // Memory scaling
        if (agentTests.Count > 0)
        {
            var memoryPerAgent = CalculateMemoryPerAgent(agentTests);
            Debug.Log($"Average memory per agent: {memoryPerAgent:F2} MB");
        }
    }
    
    private void AnalyzeUpdateRateImpact()
    {
        var updateTests = loadedResults.Where(r => r.testName.Contains("UpdateRate")).OrderBy(r => r.updateRate).ToList();
        
        if (updateTests.Count == 0) return;
        
        Debug.Log("\n--- UPDATE RATE ANALYSIS ---");
        
        foreach (var test in updateTests)
        {
            Debug.Log($"Update Rate {test.updateRate:F3}s: {test.averageFPS:F1} FPS (Min: {test.minFPS:F1}, Max: {test.maxFPS:F1})");
        }
        
        // Find optimal update rate
        var optimalTest = updateTests.Where(r => r.averageFPS >= targetFPS).OrderBy(r => r.updateRate).FirstOrDefault();
        if (optimalTest != null)
        {
            Debug.Log($"Recommended update rate for {targetFPS} FPS: {optimalTest.updateRate:F3}s");
        }
    }
    
    private void AnalyzeObstacleImpact()
    {
        var obstacleTests = loadedResults.Where(r => r.testName.Contains("Obstacles")).OrderBy(r => r.obstacleCount).ToList();
        
        if (obstacleTests.Count == 0) return;
        
        Debug.Log("\n--- OBSTACLE IMPACT ANALYSIS ---");
        
        var baselineTest = obstacleTests.FirstOrDefault(r => r.obstacleCount == 0);
        if (baselineTest != null)
        {
            Debug.Log($"Baseline (0 obstacles): {baselineTest.averageFPS:F1} FPS");
            
            foreach (var test in obstacleTests.Where(r => r.obstacleCount > 0))
            {
                float fpsLoss = baselineTest.averageFPS - test.averageFPS;
                float percentLoss = (fpsLoss / baselineTest.averageFPS) * 100f;
                Debug.Log($"{test.obstacleCount} obstacles: {test.averageFPS:F1} FPS (-{fpsLoss:F1} FPS, -{percentLoss:F1}%)");
            }
        }
    }
    
    private void AnalyzeOverallPerformance()
    {
        Debug.Log("\n--- OVERALL PERFORMANCE SUMMARY ---");
        
        var allTests = loadedResults;
        if (allTests.Count == 0) return;
        
        float avgFPS = allTests.Average(r => r.averageFPS);
        float minFPS = allTests.Min(r => r.minFPS);
        float maxFPS = allTests.Max(r => r.maxFPS);
        
        long avgMemory = (long)allTests.Average(r => r.averageMemoryUsage);
        long peakMemory = allTests.Max(r => r.peakMemoryUsage);
        
        Debug.Log($"Overall Average FPS: {avgFPS:F1}");
        Debug.Log($"Overall Min FPS: {minFPS:F1}");
        Debug.Log($"Overall Max FPS: {maxFPS:F1}");
        Debug.Log($"Average Memory Usage: {avgMemory / (1024 * 1024):F1} MB");
        Debug.Log($"Peak Memory Usage: {peakMemory / (1024 * 1024):F1} MB");
        
        // Performance warnings
        var poorPerformanceTests = allTests.Where(r => r.averageFPS < acceptableFPS).ToList();
        if (poorPerformanceTests.Count > 0)
        {
            Debug.LogWarning($"⚠️ {poorPerformanceTests.Count} tests had poor performance (< {acceptableFPS} FPS)");
        }
        
        var highMemoryTests = allTests.Where(r => r.peakMemoryUsage > maxMemoryMB * 1024 * 1024).ToList();
        if (highMemoryTests.Count > 0)
        {
            Debug.LogWarning($"⚠️ {highMemoryTests.Count} tests exceeded memory threshold ({maxMemoryMB} MB)");
        }
    }
    
    private float CalculateFPSPerAgent(List<PerformanceMetrics.TestResult> tests)
    {
        if (tests.Count < 2) return 0f;
        
        float totalFPSChange = 0f;
        int totalAgentChange = 0;
        
        for (int i = 1; i < tests.Count; i++)
        {
            float fpsChange = tests[i-1].averageFPS - tests[i].averageFPS;
            int agentChange = tests[i].agentCount - tests[i-1].agentCount;
            
            if (agentChange > 0)
            {
                totalFPSChange += fpsChange;
                totalAgentChange += agentChange;
            }
        }
        
        return totalAgentChange > 0 ? totalFPSChange / totalAgentChange : 0f;
    }
    
    private float CalculateMemoryPerAgent(List<PerformanceMetrics.TestResult> tests)
    {
        if (tests.Count < 2) return 0f;
        
        // Use linear regression to estimate memory per agent
        var test1 = tests.First();
        var test2 = tests.Last();
        
        long memoryDiff = test2.averageMemoryUsage - test1.averageMemoryUsage;
        int agentDiff = test2.agentCount - test1.agentCount;
        
        return agentDiff > 0 ? (memoryDiff / (1024f * 1024f)) / agentDiff : 0f;
    }
    
    private void GenerateReports()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string reportPath = Path.Combine(dataDirectory, $"Analysis_Report_{timestamp}.txt");
        
        using (StreamWriter writer = new StreamWriter(reportPath))
        {
            writer.WriteLine("NAVMESH PERFORMANCE ANALYSIS REPORT");
            writer.WriteLine($"Generated: {DateTime.Now}");
            writer.WriteLine($"Total Tests Analyzed: {loadedResults.Count}");
            writer.WriteLine(new string('=', 50));
            
            WriteAgentScalingReport(writer);
            WriteUpdateRateReport(writer);
            WriteObstacleReport(writer);
            WriteRecommendations(writer);
        }
        
        Debug.Log($"Analysis report saved to: {reportPath}");
    }
    
    private void WriteAgentScalingReport(StreamWriter writer)
    {
        var agentTests = loadedResults.Where(r => r.testName.Contains("AgentScaling")).OrderBy(r => r.agentCount).ToList();
        
        writer.WriteLine("\nAGENT SCALING RESULTS:");
        writer.WriteLine("Agent Count | Avg FPS | Min FPS | Max FPS | Memory (MB)");
        writer.WriteLine(new string('-', 60));
        
        foreach (var test in agentTests)
        {
            writer.WriteLine($"{test.agentCount,11} | {test.averageFPS,7:F1} | {test.minFPS,7:F1} | {test.maxFPS,7:F1} | {test.averageMemoryUsage / (1024 * 1024),10:F1}");
        }
    }
    
    private void WriteUpdateRateReport(StreamWriter writer)
    {
        var updateTests = loadedResults.Where(r => r.testName.Contains("UpdateRate")).OrderBy(r => r.updateRate).ToList();
        
        writer.WriteLine("\nUPDATE RATE RESULTS:");
        writer.WriteLine("Update Rate | Avg FPS | Min FPS | Max FPS");
        writer.WriteLine(new string('-', 40));
        
        foreach (var test in updateTests)
        {
            writer.WriteLine($"{test.updateRate,11:F3} | {test.averageFPS,7:F1} | {test.minFPS,7:F1} | {test.maxFPS,7:F1}");
        }
    }
    
    private void WriteObstacleReport(StreamWriter writer)
    {
        var obstacleTests = loadedResults.Where(r => r.testName.Contains("Obstacles")).OrderBy(r => r.obstacleCount).ToList();
        
        writer.WriteLine("\nOBSTACLE IMPACT RESULTS:");
        writer.WriteLine("Obstacles | Avg FPS | Min FPS | Max FPS");
        writer.WriteLine(new string('-', 40));
        
        foreach (var test in obstacleTests)
        {
            writer.WriteLine($"{test.obstacleCount,9} | {test.averageFPS,7:F1} | {test.minFPS,7:F1} | {test.maxFPS,7:F1}");
        }
    }
    
    private void WriteRecommendations(StreamWriter writer)
    {
        writer.WriteLine("\nRECOMMENDATIONS:");
        writer.WriteLine(new string('-', 20));
        
        // Agent count recommendations
        var agentTests = loadedResults.Where(r => r.testName.Contains("AgentScaling")).OrderBy(r => r.agentCount).ToList();
        var goodAgentTests = agentTests.Where(r => r.averageFPS >= targetFPS).ToList();
        if (goodAgentTests.Count > 0)
        {
            int maxGoodAgents = goodAgentTests.Max(r => r.agentCount);
            writer.WriteLine($"• Maximum recommended agents for {targetFPS} FPS: {maxGoodAgents}");
        }
        
        // Update rate recommendations
        var updateTests = loadedResults.Where(r => r.testName.Contains("UpdateRate")).OrderBy(r => r.updateRate).ToList();
        var optimalUpdate = updateTests.Where(r => r.averageFPS >= targetFPS).OrderBy(r => r.updateRate).FirstOrDefault();
        if (optimalUpdate != null)
        {
            writer.WriteLine($"• Recommended update rate: {optimalUpdate.updateRate:F3}s");
        }
        
        // General recommendations
        writer.WriteLine("• Enable agent optimizations for large crowds");
        writer.WriteLine("• Use low-quality obstacle avoidance for better performance");
        writer.WriteLine("• Consider LOD systems for distant agents");
        writer.WriteLine("• Monitor memory usage with large agent counts");
    }
    
    [System.Serializable]
    private class SerializableTestResults
    {
        public List<PerformanceMetrics.TestResult> results;
    }
    
    // Public methods for external analysis
    public List<PerformanceMetrics.TestResult> GetLoadedResults()
    {
        return new List<PerformanceMetrics.TestResult>(loadedResults);
    }
    
    public void AnalyzeCustomResults(List<PerformanceMetrics.TestResult> results)
    {
        loadedResults = results;
        PerformAnalysis();
    }
} 