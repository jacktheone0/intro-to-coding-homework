# Sailing Physics Explained

This document provides a detailed explanation of the physics implementation in the sailing simulator.

## Table of Contents

1. [Coordinate System](#coordinate-system)
2. [Hydrodynamics](#hydrodynamics)
3. [Aerodynamics](#aerodynamics)
4. [Forces and Moments](#forces-and-moments)
5. [Traveler Systems](#traveler-systems)
6. [Crew Dynamics](#crew-dynamics)

## Coordinate System

Unity uses a left-handed coordinate system:
- **X-axis**: Right (starboard when facing forward)
- **Y-axis**: Up
- **Z-axis**: Forward (bow direction)

In sailing terms:
- **Athwartships**: X-axis (port/starboard)
- **Vertical**: Y-axis (up/down)
- **Fore/Aft**: Z-axis (bow/stern)

## Hydrodynamics

### Buoyancy

Implemented in `HullDynamics.cs`

**Archimedes' Principle**:
```
F_buoyancy = ρ_water × V_submerged × g
```

Where:
- ρ_water = 1025 kg/m³ (saltwater density)
- V_submerged = volume of hull below waterline
- g = 9.81 m/s²

The simulator samples multiple points on the hull to determine submerged volume, accounting for:
- Hull shape (block coefficient, prismatic coefficient)
- Heel angle (changes waterline)
- Wave height (from WaterSystem)

**Center of Buoyancy (COB)**:
- Shifts to leeward when heeled
- Creates righting moment when separated from center of gravity
- Calculated dynamically based on submerged volume distribution

### Hull Resistance

Three components of drag:

**1. Friction Drag** (skin friction):
```
Cf = 0.075 / (log₁₀(Rn) - 2)²
D_friction = 0.5 × ρ × V² × S_wetted × Cf
```

Where:
- Rn = Reynolds number = V × L / ν
- S_wetted = wetted surface area
- ν = kinematic viscosity (≈ 10⁻⁶ m²/s for water)

**2. Form Drag** (pressure drag):
```
D_form = 0.5 × ρ × V² × A_ref × Cd_form
```
- Depends on hull shape
- Increases with beam width
- Coefficient typically 0.05-0.1 for sailboats

**3. Wave-Making Drag**:
```
D_wave = 0.5 × ρ × V² × A × Cd_wave
Cd_wave ∝ Fr⁴
```

Where Froude number:
```
Fr = V / √(g × L)
```

**Hull speed** (displacement mode limit):
```
V_hull ≈ 1.34 × √L_waterline (in knots)
```

Beyond this speed, wave-making drag increases dramatically. The 29er can "break through" and plane due to its flat sections and light weight.

### Foil Dynamics (Centerboard/Daggerboard/Rudder)

Implemented in `FoilDynamics.cs`

Underwater foils generate lift perpendicular to flow:

**Lift Force**:
```
L = 0.5 × ρ_water × V² × A × Cl
```

**Lift Coefficient** (thin foil theory):
```
Cl = 2π × sin(α)
```

Where α = angle of attack (angle between flow and foil)

**Stall**: Occurs at ~20° angle of attack for typical foils. Beyond this, lift drops dramatically and drag increases.

**Drag Force**:
```
D = 0.5 × ρ_water × V² × A × Cd
Cd = Cd₀ + Cd_induced
Cd_induced = Cl² / (π × AR × e)
```

Where:
- AR = aspect ratio = span² / area
- e = Oswald efficiency factor (≈0.9 for foils)

**Purpose**:
- **Centerboard/Daggerboard**: Resists lateral (leeway) force from sails
- **Rudder**: Generates lateral force for turning when deflected

## Aerodynamics

### Wind System

Implemented in `WindSystem.cs`

**True Wind**: The actual wind in the fixed reference frame
- Defined by speed and direction
- Varies with height (wind gradient)
- Includes gusts and shifts

**Wind Gradient**:
```
V(h) = V_ref × (h / h_ref)^α
```
- α ≈ 0.11 over water (lower than over land)
- h_ref = 10m (standard reference height)

**Apparent Wind**: Wind felt by moving boat
```
V_apparent = V_true - V_boat
```

This is a vector subtraction! As boat accelerates:
- Apparent wind speed increases
- Apparent wind angle moves forward (heads the boat)

Example:
- True wind: 10 knots from north
- Boat moving: 5 knots north
- Apparent wind: 15 knots from north

### Sail Aerodynamics

Implemented in `SailAerodynamics.cs`

Sails work like airplane wings, but:
1. Can operate at much higher angles of attack
2. Change shape dynamically
3. Have lower aspect ratios
4. Generate both lift and drag

**Lift Coefficient** for sails:
```
Cl = f(α)

α < 15°:    Cl = 0.2 (luffing, very low)
15° < α < 45°:  Cl = 0.8 to 1.5 (optimal)
45° < α < 90°:  Cl = 1.5 to 0.8 (deep angles)
α > 90°:    Cl = 0.3 (backwinded)
```

**Drag Coefficient**:
```
Cd = Cd₀ + k × Cl²
```
- Cd₀ ≈ 0.05 for sails (base drag)
- k ≈ 0.15 (induced drag factor, higher than rigid foils)

**Force Components**:
```
Lift = 0.5 × ρ_air × V_apparent² × A_sail × Cl
Drag = 0.5 × ρ_air × V_apparent² × A_sail × Cd
```

Where:
- ρ_air = 1.225 kg/m³
- A_sail = sail area
- V_apparent = apparent wind speed

**Direction**:
- Lift: Perpendicular to apparent wind
- Drag: Parallel to apparent wind (opposite direction)

### Sail Material Effects

**Dacron** (i420):
- Stretchier, more forgiving
- Cl reduced by 5%, Cd increased by 5%

**Mylar** (ILCA):
- Balanced performance
- Standard coefficients

**Laminate** (29er):
- Very stiff, efficient
- Cl increased by 5%, Cd reduced by 5%

### Sail Controls

**Mainsheet**: Primary sail angle control
- Tight (1.0): ~10° boom angle
- Eased (0.0): ~80° boom angle

**Vang**: Controls boom height and leech tension
- More vang → flatter sail → slightly higher Cl

**Cunningham**: Luff tension
- Moves draft forward → slightly higher Cl

**Outhaul**: Foot tension
- Tighter → flatter sail → lower Cl, lower drag

## Forces and Moments

### Force Balance

For steady sailing (equilibrium):

**Forward/Backward**:
```
Thrust (from sails) = Drag (from hull + foils)
```

**Sideways**:
```
Lateral force (from sails) = Lateral resistance (from foils)
```

**Heel Moment**:
```
Heeling moment (from sails) = Righting moment (from buoyancy + crew)
```

### Heeling Moment

Calculated in `BaseBoat.cs`:

```
M_heel = F_lateral × h_CE
```

Where:
- F_lateral = lateral component of sail force
- h_CE = height of center of effort above center of gravity

### Righting Moment

Two components:

**1. Hull Form Stability**:
```
M_righting_hull = (COB_x - COG_x) × Weight
```
- COB shifts to leeward when heeled
- Creates restoring moment

**2. Crew Weight**:
```
M_righting_crew = Σ(m_crew × g × x_crew)
```
- x_crew = horizontal distance from centerline
- Crew hiking dramatically increases this

**3. Trapeze** (i420, 29er):
```
M_righting_trapeze = m_crew × g × L_wire × cos(θ_heel)
```
- L_wire = wire length (≈2.5m)
- Provides much more moment than hiking (~2x)

### Heel Dynamics

```
I_heel × α = M_heel - M_righting
```

Where:
- I_heel = moment of inertia about fore/aft axis
- α = angular acceleration

Simplified in simulator with damping:
```
θ_heel(t+Δt) = θ_heel(t) + α × Δt × damping
```

## Traveler Systems

Travelers control the athwartships (sideways) position of the mainsheet attachment point.

### Effect on Sailing

**Centered Traveler** (0 position):
- Mainsheet pulls down and aft
- Tight slot between jib and main
- Good for upwind

**Eased to Leeward** (negative position):
- Mainsheet pulls more downward
- Opens leech (top of sail)
- Reduces heeling moment
- Good for reaching/running

**To Windward** (positive position):
- Used rarely, mainly for fine-tuning upwind
- Can help pointing in light air

### ILCA Traveler

**Type**: Simple bridle with 4:1 purchase

**Travel**: 0.4m (12-18 inches)

**Mechanical Advantage**:
- Need to pull 4× the distance
- Provides more control, but slower adjustment

**Angle Modifier**:
```
Δθ_boom = position × 5°  (-5° to +5°)
```

**Use Case**: Fine-tuning upwind in <60° AWA

### i420 Traveler

**Type**: Track-and-car system

**Travel**: 1.4m (4-5 feet)

**Control**: Twin lines, one per side
- Fast, responsive
- Can be quickly repositioned

**Angle Modifier**:
```
Δθ_boom = position × 20°  (-20° to +20°)
```

**Dynamic Positioning**:
```
Upwind (AWA < 50°):     position = 0 (centered)
Reaching (50° < AWA < 90°): position = -0.3
Running (AWA > 90°):     position = -0.6
```

### 29er Traveler

**Type**: Continuous line with cam cleat

**Travel**: 1.6m (5-6 feet)

**Mechanism**:
- Release cam cleat for instant adjustment
- Engage to lock position

**Angle Modifier**:
```
Δθ_boom = position × 25°  (-25° to +25°)
```

**Dynamic Control** (when planing):
```
if (heel > 15°):
    position = -0.7  // Significant depower
else if (planing):
    position = -0.3  // Moderate ease
```

Skiffs require constant traveler adjustment to maintain control.

## Crew Dynamics

### Weight Distribution

**Fore/Aft** (pitch trim):
- Forward: Prevents bow-up, good for running
- Aft: Helps boat climb waves upwind
- Centered: Default for most conditions

**Athwartships** (heel control):
- Windward (hiking): Reduces heel
- Leeward: Increases heel (useful in light air to depower)
- Centered: Neutral

### Hiking

Crew sits on side deck, body outside boat:

**Righting Moment**:
```
M = m_crew × g × x_hiking
```

Where x_hiking ≈ 0.5m to 0.8m depending on boat beam

### Trapeze

Crew hangs from wire attached to mast:

**Geometry**:
```
x_trapeze = L_wire × sin(θ_heel)
y_trapeze = -L_wire × cos(θ_heel)
```

**Righting Moment**:
```
M = m_crew × g × L_wire × cos(θ_heel)
```

For L_wire = 2.5m, this is ~3× more effective than hiking!

**Trade-off**:
- Requires skill and timing
- Crew fully commits to one side
- Difficult to adjust quickly

## Performance Optimization

### Upwind

**Goal**: Maximize VMG (Velocity Made Good) to windward

**Optimal TWA**:
- ILCA: ~40-45°
- i420: ~38-42°
- 29er: ~35-40° (can point higher)

**Controls**:
- Mainsheet: Tight (0.7-0.9)
- Traveler: Centered (0)
- Crew: Hiked to keep heel 10-15°
- Centerboard: Fully down

### Reaching

**Goal**: Maximum boat speed

**Optimal TWA**: 90-110°

**Controls**:
- Mainsheet: Moderate (0.4-0.6)
- Traveler: Slightly eased (-0.3)
- Crew: Centered athwartships, slightly aft
- Centerboard: 70-80% down

### Running

**Goal**: Maximize VMG downwind

**Optimal TWA**:
- ILCA: 150-160°
- i420: 140-150° (with spinnaker)
- 29er: 130-145° (faster angles, can plane)

**Controls**:
- Mainsheet: Very eased (0.1-0.3)
- Traveler: Well eased (-0.6 to -0.8)
- Spinnaker: Deployed (i420, 29er)
- Crew: Forward to prevent bow-up
- Centerboard: 30-50% (reduce wetted surface)

## Planing (29er)

**Condition**: Fr > 0.4

When boat speed exceeds:
```
V_planing ≈ 0.4 × √(g × L) ≈ 2.6 m/s ≈ 5 knots
```

**Effects**:
- Hull lifts partially out of water
- Wetted surface area reduces dramatically
- Speed can exceed theoretical hull speed
- Requires constant balancing and trim

**Technique**:
- Pump sails to initiate
- Crew weight aft initially, then forward
- Ease traveler to depower and maintain control
- Centerboard partially raised

## Further Reading

For more details on sailing physics:
- "Principles of Yacht Design" by Larsson & Eliasson
- "The Physics of Sailing" by John Kimball
- "Sail Power" by Wallace Ross

For Unity physics implementation:
- Unity Rigidbody documentation
- "Game Physics" by David Eberly
- "Real-Time Collision Detection" by Christer Ericson
