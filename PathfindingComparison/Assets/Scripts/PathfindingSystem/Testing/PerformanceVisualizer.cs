using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PathfindingSystem.Common;

namespace PathfindingSystem.Testing
{
    /// <summary>
    /// Visualizes performance metrics as charts
    /// </summary>
    public class PerformanceVisualizer : MonoBehaviour
    {
        [Header("References")]
        public PerformanceMonitor performanceMonitor;
        
        [Header("UI Elements")]
        public RectTransform chartContainer;
        public GameObject linePrefab;
        public GameObject pointPrefab;
        public Text titleText;
        
        [Header("Chart Settings")]
        public Vector2 chartSize = new Vector2(800f, 400f);
        public float padding = 50f;
        public int maxVisibleDataPoints = 100;
        public Color fpsLineColor = Color.green;
        public Color cpuLineColor = Color.red;
        public Color memoryLineColor = Color.blue;
        
        private List<GameObject> chartElements = new List<GameObject>();
        private bool isVisualizingData = false;
        
        private void Start()
        {
            if (performanceMonitor == null)
            {
                performanceMonitor = FindObjectOfType<PerformanceMonitor>();
                if (performanceMonitor == null)
                {
                    Debug.LogError("PerformanceVisualizer requires a PerformanceMonitor component");
                    enabled = false;
                    return;
                }
            }
            
            // Set up chart container
            if (chartContainer != null)
            {
                chartContainer.sizeDelta = chartSize;
            }
        }
        
        /// <summary>
        /// Draw FPS chart from recorded data
        /// </summary>
        public void DrawFPSChart()
        {
            ClearChart();
            
            if (titleText != null)
                titleText.text = "FPS Chart";
                
            isVisualizingData = true;
            DrawMetricChart(metric => metric.fps, fpsLineColor);
        }
        
        /// <summary>
        /// Draw CPU usage chart from recorded data
        /// </summary>
        public void DrawCPUChart()
        {
            ClearChart();
            
            if (titleText != null)
                titleText.text = "CPU Usage Chart (ms per frame)";
                
            isVisualizingData = true;
            DrawMetricChart(metric => metric.cpuUsage, cpuLineColor);
        }
        
        /// <summary>
        /// Draw memory usage chart from recorded data
        /// </summary>
        public void DrawMemoryChart()
        {
            ClearChart();
            
            if (titleText != null)
                titleText.text = "Memory Usage Chart (MB)";
                
            isVisualizingData = true;
            DrawMetricChart(metric => metric.memoryUsageMB, memoryLineColor);
        }
        
        /// <summary>
        /// Draw comparison between A* and NavMesh for a specific metric and agent count
        /// </summary>
        public void DrawComparisonChart(int agentCount, string metricName)
        {
            ClearChart();
            
            if (titleText != null)
                titleText.text = $"Comparison: {metricName} with {agentCount} Agents";
                
            isVisualizingData = true;
            
            var aStarData = new List<PerformanceMonitor.PerformanceMetrics>();
            var navMeshData = new List<PerformanceMonitor.PerformanceMetrics>();
            
            // Filter data by agent count and algorithm
            foreach (var metric in performanceMonitor.recordedMetrics)
            {
                if (metric.activeAgentCount == agentCount)
                {
                    if (metric.pathfindingMethod == "A*")
                    {
                        aStarData.Add(metric);
                    }
                    else if (metric.pathfindingMethod == "NavMesh")
                    {
                        navMeshData.Add(metric);
                    }
                }
            }
            
            // Draw comparison charts
            if (metricName == "FPS")
            {
                DrawMetricChart(aStarData, metric => metric.fps, fpsLineColor, "A*");
                DrawMetricChart(navMeshData, metric => metric.fps, Color.cyan, "NavMesh");
            }
            else if (metricName == "CPU")
            {
                DrawMetricChart(aStarData, metric => metric.cpuUsage, cpuLineColor, "A*");
                DrawMetricChart(navMeshData, metric => metric.cpuUsage, Color.magenta, "NavMesh");
            }
            else if (metricName == "Memory")
            {
                DrawMetricChart(aStarData, metric => metric.memoryUsageMB, memoryLineColor, "A*");
                DrawMetricChart(navMeshData, metric => metric.memoryUsageMB, Color.yellow, "NavMesh");
            }
        }
        
