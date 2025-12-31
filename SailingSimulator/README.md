# Advanced Sailing Simulator - Unity

A high-fidelity sailing simulator focused on realistic physics and boat handling for three Olympic and youth racing classes.

## Features

### ⛵ Three Boat Classes

1. **ILCA (Laser) Full Rig**
   - Single-handed operation
   - Mylar sail material
   - Simple bridle traveler
   - Specifications: 4.23m length, 7.06m² sail area

2. **i420 (International 420)**
   - Two-person racing boat
   - Jib + symmetric spinnaker
   - Trapeze system
   - Track-and-car traveler (1.4m travel)
   - Dacron sails

3. **29er (International 29er)**
   - High-performance skiff
   - Asymmetric spinnaker
   - Dual trapeze capability
   - Continuous-line traveler (1.6m travel)
   - Laminate sails
   - Planing capability

### 🌊 Realistic Physics

#### Hydrodynamics
- Accurate buoyancy modeling (Archimedes principle)
- Hull resistance (friction, form, wave-making drag)
- Foil dynamics for centerboard/daggerboard and rudder
- Heel effects on performance
- Wave interaction with pitch and roll
- Current and tide simulation

#### Aerodynamics
- True wind particle modeling with turbulence
- Apparent wind calculation and display
- Lift and drag from sails at varying angles of attack
- Sail-specific materials (Dacron, Mylar, Laminate)
- Realistic luffing and sail shape deformation
- Wind gradient (speed increases with height)
- Wind shifts (oscillating and persistent)
- Gust modeling

#### Crew Dynamics
- Dynamic weight distribution (fore/aft, athwartships)
- Realistic hiking and righting moment
- Trapeze physics (i420 & 29er)
- Reference-based positioning for authenticity

### 🎮 Controls

**Class-Specific Traveler Systems:**
- **ILCA**: Simple bridle with 4:1 purchase, limited travel
- **i420**: Wide-range track system, fast adjustment
- **29er**: Continuous-line with cam cleat, instant repositioning

**Universal Controls:**
- Mainsheet, vang, cunningham, outhaul
- Centerboard/daggerboard positioning
- Rudder steering
- Crew weight positioning

**Multi-Sail Boats (i420, 29er):**
- Jib sheet and car adjustment
- Spinnaker controls (symmetric vs asymmetric)
- Trapeze systems with adjustable height

### 📊 Performance Instruments

Real-time display of:
- Boat speed (knots)
- VMG (Velocity Made Good) upwind and downwind
- Apparent wind angle and speed
- True wind angle and speed
- Heel angle
- Compass heading
- Polar diagram targets
- Sail trim indicators with telltales

### 🎥 Camera Modes

- **First Person**: Cockpit view from skipper position
- **Third Person Chase**: Following from behind/side
- **Broadcast**: Cinematic dynamic angles
- **Tactical Overhead**: Bird's eye for strategy

## Architecture

### Core Systems

```
SailingSimulator/
├── Core/
│   ├── Physics/
│   │   ├── PhysicsConstants.cs       # Physical constants
│   │   ├── VectorUtilities.cs        # Sailing calculations
│   │   ├── FoilDynamics.cs          # Centerboard/rudder forces
│   │   ├── SailAerodynamics.cs      # Sail lift/drag
│   │   └── HullDynamics.cs          # Hull resistance/buoyancy
│   ├── Boats/
│   │   ├── BaseBoat.cs              # Abstract boat class
│   │   ├── CrewWeightSystem.cs      # Crew positioning
│   │   ├── TravelerSystems.cs       # Class-specific travelers
│   │   ├── ILCABoat.cs              # ILCA implementation
│   │   ├── I420Boat.cs              # i420 implementation
│   │   └── Boat29er.cs              # 29er implementation
│   ├── Environment/
│   │   ├── WindSystem.cs            # Wind simulation
│   │   └── WaterSystem.cs           # Water physics
│   └── Input/
│       ├── SailingInputController.cs # Input handling
│       └── CameraController.cs       # Camera management
└── UI/
    └── SailingInstruments.cs         # Performance displays
```

