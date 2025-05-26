using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using System.Linq;
using UnityEngine.AI;

namespace PathfindingSystem.Common
{
    /// <summary>
    /// Monitors and records performance metrics
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        [System.Serializable]
        public class PerformanceMetrics
        {
            public float fps;
            public float cpuUsage; // ms per frame
            public float memoryUsageMB;
            public int activeAgentCount;
            public string pathfindingMethod;
            public float timeStamp;
            
            public PerformanceMetrics(float fps, float cpuUsage, float memoryUsageMB, int activeAgentCount, string pathfindingMethod)
            {
                this.fps = fps;
                this.cpuUsage = cpuUsage;
                this.memoryUsageMB = memoryUsageMB;
                this.activeAgentCount = activeAgentCount;
                this.pathfindingMethod = pathfindingMethod;
                this.timeStamp = Time.time;
            }
        }
        
        [Header("Monitoring Settings")]
        public bool isRecording = false;
        public float sampleInterval = 1f;
        public int maxSamples = 1000;
        
        [Header("Current Metrics")]
        public float currentFPS;
        public float currentCPUTime;
        public float currentMemoryUsageMB;
        public int currentAgentCount;
        public string currentPathfindingMethod;
        
        // List to store performance data over time
        public List<PerformanceMetrics> recordedMetrics = new List<PerformanceMetrics>();
        
        private float[] fpsBuffer;
        private int fpsBufferIndex;
        private float fpsNextSampleTime;
        private float recordNextSampleTime;
        
        private void Start()
        {
            fpsBuffer = new float[60];
            fpsBufferIndex = 0;
            fpsNextSampleTime = Time.unscaledTime;
            recordNextSampleTime = Time.time;
        }
        
        private void Update()
        {
            // Update FPS calculation
            if (Time.unscaledTime >= fpsNextSampleTime)
            {
                fpsBuffer[fpsBufferIndex++] = 1f / Time.unscaledDeltaTime;
                if (fpsBufferIndex >= fpsBuffer.Length)
                {
                    fpsBufferIndex = 0;
                }
                
                fpsNextSampleTime += 0.1f; // Sample every 0.1 seconds
                
                // Calculate average FPS
                currentFPS = fpsBuffer.Average();
            }
            
            // Update CPU usage
            currentCPUTime = Time.unscaledDeltaTime * 1000f; // ms per frame
            
            // Update memory usage
            currentMemoryUsageMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            
            // Record metrics if enabled
            if (isRecording && Time.time >= recordNextSampleTime)
            {
                RecordCurrentMetrics();
                recordNextSampleTime = Time.time + sampleInterval;
            }
        }
        
        /// <summary>
        /// Record the current performance metrics
        /// </summary>
        public void RecordCurrentMetrics()
        {
            PerformanceMetrics metrics = new PerformanceMetrics(
                currentFPS,
                currentCPUTime,
                currentMemoryUsageMB,
                currentAgentCount,
                currentPathfindingMethod
            );
            
            recordedMetrics.Add(metrics);
            
            // Limit the number of samples
            if (recordedMetrics.Count > maxSamples)
            {
                recordedMetrics.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Start recording performance metrics
        /// </summary>
        public void StartRecording()
        {
            isRecording = true;
        }
        
        /// <summary>
        /// Stop recording performance metrics
        /// </summary>
        public void StopRecording()
        {
            isRecording = false;
        }
        
        /// <summary>
        /// Clear recorded performance metrics
        /// </summary>
        public void ClearRecordings()
        {
            recordedMetrics.Clear();
        }
        
        /// <summary>
        /// Set the current pathfinding method
        /// </summary>
        public void SetPathfindingMethod(string methodName)
        {
            currentPathfindingMethod = methodName;
        }
        
        /// <summary>
        /// Set the current agent count
        /// </summary>
        public void SetAgentCount(int count)
        {
            currentAgentCount = count;
        }
    }
} 