        /// <summary>
        /// Draw chart for a specific metric from all recorded data
        /// </summary>
        private void DrawMetricChart(System.Func<PerformanceMonitor.PerformanceMetrics, float> metricSelector, Color lineColor)
        {
            DrawMetricChart(performanceMonitor.recordedMetrics, metricSelector, lineColor);
        }
        
        /// <summary>
        /// Draw chart for a specific metric from provided data
        /// </summary>
        private void DrawMetricChart(List<PerformanceMonitor.PerformanceMetrics> data, System.Func<PerformanceMonitor.PerformanceMetrics, float> metricSelector, Color lineColor, string label = "")
        {
            if (data.Count == 0 || chartContainer == null)
                return;
                
            // Calculate min/max values for scaling
            float minValue = float.MaxValue;
            float maxValue = float.MinValue;
            
            foreach (var metric in data)
            {
                float value = metricSelector(metric);
                if (value < minValue) minValue = value;
                if (value > maxValue) maxValue = value;
            }
            
            // Ensure range is valid
            if (minValue == maxValue)
            {
                maxValue = minValue + 1;
            }
            
            // Buffer for better visualization
            float range = maxValue - minValue;
            minValue -= range * 0.1f;
            maxValue += range * 0.1f;
            
            // Calculate chart area
            float chartWidth = chartSize.x - (padding * 2);
            float chartHeight = chartSize.y - (padding * 2);
            
            // Draw data points and lines
            Vector2 previousPoint = Vector2.zero;
            bool isFirstPoint = true;
            
            // Determine visible range
            int startIndex = data.Count <= maxVisibleDataPoints ? 0 : data.Count - maxVisibleDataPoints;
            
            for (int i = startIndex; i < data.Count; i++)
            {
                float normalizedX = (float)(i - startIndex) / (data.Count - startIndex <= 1 ? 1 : data.Count - startIndex - 1);
                float normalizedY = Mathf.InverseLerp(minValue, maxValue, metricSelector(data[i]));
                
                // Calculate point position in chart space
                Vector2 pointPosition = new Vector2(
                    padding + (normalizedX * chartWidth),
                    padding + (normalizedY * chartHeight)
                );
                
                // Create point marker
                GameObject pointObj = Instantiate(pointPrefab, chartContainer);
                pointObj.GetComponent<RectTransform>().anchoredPosition = pointPosition;
                pointObj.GetComponent<Image>().color = lineColor;
                chartElements.Add(pointObj);
                
                // Draw line to previous point (except for first point)
                if (!isFirstPoint)
                {
                    GameObject lineObj = Instantiate(linePrefab, chartContainer);
                    RectTransform lineRect = lineObj.GetComponent<RectTransform>();
                    
                    // Calculate line position and size
                    Vector2 direction = pointPosition - previousPoint;
                    float distance = direction.magnitude;
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    
                    lineRect.sizeDelta = new Vector2(distance, 2f);
                    lineRect.anchoredPosition = previousPoint + direction * 0.5f;
                    lineRect.localEulerAngles = new Vector3(0, 0, angle);
                    lineObj.GetComponent<Image>().color = lineColor;
                    
                    chartElements.Add(lineObj);
                }
                
                previousPoint = pointPosition;
                isFirstPoint = false;
            }
            
            // Add label if provided
            if (!string.IsNullOrEmpty(label))
            {
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(chartContainer, false);
                Text labelText = labelObj.AddComponent<Text>();
                labelText.text = label;
                labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                labelText.fontSize = 16;
                labelText.color = lineColor;
                labelText.alignment = TextAnchor.MiddleLeft;
                
                RectTransform labelRect = labelObj.GetComponent<RectTransform>();
                labelRect.anchoredPosition = new Vector2(padding, chartHeight - 20 * chartElements.Count / 5);
                labelRect.sizeDelta = new Vector2(200, 20);
                
                chartElements.Add(labelObj);
            }
        }
        
        /// <summary>
        /// Clear all chart elements
        /// </summary>
        public void ClearChart()
        {
            foreach (GameObject element in chartElements)
            {
                Destroy(element);
            }
            
            chartElements.Clear();
            isVisualizingData = false;
        }
    }
} 