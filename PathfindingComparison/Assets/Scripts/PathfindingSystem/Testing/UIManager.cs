using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PathfindingSystem.Testing
{
    /// <summary>
    /// Manages the UI for the pathfinding comparison tests
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("References")]
        public TestManager testManager;
        public PerformanceVisualizer performanceVisualizer;
        
        [Header("Panels")]
        public GameObject mainPanel;
        public GameObject testPanel;
        public GameObject resultsPanel;
        
        [Header("Test Controls")]
        public TMP_Dropdown algorithmDropdown;
        public TMP_Dropdown agentCountDropdown;
        public Button startButton;
        public Button stopButton;
        public Button runAllButton;
        
        [Header("Results Controls")]
        public TMP_Dropdown metricDropdown;
        public TMP_Dropdown comparisonAgentCountDropdown;
        public Button fpsChartButton;
        public Button cpuChartButton;
        public Button memoryChartButton;
        public Button comparisonButton;
        public Button exportButton;
        
        [Header("Navigation")]
        public Button testPanelButton;
        public Button resultsPanelButton;
        
        private void Start()
        {
            // Find references if not assigned
            if (testManager == null)
            {
                testManager = FindObjectOfType<TestManager>();
            }
            
            if (performanceVisualizer == null)
            {
                performanceVisualizer = FindObjectOfType<PerformanceVisualizer>();
            }
            
            // Set up UI elements
            InitializeUI();
            
            // Show main panel
            ShowPanel(mainPanel);
        }
        
        private void InitializeUI()
        {
            // Set up algorithm dropdown
            if (algorithmDropdown != null)
            {
                algorithmDropdown.ClearOptions();
                algorithmDropdown.AddOptions(new List<string> { "A*", "NavMesh" });
                algorithmDropdown.onValueChanged.AddListener(OnAlgorithmChanged);
            }
            
            // Set up agent count dropdown
            if (agentCountDropdown != null)
            {
                agentCountDropdown.ClearOptions();
                List<string> agentCounts = new List<string>();
                
                foreach (int count in testManager.agentCountsToTest)
                {
                    agentCounts.Add(count.ToString());
                }
                
                agentCountDropdown.AddOptions(agentCounts);
                agentCountDropdown.onValueChanged.AddListener(OnAgentCountChanged);
            }
            
            // Set up comparison agent count dropdown
            if (comparisonAgentCountDropdown != null)
            {
                comparisonAgentCountDropdown.ClearOptions();
                List<string> agentCounts = new List<string>();
                
                foreach (int count in testManager.agentCountsToTest)
                {
                    agentCounts.Add(count.ToString());
                }
                
                comparisonAgentCountDropdown.AddOptions(agentCounts);
            }
            
            // Set up metric dropdown
            if (metricDropdown != null)
            {
                metricDropdown.ClearOptions();
                metricDropdown.AddOptions(new List<string> { "FPS", "CPU", "Memory" });
            }
            
            // Set up buttons
            if (startButton != null)
                startButton.onClick.AddListener(OnStartTest);
                
            if (stopButton != null)
                stopButton.onClick.AddListener(OnStopTest);
                
            if (runAllButton != null)
                runAllButton.onClick.AddListener(OnRunAllTests);
                
            if (fpsChartButton != null)
                fpsChartButton.onClick.AddListener(OnShowFPSChart);
                
            if (cpuChartButton != null)
                cpuChartButton.onClick.AddListener(OnShowCPUChart);
                
            if (memoryChartButton != null)
                memoryChartButton.onClick.AddListener(OnShowMemoryChart);
                
            if (comparisonButton != null)
                comparisonButton.onClick.AddListener(OnShowComparisonChart);
                
            if (exportButton != null)
                exportButton.onClick.AddListener(OnExportData);
                
            if (testPanelButton != null)
                testPanelButton.onClick.AddListener(() => ShowPanel(testPanel));
                
            if (resultsPanelButton != null)
                resultsPanelButton.onClick.AddListener(() => ShowPanel(resultsPanel));
        }
        
        private void ShowPanel(GameObject panel)
        {
            if (mainPanel != null)
                mainPanel.SetActive(panel == mainPanel);
                
            if (testPanel != null)
                testPanel.SetActive(panel == testPanel);
                
            if (resultsPanel != null)
                resultsPanel.SetActive(panel == resultsPanel);
        }
        
        #region Button Event Handlers
        
        private void OnAlgorithmChanged(int index)
        {
            if (testManager != null)
            {
                testManager.currentAlgorithm = (TestManager.PathfindingAlgorithm)index;
            }
        }
        
        private void OnAgentCountChanged(int index)
        {
            if (testManager != null && index < testManager.agentCountsToTest.Length)
            {
                testManager.currentAgentCount = testManager.agentCountsToTest[index];
            }
        }
        
        private void OnStartTest()
        {
            if (testManager != null)
            {
                testManager.StartTest();
            }
        }
        
        private void OnStopTest()
        {
            if (testManager != null)
            {
                testManager.StopTest();
            }
        }
        
        private void OnRunAllTests()
        {
            if (testManager != null)
            {
                testManager.RunAllTests();
            }
        }
        
        private void OnShowFPSChart()
        {
            if (performanceVisualizer != null)
            {
                performanceVisualizer.DrawFPSChart();
            }
        }
        
        private void OnShowCPUChart()
        {
            if (performanceVisualizer != null)
            {
                performanceVisualizer.DrawCPUChart();
            }
        }
        
        private void OnShowMemoryChart()
        {
            if (performanceVisualizer != null)
            {
                performanceVisualizer.DrawMemoryChart();
            }
        }
        
        private void OnShowComparisonChart()
        {
            if (performanceVisualizer != null && metricDropdown != null && comparisonAgentCountDropdown != null)
            {
                string metric = metricDropdown.options[metricDropdown.value].text;
                int agentIndex = comparisonAgentCountDropdown.value;
                
                if (agentIndex < testManager.agentCountsToTest.Length)
                {
                    int agentCount = testManager.agentCountsToTest[agentIndex];
                    performanceVisualizer.DrawComparisonChart(agentCount, metric);
                }
            }
        }
        
        private void OnExportData()
        {
            if (testManager != null)
            {
                testManager.ExportData();
            }
        }
        
        #endregion
    }
} 