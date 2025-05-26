# Pathfinding Comparison System

A Unity project for comparing the performance of A* and NavMesh pathfinding algorithms under various loads and conditions.

## Project Overview

This system provides a framework for testing and comparing the performance of two pathfinding algorithms:
1. A* - A grid-based pathfinding algorithm
2. NavMesh - Unity's built-in navigation mesh system

The system measures:
- FPS (Frames Per Second)
- CPU usage (milliseconds per frame)
- Memory consumption (in MB)

## Getting Started

### Prerequisites
- Unity 2020.3 or newer
- TextMesh Pro package (imported through Package Manager if not already present)

### Project Setup

1. **Scene Setup**:
   - Create a new Unity scene
   - Add a plane or other suitable ground surface
   - Create a basic camera setup looking down at the scene

2. **Required Components**:
   - Create an empty GameObject named "PathfindingSystem"
   - Add the following components to it:
     - `TestManager`
     - `PerformanceMonitor`
     - `UIManager`

3. **NavMesh Setup**:
   - Create a NavMesh surface in your scene
   - Ensure all walkable areas are properly baked in the NavMesh

4. **Agent Prefab**:
   - Create a simple agent prefab (e.g., a capsule or sphere)
   - Add the `Agent` component to it
   - (Optional) Add visual elements or effects to make agents more visible

5. **Obstacle Prefab**:
   - Create a simple obstacle prefab (e.g., a cube)
   - Ensure it has a collider component
   - Set appropriate layer for obstacle detection

6. **UI Setup**:
   - Create a Canvas for the UI
   - Add Text elements for displaying performance metrics
   - Add buttons and dropdowns for controlling the tests
   - Assign all UI references in the UIManager component

## Running Tests

### Manual Testing
1. Select the pathfinding algorithm (A* or NavMesh)
2. Set the desired number of agents
3. Click "Start Test" to begin
4. Observe performance metrics in real-time
5. Click "Stop Test" when done

### Automated Testing
1. Configure test parameters in the TestManager component:
   - Agent counts to test
   - Test duration
   - Delay between tests
2. Click "Run All Tests" to execute the full test sequence
3. Wait for all tests to complete
4. View or export results

## Visualizing Results

1. Navigate to the Results panel
2. Choose a visualization option:
   - FPS Chart
   - CPU Usage Chart
   - Memory Usage Chart
   - Algorithm Comparison Chart
3. For comparison charts, select:
   - The metric to compare (FPS, CPU, Memory)
   - The agent count to compare

## Exporting Data

1. Run tests to collect performance data
2. Click "Export Data" to save results
3. Data is exported as a CSV file to the application's persistent data path
4. Use external tools (Excel, Google Sheets, etc.) for further analysis

## Customization

### Modifying A* Settings
- Grid size and resolution can be adjusted in the Grid component
- Pathfinding behavior can be modified in AStarPathfinder

### Modifying NavMesh Settings
- NavMesh settings can be adjusted in the NavMesh component
- Rebake frequency for dynamic obstacles can be changed in NavMeshPathfinder

### Testing Parameters
- Adjust agent counts, test duration, and other parameters in TestManager
- Modify agent behavior in the Agent component

## Implementation Notes

- The A* implementation uses an optimized grid system with multi-threading support
- NavMesh implementation uses Unity's built-in NavMesh system
- Dynamic obstacles cause NavMesh rebaking at controlled intervals
- Performance monitoring samples at regular intervals to provide consistent measurements

## Troubleshooting

- If A* pathfinding is slow, try adjusting grid size or enabling multithreading
- If NavMesh rebaking causes framerate drops, increase the rebake interval
- For very high agent counts, reduce pathfinding frequency in the Agent component 