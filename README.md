# Advanced Sailing Simulator - Unity Project

This repository contains a high-fidelity sailing simulator built in Unity with realistic physics simulation.

## What's Inside

This project features:

- **Three Olympic/Youth Racing Boat Classes**:
  - ILCA (Laser) Full Rig - Single-handed sailing
  - i420 - Two-person with symmetric spinnaker and trapeze
  - 29er - High-performance skiff with asymmetric spinnaker

- **Realistic Physics**:
  - Hydrodynamics (buoyancy, hull drag, foils)
  - Aerodynamics (wind simulation, sail forces)
  - Crew dynamics (weight distribution, trapeze)
  - Class-specific traveler systems

- **Full Control Systems**:
  - Keyboard and controller support
  - Multiple camera modes
  - Performance instruments
  - Realistic boat handling

## Getting Started

See the complete documentation in the `SailingSimulator` folder:

- **[README.md](SailingSimulator/README.md)** - Project overview and features
- **[SETUP_GUIDE.md](SailingSimulator/Documentation/SETUP_GUIDE.md)** - Unity installation and setup
- **[PHYSICS_EXPLAINED.md](SailingSimulator/Documentation/PHYSICS_EXPLAINED.md)** - Detailed physics documentation

## Quick Start

1. Open this project in Unity 2021.3 LTS or newer
2. Navigate to `SailingSimulator/Documentation/SETUP_GUIDE.md`
3. Follow the setup instructions to create your first scene
4. Press Play and start sailing!

## Project Structure

```
SailingSimulator/
├── Scripts/
│   ├── Core/
│   │   ├── Physics/      # Physics calculations
│   │   ├── Boats/        # Boat implementations
│   │   ├── Environment/  # Wind and water
│   │   └── Input/        # Controls and camera
│   └── UI/               # Instruments display
├── Documentation/        # Guides and explanations
├── Prefabs/             # Boat prefabs (to create)
└── README.md            # Main project documentation
```

## Controls

- **A/D**: Steer
- **W/S**: Mainsheet (trim/ease)
- **Arrow Keys**: Crew weight
- **T/G**: Traveler
- **C**: Cycle camera modes

Full controls in the [documentation](SailingSimulator/README.md).

## Features Implemented

✅ Complete physics engine with realistic forces
✅ Three distinct boat classes with unique characteristics
✅ Traveler systems (ILCA bridle, i420 track, 29er continuous-line)
✅ Trapeze mechanics for i420 and 29er
✅ Wind simulation with gusts and shifts
✅ Water physics with waves and currents
✅ Camera system with multiple views
✅ Input handling for keyboard and controller
✅ Performance instruments display

## Future Development

- Practice mode with adjustable conditions
- Tutorial system
- Racing mode with courses
- Multiplayer networking
- AI opponents
- Visual effects (spray, wake, sail animation)

## Technical Details

Built with Unity C# using physics-based simulation:
- Archimedes principle for buoyancy
- Lift/drag calculations for sails and foils
- Realistic wind gradient and turbulence
- Class-specific specifications from real boats

## License

Educational sailing simulator for training and learning.

---

**For full documentation, see [SailingSimulator/README.md](SailingSimulator/README.md)**