### Physics Integration

Each boat integrates multiple physics systems:
1. **Wind System** → Apparent wind calculation
2. **Sail Aerodynamics** → Lift and drag forces
3. **Hull Dynamics** → Buoyancy and resistance
4. **Foil Dynamics** → Lateral resistance and turning
5. **Crew System** → Righting moment
6. **Traveler System** → Sail angle control

Forces are calculated each physics tick and applied to Unity Rigidbody.

## Installation

See [SETUP_GUIDE.md](Documentation/SETUP_GUIDE.md) for detailed installation instructions.

Quick start:
1. Import scripts into Unity Assets folder
2. Create environment (water plane + wind system)
3. Set up boat with transforms and components
4. Add camera and input controllers
5. Press play and sail!

## Controls

| Action | Keyboard | Function |
|--------|----------|----------|
| Steer | A/D or ← → | Rudder left/right |
| Mainsheet | W/S | Trim in / ease out |
| Jib Sheet | E/Q | Trim in / ease out (i420/29er) |
| Crew Weight | Arrow Keys | Move fore/aft, port/starboard |
| Traveler | T/G | Move starboard/port |
| Centerboard | Z/X | Lower/raise |
| Vang | V/B | Increase/decrease |
| Spinnaker | P | Deploy/douse (i420/29er) |
| Trapeze | R | Toggle crew on wire |
| Camera | C | Cycle views |

## Technical Details

### Boat Specifications

**ILCA Full Rig:**
- Length: 4.23m, Beam: 1.39m
- Displacement: 139kg (59kg hull + 80kg sailor)
- Sail Area: 7.06m²
- Mast Height: 8.03m

**i420:**
- Length: 4.2m, Beam: 1.63m
- Displacement: 240kg (100kg hull + 140kg crew)
- Main: 7.71m², Jib: 2.97m², Spinnaker: 13m²
- Mast Height: 6.3m

**29er:**
- Length: 4.45m, Beam: 1.77m
- Displacement: 200kg (70kg hull + 130kg crew)
- Main: 9.0m², Jib: 3.5m², Asym Spinnaker: 14.5m²
- Mast Height: 7.8m
- Planing capability at 12+ knots

### Physics Formulas

**Lift Coefficient** (thin airfoil theory):
```
cl = 2π × sin(α)
```
With stall beyond 15-20° for sails

**Drag Coefficient**:
```
cd = cd₀ + k × cl²
```
Includes induced drag from finite span

**Hull Speed** (theoretical maximum):
```
V_hull ≈ 1.34 × √LWL (knots)
```

**Apparent Wind**:
```
V_apparent = V_true - V_boat
```

## Future Development

Planned features:
- [ ] Practice mode with adjustable conditions
- [ ] Step-by-step tutorial system
- [ ] Racing mode with courses and starts
- [ ] AI opponents with skill levels
- [ ] Multiplayer networking
- [ ] Visual effects (spray, wake, sail animation)
- [ ] Audio system (wind, water, rigging sounds)
- [ ] Weather variations
- [ ] Replay system
- [ ] Polar diagram visualization

## Performance Notes

- Fixed timestep: 0.02s recommended
- Optimized for single boat simulation
- Multiplayer: 10+ boats possible with LOD
- Physics calculated at fixed update rate
- Visual updates at frame rate

## Success Criteria

✅ Boats handle realistically per sailing principles
✅ Traveler systems function authentically per class
✅ Trapeze provides correct righting moment
✅ All three boats feel distinctly different
✅ Controls intuitive for keyboard/controller
⏳ Experienced sailors recognize accurate physics
⏳ Newcomers can learn through tutorials
⏳ Multiplayer races stable and competitive

## License

Educational and training simulator.

Boat specifications based on published class rules and measurements.

## Contributing

Contributions welcome! Areas of focus:
- Improved polar diagram data
- Visual effects and graphics
- Tutorial content
- Racing rule implementation
- Multiplayer optimization

## Contact

For questions, suggestions, or collaboration opportunities, please create an issue in the repository.

---

**Built with Unity | Physics-Based Simulation | Realistic Sailing**
