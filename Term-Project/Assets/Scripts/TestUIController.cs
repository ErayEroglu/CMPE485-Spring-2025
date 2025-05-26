using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TestUIController : MonoBehaviour
{
    [Header("UI References")]
    public Canvas testCanvas;
    public Button startTestsButton;
    public Button stopTestsButton;
    public Button clearAgentsButton;
    public Button exportResultsButton;
    
    [Header("Manual Test Controls")]
    public Slider agentCountSlider;
    public TextMeshProUGUI agentCountText;
    public Slider updateRateSlider;
    public TextMeshProUGUI updateRateText;
    public Slider obstacleCountSlider;
    public TextMeshProUGUI obstacleCountText;
    
    [Header("Test Buttons")]
    public Button testAgentScalingButton;
    public Button testUpdateRateButton;
    public Button testObstacleButton;
    
    [Header("Status Display")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI fpsText;
    public TextMeshProUGUI memoryText;
    public TextMeshProUGUI agentText;
    public TextMeshProUGUI obstacleText;
    
    [Header("Progress")]
    public Slider progressSlider;
    public TextMeshProUGUI progressText;
    
    private PerformanceTestManager testManager;
    private PerformanceMetrics metrics;
    
    private void Start()
    {
        testManager = FindObjectOfType<PerformanceTestManager>();
        metrics = FindObjectOfType<PerformanceMetrics>();
        
        if (testManager == null)
        {
            Debug.LogError("PerformanceTestManager not found!");
            return;
        }
        
        SetupUI();
        SetupEventListeners();
    }
    
    private void SetupUI()
    {
        // Initialize sliders
        if (agentCountSlider != null)
        {
            agentCountSlider.minValue = 10;
            agentCountSlider.maxValue = 1000;
            agentCountSlider.value = 100;
            agentCountSlider.wholeNumbers = true;
        }
        
        if (updateRateSlider != null)
        {
            updateRateSlider.minValue = 0.01f;
            updateRateSlider.maxValue = 0.5f;
            updateRateSlider.value = 0.1f;
        }
        
        if (obstacleCountSlider != null)
        {
            obstacleCountSlider.minValue = 0;
            obstacleCountSlider.maxValue = 200;
            obstacleCountSlider.value = 10;
            obstacleCountSlider.wholeNumbers = true;
        }
        
        // Initialize progress
        if (progressSlider != null)
        {
            progressSlider.value = 0;
        }
        
        UpdateSliderTexts();
    }
    
    private void SetupEventListeners()
    {
        // Main test controls
        if (startTestsButton != null)
            startTestsButton.onClick.AddListener(StartAutomaticTests);
        
        if (stopTestsButton != null)
            stopTestsButton.onClick.AddListener(StopTests);
        
        if (clearAgentsButton != null)
            clearAgentsButton.onClick.AddListener(ClearAllAgents);
        
        if (exportResultsButton != null)
            exportResultsButton.onClick.AddListener(ExportResults);
        
        // Manual test buttons
        if (testAgentScalingButton != null)
            testAgentScalingButton.onClick.AddListener(TestAgentScaling);
        
        if (testUpdateRateButton != null)
            testUpdateRateButton.onClick.AddListener(TestUpdateRate);
        
        if (testObstacleButton != null)
            testObstacleButton.onClick.AddListener(TestObstacles);
        
        // Slider listeners
        if (agentCountSlider != null)
            agentCountSlider.onValueChanged.AddListener(OnAgentCountChanged);
        
        if (updateRateSlider != null)
            updateRateSlider.onValueChanged.AddListener(OnUpdateRateChanged);
        
        if (obstacleCountSlider != null)
            obstacleCountSlider.onValueChanged.AddListener(OnObstacleCountChanged);
    }
    
    private void Update()
    {
        UpdateStatusDisplay();
        UpdateProgress();
    }
    
    private void UpdateStatusDisplay()
    {
        if (testManager == null) return;
        
        // Update status text
        if (statusText != null)
        {
            statusText.text = $"Status: {testManager.currentState}";
        }
        
        // Update performance metrics
        if (metrics != null)
        {
            if (fpsText != null)
                fpsText.text = $"FPS: {metrics.GetCurrentFPS():F1}";
            
            if (memoryText != null)
                memoryText.text = $"Memory: {metrics.GetCurrentMemoryUsage() / (1024 * 1024):F1} MB";
        }
        
        // Update agent and obstacle counts
        if (agentText != null)
            agentText.text = $"Agents: {testManager.activeAgents.Count}";
        
        if (obstacleText != null)
            obstacleText.text = $"Obstacles: {testManager.activeObstacles.Count}";
    }
    
    private void UpdateProgress()
    {
        if (testManager == null || progressSlider == null) return;
        
        if (testManager.currentState == PerformanceTestManager.TestState.Running)
        {
            float progress = (float)testManager.currentTestIndex / testManager.testConfigurations.Count;
            progressSlider.value = progress;
            
            if (progressText != null)
            {
                progressText.text = $"Test {testManager.currentTestIndex + 1}/{testManager.testConfigurations.Count}";
            }
        }
        else
        {
            progressSlider.value = testManager.currentState == PerformanceTestManager.TestState.Completed ? 1f : 0f;
            
            if (progressText != null)
            {
                progressText.text = testManager.currentState == PerformanceTestManager.TestState.Completed ? "Completed" : "Ready";
            }
        }
    }
    
    private void UpdateSliderTexts()
    {
        if (agentCountText != null && agentCountSlider != null)
            agentCountText.text = $"Agents: {agentCountSlider.value:F0}";
        
        if (updateRateText != null && updateRateSlider != null)
            updateRateText.text = $"Update Rate: {updateRateSlider.value:F3}s";
        
        if (obstacleCountText != null && obstacleCountSlider != null)
            obstacleCountText.text = $"Obstacles: {obstacleCountSlider.value:F0}";
    }
    
    // Event handlers
    private void StartAutomaticTests()
    {
        if (testManager != null)
        {
            StartCoroutine(testManager.RunAllTests());
        }
    }
    
    private void StopTests()
    {
        if (testManager != null)
        {
            StopAllCoroutines();
            testManager.StopAllCoroutines();
        }
    }
    
    private void ClearAllAgents()
    {
        if (testManager != null)
        {
            testManager.ClearAllAgents();
            testManager.ClearAllObstacles();
        }
    }
    
    private void ExportResults()
    {
        if (metrics != null)
        {
            metrics.ExportResults();
        }
    }
    
    private void TestAgentScaling()
    {
        if (testManager != null && agentCountSlider != null)
        {
            int agentCount = Mathf.RoundToInt(agentCountSlider.value);
            testManager.RunAgentScalingTest(agentCount);
        }
    }
    
    private void TestUpdateRate()
    {
        if (testManager != null && updateRateSlider != null)
        {
            float updateRate = updateRateSlider.value;
            testManager.RunUpdateRateTest(updateRate);
        }
    }
    
    private void TestObstacles()
    {
        if (testManager != null && obstacleCountSlider != null)
        {
            int obstacleCount = Mathf.RoundToInt(obstacleCountSlider.value);
            testManager.RunObstacleTest(obstacleCount);
        }
    }
    
    private void OnAgentCountChanged(float value)
    {
        UpdateSliderTexts();
    }
    
    private void OnUpdateRateChanged(float value)
    {
        UpdateSliderTexts();
    }
    
    private void OnObstacleCountChanged(float value)
    {
        UpdateSliderTexts();
    }
    
    // Quick spawn methods for immediate testing
    public void QuickSpawnAgents()
    {
        if (testManager != null && agentCountSlider != null && updateRateSlider != null)
        {
            int count = Mathf.RoundToInt(agentCountSlider.value);
            float rate = updateRateSlider.value;
            testManager.SpawnAgents(count, rate);
        }
    }
    
    public void QuickSpawnObstacles()
    {
        if (testManager != null && obstacleCountSlider != null)
        {
            int count = Mathf.RoundToInt(obstacleCountSlider.value);
            testManager.SpawnObstacles(count);
        }
    }
    
    // Keyboard shortcuts
    private void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            switch (e.keyCode)
            {
                case KeyCode.F1:
                    StartAutomaticTests();
                    break;
                case KeyCode.F2:
                    StopTests();
                    break;
                case KeyCode.F3:
                    ClearAllAgents();
                    break;
                case KeyCode.F4:
                    ExportResults();
                    break;
                case KeyCode.F5:
                    QuickSpawnAgents();
                    break;
                case KeyCode.F6:
                    QuickSpawnObstacles();
                    break;
            }
        }
        
        // Display keyboard shortcuts
        GUILayout.BeginArea(new Rect(10, Screen.height - 120, 300, 110));
        GUILayout.Label("Keyboard Shortcuts:", GUI.skin.box);
        GUILayout.Label("F1: Start Tests | F2: Stop Tests");
        GUILayout.Label("F3: Clear All | F4: Export Results");
        GUILayout.Label("F5: Spawn Agents | F6: Spawn Obstacles");
        GUILayout.EndArea();
    }
} 