using UnityEngine;

namespace SailingSimulator.Core.Physics
{
    /// <summary>
    /// Sail cloth material properties
    /// </summary>
    public enum SailMaterial
    {
        Dacron,     // Heavy, stretchy (i420)
        Mylar,      // Crisp, minimal stretch (ILCA)
        Laminate    // Lightweight, very stiff (29er)
    }

    /// <summary>
    /// Sail types
    /// </summary>
    public enum SailType
    {
        Mainsail,
        Jib,
        SymmetricSpinnaker,
        AsymmetricSpinnaker
    }

    /// <summary>
    /// Calculates aerodynamic forces from sails
    /// </summary>
    public class SailAerodynamics
    {
        // Sail properties
        public SailType sailType;
        public SailMaterial material;
        public float area; // m²
        public float aspectRatio;

        // Control settings
        public float sheetTension = 0.5f; // 0-1, how much sheet is trimmed
        public float cunninghamTension = 0f; // 0-1
        public float outhaul = 0.5f; // 0-1
        public float vangTension = 0f; // 0-1

        // State
        public bool isLuffing = false;
        public float luffAmount = 0f; // 0-1

        private const float LUFF_ANGLE_THRESHOLD = 15f; // degrees

        public SailAerodynamics(SailType type, SailMaterial mat, float sailArea)
        {
            sailType = type;
            material = mat;
            area = sailArea;
            aspectRatio = CalculateAspectRatio(type);
        }

        /// <summary>
        /// Calculate lift and drag forces from sail
        /// </summary>
        public void CalculateForces(
            Vector3 apparentWind,
            Vector3 sailNormal,
            Vector3 boomDirection,
            float height,
            out Vector3 lift,
            out Vector3 drag)
        {
            lift = Vector3.zero;
            drag = Vector3.zero;

            // Apply wind gradient (wind speed increases with height)
            float windGradientFactor = PhysicsConstants.WindSpeedAtHeight(1f, height);
            Vector3 adjustedWind = apparentWind * windGradientFactor;

            float windSpeed = adjustedWind.magnitude;
            if (windSpeed < 0.5f)
                return;

            // Calculate angle of attack (angle between apparent wind and sail)
            Vector3 windDirection = adjustedWind.normalized;
            float angleOfAttack = Vector3.SignedAngle(windDirection, sailNormal, Vector3.up);

            // Determine if sail is luffing
            UpdateLuffingState(angleOfAttack);

            // Get effective sail coefficients
            float cl = CalculateSailLiftCoefficient(angleOfAttack);
            float cd = CalculateSailDragCoefficient(cl, angleOfAttack);

            // Apply material and control modifiers
            ApplyControlEffects(ref cl, ref cd);

            // Calculate forces
            VectorUtilities.CalculateLiftDrag(
                adjustedWind,
                sailNormal,
                area,
                cl,
                cd,
                PhysicsConstants.AIR_DENSITY,
                out lift,
                out drag
            );

            // Spinnakers have different force characteristics
            if (IsSpinnaker())
            {
                ApplySpinnakerModifications(ref lift, ref drag, angleOfAttack);
            }
        }

        /// <summary>
        /// Calculate optimal sail trim angle for given apparent wind
        /// </summary>
        public float CalculateOptimalTrimAngle(float apparentWindAngle)
        {
            if (IsSpinnaker())
            {
                // Spinnakers: broad reaching angles
                return Mathf.Clamp(apparentWindAngle * 0.6f, 60f, 90f);
            }
            else
            {
                // Main and jib: narrower angles
                return Mathf.Clamp(apparentWindAngle * 0.5f, 10f, 45f);
            }
        }

