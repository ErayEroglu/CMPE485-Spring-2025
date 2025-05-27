# Unity NavMesh Performance Testing MVP

A comprehensive Unity project for evaluating and comparing the performance scalability of Unity's NavMesh algorithm under increasing computational load.

## Overview

This MVP allows you to test NavMesh performance under three key scenarios:
1. **Agent Scaling**: Increasing number of agents
2. **Update Rate Scaling**: Higher frequency path updates
3. **Dynamic Obstacles**: Changing environment with moving obstacles

## Features

- **Automated Testing Suite**: Run comprehensive tests with predefined parameters
- **Manual Testing Controls**: Real-time parameter adjustment and testing
- **Performance Metrics Collection**: FPS, memory usage, frame time tracking
- **Data Export**: CSV and JSON export for analysis
- **Real-time Monitoring**: Live performance display during tests
- **Dynamic Obstacles**: Various movement patterns for obstacles
- **Optimized Agent Behavior**: Multiple movement patterns with performance optimizations

## Quick Setup (2-Day Implementation)

### Day 1: Basic Setup

1. **Scene Setup**:
   - Create a new scene or use existing `SampleScene`
   - Add a large Plane (scale 10x1x10) as the ground
   - Add NavMesh Surface component to the Plane
   - Bake the NavMesh (Window > AI > Navigation, select Plane, click Bake)

2. **Create Prefabs**:
   
   **Agent Prefab**:
   - Create a Capsule GameObject
   - Add NavMeshAgent component
   - Add either `EnemyMovement` or `TestAgent` script
   - Save as prefab in Assets folder
   
   **Obstacle Prefab**:
   - Create a Cube GameObject
   - Add NavMeshObstacle component
   - Add `DynamicObstacle` script
   - Save as prefab in Assets folder

3. **Setup Test Manager**:
   - Create empty GameObject named "TestManager"
   - Add `PerformanceTestManager` script
   - Add `PerformanceMetrics` script
   - Assign Agent and Obstacle prefabs
   - Set spawn area transform (can be the TestManager itself)

### Day 2: UI and Testing

4. **Create UI** (Optional but recommended):
   - Create Canvas
   - Add `TestUIController` script to a GameObject
   - Create basic UI elements (buttons, sliders, text)
   - Connect UI elements to TestUIController script

5. **Testing**:
   - Press Play
   - Use keyboard shortcuts (A, S, D, F, G, H) for quick testing
   - Or use UI controls if implemented
   - Results will be exported to `PerformanceResults` folder

## Scripts Overview

### Core Scripts

- **`PerformanceTestManager.cs`**: Main controller for all tests
- **`PerformanceMetrics.cs`**: Collects and exports performance data
- **`TestUIController.cs`**: UI controls for manual testing
- **`TestAgent.cs`**: Optimized agent with multiple movement patterns
- **`DynamicObstacle.cs`**: Dynamic obstacles with various behaviors

### Legacy Scripts (Already in project)

- **`EnemyMovement.cs`**: Basic agent following behavior
- **`PlayerMovement.cs`**: Click-to-move player controller

## Usage

### Automatic Testing

```csharp
// Enable auto-run in PerformanceTestManager inspector
// Or use keyboard shortcut A
// Or call via script:
testManager.StartCoroutine(testManager.RunAllTests());
```

### Manual Testing

```csharp
// Spawn agents manually
testManager.SpawnAgents(100, 0.1f); // 100 agents, 0.1s update rate

// Spawn obstacles
testManager.SpawnObstacles(25); // 25 dynamic obstacles

// Run specific tests
testManager.RunAgentScalingTest(500);
testManager.RunUpdateRateTest(0.05f);
testManager.RunObstacleTest(50);
```

### Keyboard Shortcuts

- **A**: Start automatic tests
- **S**: Stop all tests
- **D**: Clear all agents and obstacles
- **F**: Export results
- **G**: Quick spawn agents (based on UI sliders)
- **H**: Quick spawn obstacles (based on UI sliders)

## Test Parameters

### Default Agent Scaling Tests
- Agent counts: 50, 100, 150, 200, ..., 1000
- Update rate: 0.1s
- Obstacles: 0

### Default Update Rate Tests
- Update rates: 0.1s, 0.05s, 0.02s, 0.01s
- Agent count: 200
- Obstacles: 0

### Default Obstacle Tests
- Obstacle counts: 0, 10, 25, 50, 100
- Agent count: 200
- Update rate: 0.1s

## Performance Metrics

The system tracks:
- **FPS**: Frames per second
- **Frame Time**: Time per frame in milliseconds
- **Memory Usage**: Total allocated memory
- **Agent Count**: Number of active agents
- **Obstacle Count**: Number of active obstacles
- **NavMesh Queries**: Active pathfinding queries

## Data Export

Results are exported to `PerformanceResults/` folder:
- **CSV**: Summary data for spreadsheet analysis
- **JSON**: Detailed data including raw samples

## Optimization Features

### Agent Optimizations
- Distance-based culling
- Optimized NavMeshAgent settings
- Coroutine-based updates
- Target position caching

### Obstacle Optimizations
- Visibility-based enabling/disabling
- Configurable carving settings
- Multiple movement patterns
- Performance-aware updates

## Customization

### Adding New Test Types

```csharp
// In PerformanceTestManager.cs
public void RunCustomTest(int agents, float rate, int obstacles)
{
    var config = new TestConfiguration
    {
        testName = "Custom_Test",
        agentCount = agents,
        updateRate = rate,
        obstacleCount = obstacles,
        testType = TestType.Custom
    };
    
    StartCoroutine(RunSingleTest(config));
}
```

### Custom Agent Behaviors

```csharp
// In TestAgent.cs, add new movement pattern
public enum MovementPattern
{
    // ... existing patterns
    YourCustomPattern
}

// Implement in UpdateMovement()
case MovementPattern.YourCustomPattern:
    UpdateYourCustomPattern();
    break;
```

## Expected Results

### Performance Baselines (approximate)
- **100 agents**: 60+ FPS on modern hardware
- **500 agents**: 30-60 FPS depending on hardware
- **1000 agents**: 15-30 FPS, may require optimization

### Key Metrics to Analyze
1. **FPS degradation** vs agent count
2. **Memory usage growth** with scale
3. **Frame time consistency** under load
4. **Impact of dynamic obstacles** on performance

## Troubleshooting

### Common Issues

1. **Agents not moving**: Check NavMesh is baked and agents are on NavMesh
2. **Poor performance**: Reduce agent count or enable optimizations
3. **No data export**: Check folder permissions and file paths
4. **UI not working**: Ensure UI elements are properly connected

### Performance Tips

1. Use **Low Quality** obstacle avoidance for large agent counts
2. Enable **optimizations** in TestAgent and DynamicObstacle scripts
3. Reduce **update rates** for better performance
4. Use **culling distance** to disable distant objects

## Future Enhancements

- A* pathfinding comparison
- Multi-threading support
- GPU-based crowd simulation
- Advanced analytics and visualization
- Automated report generation

## Requirements

- Unity 2021.3 LTS or newer
- NavMesh components package
- TextMeshPro (for UI)

## License

This project is for educational and research purposes. 