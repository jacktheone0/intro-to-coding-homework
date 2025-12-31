# Sailing Simulator - Unity Setup Guide

## Overview

This is a high-fidelity sailing simulator built in Unity with realistic physics for three Olympic/youth racing boat classes:
- **ILCA (Laser) Full Rig** - Single-handed, single-sail
- **i420 (International 420)** - Two-person with symmetric spinnaker and trapeze
- **29er** - Two-person high-performance skiff with asymmetric spinnaker and trapeze

## Prerequisites

- Unity 2021.3 LTS or newer (recommended)
- Basic understanding of Unity Editor
- TextMeshPro package (for UI)

## Installation Steps

### 1. Import Scripts into Unity Project

1. Create a new Unity 3D project or open an existing one
2. Copy the entire `SailingSimulator` folder into your Unity project's `Assets` directory
3. Unity will automatically compile the C# scripts

### 2. Project Structure

```
Assets/
└── SailingSimulator/
    ├── Scripts/
    │   ├── Core/
    │   │   ├── Physics/          # Physics calculations
    │   │   ├── Boats/            # Boat classes
    │   │   ├── Environment/      # Wind and water systems
    │   │   └── Input/            # Input and camera
    │   ├── UI/                   # User interface
    │   └── Game/                 # Game modes (future)
    ├── Prefabs/                  # Boat prefabs (to create)
    ├── Scenes/                   # Game scenes
    └── Documentation/            # This guide
```

### 3. Create the Environment

#### Water Plane
1. Create a new **3D Object → Plane** in your scene
2. Scale it large (e.g., X: 100, Z: 100)
3. Position at Y: 0
4. Add the **WaterSystem** component to it
5. (Optional) Apply a water material/shader for visuals

#### Wind System
1. Create an empty GameObject named "WindSystem"
2. Add the **WindSystem** component
3. Configure wind settings:
   - Base Wind Speed: 10 knots (good starting point)
   - Base Wind Direction: 0° (north)
   - Enable Gusts: ☑
   - Enable Oscillating Shifts: ☑

### 4. Create Your First Boat (ILCA)

#### Basic Hull Setup
1. Create an empty GameObject named "ILCA_Boat"
2. Add a **Rigidbody** component:
   - Mass: 139 (kg)
   - Drag: 0.5
   - Angular Drag: 0.3
   - Use Gravity: ☑

3. Create child objects for visual representation:
   - **Hull**: 3D Capsule or custom mesh
   - **Mast**: Cylinder (height 8m)
   - **Boom**: Cylinder (horizontal, ~2.5m)
   - **Mainsail**: Quad or plane (for visual)
   - **Centerboard**: Small box below hull
   - **Rudder**: Small box at stern

#### Add ILCA Script
1. Add the **ILCABoat** script to the ILCA_Boat GameObject
2. Assign references:
   - Main Sail Transform: The sail quad/plane
   - Boom Transform: The boom cylinder
   - Mast Transform: The mast cylinder
   - Centerboard Transform: The centerboard object
   - Rudder Transform: The rudder object
   - Wind System: The WindSystem GameObject
   - Water System: The water plane GameObject

### 5. Set Up Camera

1. Create an empty GameObject named "CameraController"
2. Add the **CameraController** script
3. Assign references:
   - Boat Transform: The ILCA_Boat GameObject
   - Skipper Seat Transform: A position in the cockpit
4. The Main Camera will be controlled by this script

### 6. Set Up Input

1. On the ILCA_Boat GameObject, add the **SailingInputController** script
2. Assign references:
   - Current Boat: The ILCABoat component
   - Camera Controller: The CameraController component

### 7. Test the Scene

1. Position your boat above the water (Y: 0.5)
2. Press Play
3. Use these controls:
   - **A/D**: Steer
   - **W/S**: Mainsheet (trim/ease)
   - **Arrow Keys**: Crew weight
   - **T/G**: Traveler
   - **C**: Cycle camera modes

## Creating Other Boats

### i420 Boat
1. Follow the same steps as ILCA
2. Use **I420Boat** script instead
3. Add additional child objects:
   - Jib sail
   - Spinnaker (can be hidden initially)
   - Trapeze wire visualization
4. Configure for two crew members

### 29er Boat
1. Follow the same steps as ILCA
2. Use **Boat29er** script instead
3. Add asymmetric spinnaker (different shape from i420)
4. Configure for high-performance skiff characteristics

## Advanced Configuration

### Hull Specifications
Each boat script has detailed hull specifications. You can tune these in the inspector:
- Length, Beam, Displacement
- Draft, Wetted Surface Area
- Center of Buoyancy and Gravity

### Wind Conditions
Adjust wind for different scenarios:
- **Light Air**: 5-8 knots
- **Medium**: 10-15 knots
- **Heavy**: 18-25 knots

Enable/disable:
- Gusts (wind speed variation)
- Oscillating shifts (periodic direction changes)
- Persistent shifts (gradual direction changes)

### Tuning Presets
Each boat has tuning presets for different conditions. Call these from code:
```csharp
ilcaBoat.ApplyTuningPreset(WindCondition.Heavy);
```

## Performance Optimization

1. Use LOD (Level of Detail) for boat models
2. Limit physics update rate if needed: Edit → Project Settings → Time → Fixed Timestep
3. Use occlusion culling for multiple boats
4. Optimize water shader complexity

## Troubleshooting

### Boat Sinks or Flies Away
- Check Rigidbody mass matches boat displacement
- Verify WaterSystem is assigned and active
- Check that water plane is at Y: 0

### No Wind Effect
- Ensure WindSystem is in the scene and active
- Verify WindSystem reference is assigned to boat
- Check that wind speed is not zero

### Boat Doesn't Turn
- Verify rudder transform is assigned
- Check rudder input is being received (watch rudder angle in inspector during play)
- Increase rudder area in RudderDynamics if needed

### Poor Performance
- Reduce physics Fixed Timestep (0.02 → 0.03)
- Simplify boat mesh colliders
- Reduce water plane size
- Disable unnecessary visual effects

## Next Steps

After basic setup:
1. Add visual effects (water spray, wake, sail animation)
2. Create racing courses with marks
3. Implement AI opponents
4. Add multiplayer networking
5. Build tutorial system
6. Create polar diagram displays
7. Add weather variations (waves, rain)

## Controls Reference

### Universal Controls
- **A / D** or **← / →**: Rudder (steering)
- **W / S**: Mainsheet (trim in / ease out)
- **Arrow Keys** or **IJKL**: Crew weight position
- **T / G**: Traveler (starboard / port)
- **Z / X**: Centerboard (down / up)
- **V / B**: Vang (more / less)
- **C**: Cycle camera modes
- **Backspace**: Reset boat

### i420 / 29er Additional Controls
- **E / Q**: Jib sheet (trim / ease)
- **P**: Deploy/douse spinnaker
- **R**: Crew trapeze (toggle)
- **F**: Skipper trapeze (29er only)

## Credits

Developed as a high-fidelity sailing physics simulator for training and education.

Based on real-world specifications for ILCA, i420, and 29er class sailboats.