        private float CalculateSailLiftCoefficient(float angleOfAttack)
        {
            // Sails operate at higher angles than foils
            float absAngle = Mathf.Abs(angleOfAttack);

            if (absAngle < LUFF_ANGLE_THRESHOLD)
            {
                // Luffing - very low lift
                return 0.2f * (absAngle / LUFF_ANGLE_THRESHOLD);
            }
            else if (absAngle < 45f)
            {
                // Optimal range
                float normalized = (absAngle - LUFF_ANGLE_THRESHOLD) / (45f - LUFF_ANGLE_THRESHOLD);
                return Mathf.Lerp(0.8f, 1.5f, normalized);
            }
            else if (absAngle < 90f)
            {
                // Deep angles - reducing efficiency
                float normalized = (absAngle - 45f) / 45f;
                return Mathf.Lerp(1.5f, 0.8f, normalized);
            }
            else
            {
                // Sail stalled/backwinded
                return 0.3f;
            }
        }

        private float CalculateSailDragCoefficient(float cl, float angleOfAttack)
        {
            float baseDrag = 0.05f;

            // Sails have high drag compared to rigid foils
            float inducedDrag = cl * cl * 0.15f;

            // Add drag from sail flapping
            if (isLuffing)
            {
                baseDrag += luffAmount * 0.5f;
            }

            return baseDrag + inducedDrag;
        }

        private void UpdateLuffingState(float angleOfAttack)
        {
            float absAngle = Mathf.Abs(angleOfAttack);

            if (absAngle < LUFF_ANGLE_THRESHOLD)
            {
                isLuffing = true;
                luffAmount = 1f - (absAngle / LUFF_ANGLE_THRESHOLD);
            }
            else
            {
                isLuffing = false;
                luffAmount = 0f;
            }
        }

        private void ApplyControlEffects(ref float cl, ref float cd)
        {
            // Cunningham: Tightens luff, moves draft forward
            float cunninghamEffect = cunninghamTension * 0.1f;
            cl *= (1f + cunninghamEffect);

            // Outhaul: Flattens sail
            float outhaulEffect = outhaul * 0.05f;
            cl *= (1f - outhaulEffect);

            // Vang: Controls leech tension and twist
            float vangEffect = vangTension * 0.08f;
            cl *= (1f + vangEffect);

            // Material properties
            switch (material)
            {
                case SailMaterial.Dacron:
                    // More stretch = less efficient but more forgiving
                    cl *= 0.95f;
                    cd *= 1.05f;
                    break;
                case SailMaterial.Mylar:
                    // Balanced performance
                    break;
                case SailMaterial.Laminate:
                    // Very efficient
                    cl *= 1.05f;
                    cd *= 0.95f;
                    break;
            }
        }

        private void ApplySpinnakerModifications(ref Vector3 lift, ref Vector3 drag, float angleOfAttack)
        {
            // Spinnakers generate more drag-based force
            float dragBoost = 1.5f;
            drag *= dragBoost;

            // Reduce lift at extreme angles
            if (Mathf.Abs(angleOfAttack) > 60f)
            {
                lift *= 0.7f;
            }
        }

        private bool IsSpinnaker()
        {
            return sailType == SailType.SymmetricSpinnaker || sailType == SailType.AsymmetricSpinnaker;
        }

        private float CalculateAspectRatio(SailType type)
        {
            switch (type)
            {
                case SailType.Mainsail:
                    return 2.5f;
                case SailType.Jib:
                    return 3.0f;
                case SailType.SymmetricSpinnaker:
                    return 1.5f;
                case SailType.AsymmetricSpinnaker:
                    return 2.0f;
                default:
                    return 2.5f;
            }
        }

        /// <summary>
        /// Get visual sail shape parameters for rendering
        /// </summary>
        public float GetSailCamber()
        {
            // Camber (depth of sail curve) based on sheet tension
            float baseCamber = 0.1f;

            // Tighter sheet = flatter sail
            float sheetEffect = (1f - sheetTension) * 0.05f;

            // Outhaul also flattens
            float outhaulEffect = outhaul * 0.03f;

            return baseCamber + sheetEffect - outhaulEffect;
        }
    }
}
