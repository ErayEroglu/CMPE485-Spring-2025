# Setup Instructions for NavMesh Performance Testing MVP

## ✅ All Compilation Errors Fixed!

The latest version of the scripts includes fixes for all known compilation errors:

- ✅ **NavMeshSurface/CollectObjects errors** - Fixed with reflection-based compatibility
- ✅ **Profiler.GetTotalAllocatedMemory warnings** - Fixed with compatibility helper
- ✅ **Float to long conversion errors** - Fixed with proper casting
- ✅ **Missing using directives** - All added

## Quick Setup

### 1. Install AI Navigation Package (Recommended)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click the `+` button in the top-left
3. Select `Add package by name...`
4. Enter: `com.unity.ai.navigation`
5. Click `Add`

This will install the modern NavMesh components and provide the best experience.

### 2. Alternative: Use Legacy NavMesh (Automatic Fallback)

If you can't install the AI Navigation package, the scripts will automatically fall back to Unity's built-in NavMesh system with full functionality.

## Verification Steps

After setup, verify everything works:

1. **Open the Scene**:
   - Create a new scene or use an existing one
   - Add the `SceneSetupHelper` component to any GameObject

2. **Run Scene Setup**:
   ```
   Right-click on SceneSetupHelper component → "Setup Complete Scene"
   ```

3. **Test Basic Functionality**:
   - Press Play
   - Press G to spawn some agents
   - Agents should move around the scene

4. **Run Performance Tests**:
   - Press A to start automated tests
   - Check Console for progress updates
   - Results will be saved to `PerformanceResults/` folder

## Keyboard Controls

- **A** - Start automated performance tests
- **S** - Stop current test
- **D** - Clear all agents and obstacles
- **F** - Export current results
- **G** - Spawn 50 agents
- **H** - Spawn 10 obstacles

## Package Requirements

- **Unity 2021.3 LTS or newer**
- **AI Navigation Package** (com.unity.ai.navigation) - Optional but recommended
- **TextMeshPro** - For UI (usually pre-installed)

## Troubleshooting

### No Compilation Errors Expected
All known compilation errors have been fixed with compatibility layers.

### "NavMeshSurface component not available"
This is just a warning. The system will fall back to legacy NavMesh baking automatically.

### Agents not moving
1. Ensure NavMesh is baked (blue areas should be visible in Scene view)
2. Check that agents are positioned on the NavMesh
3. Verify a Player object exists in the scene for agents to follow

### Performance Test Results
Results are automatically exported to the `PerformanceResults/` folder in your project directory as both CSV and JSON files.

## Success Indicators

✅ **No compilation errors**  
✅ **Agents spawn and move when pressing G**  
✅ **NavMesh visible as blue areas in Scene view**  
✅ **Performance tests run when pressing A**  
✅ **Results export to PerformanceResults folder**  
✅ **Real-time performance metrics display during tests**

## What's Fixed

### Memory Profiling Compatibility
- Automatically uses `GetTotalAllocatedMemoryLong()` when available
- Falls back to `GetTotalAllocatedMemory()` with proper casting for older Unity versions
- Suppresses obsolete warnings appropriately

### NavMesh Compatibility
- Uses reflection to safely set NavMeshSurface properties
- Graceful fallback to legacy NavMesh baking
- Works with or without AI Navigation package

### Type Safety
- All float-to-long conversions handled properly
- Proper exception handling for missing components
- Comprehensive error logging

## Next Steps

Once setup is complete, you can:

1. **Run Automated Tests**: Press A to run all 29 predefined performance tests
2. **Manual Testing**: Use G/H for quick agent/obstacle spawning
3. **Data Analysis**: Check the exported CSV/JSON files for detailed performance metrics
4. **Customize Tests**: Modify the test configurations in `PerformanceTestManager.cs`

The MVP is now ready for your 2-day implementation timeline with comprehensive NavMesh performance evaluation capabilities! 