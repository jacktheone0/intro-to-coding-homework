# Water Volume Setup Guide

## Current Setup (Wrong for Visuals)

```
Water Plane (2D surface at Y=0)
└── Shows water surface only
    └── Buoyancy calculated by Y-coordinate check
```

## Better Setup (3D Water Volume)

```
Water Volume (3D Box)
├── Visual: See-through blue box
├── Physics: Boat floats in it
└── Buoyancy: Calculated based on submersion depth
```

## How to Create Water Volume in Unity

### Step 1: Delete the Water Plane

1. Select your water plane
2. Delete it

### Step 2: Create Water Volume Box

1. GameObject → 3D Object → Cube
2. Rename to "WaterVolume"
3. Set Transform:
   ```
   Position: (0, -5, 0)  ← Half underwater
   Scale: (100, 10, 100)  ← 100x100 area, 10m deep
   ```

### Step 3: Setup Water Volume

**Add Components:**
```
WaterVolume GameObject:
├── Box Collider
│   ├── Is Trigger: ☑ CHECKED (boat passes through)
│   └── Size: (1, 1, 1) ← Unity scales this
│
└── WaterSystem Script (drag from before)
    └── Water Level: 0 (surface at Y=0)
```

### Step 4: Create Water Material

**For See-Through Water:**
```
1. Create Material: "WaterVolumeMaterial"
2. Settings:
   - Rendering Mode: Transparent
   - Albedo: Blue (0.0, 0.3, 0.6)
   - Alpha: 100 (very transparent)
   - Metallic: 0.3
   - Smoothness: 0.95
```

3. Drag material onto WaterVolume cube

### Step 5: Add Top Surface (Optional)

For a visible water surface with waves:

```
1. Create Child GameObject: "WaterSurface"
2. Add Quad (rotated to horizontal)
3. Position at Y = 5 (top of water box)
4. Scale to match water surface
5. Apply water shader with waves
```

## Visual Result

```
     _______________  ← Y = 0 (Water Surface - visible)
    |               |
    |   WATER BOX   | ← Semi-transparent blue
    |               |
    |    ⛵Boat      | ← Boat partially submerged
    |               |
    |_______________|  ← Y = -10 (Bottom)
```

## Physics Explanation

- **Y > 0**: Above water (air)
- **Y = 0**: Water surface
- **Y < 0**: Underwater (applies buoyancy)

The WaterSystem checks: `if (position.y < waterLevel)` to determine buoyancy.
