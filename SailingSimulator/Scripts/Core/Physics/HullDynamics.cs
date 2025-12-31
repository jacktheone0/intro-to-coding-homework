using UnityEngine;

namespace SailingSimulator.Core.Physics
{
    /// <summary>
    /// Hull shape and performance characteristics
    /// </summary>
    [System.Serializable]
    public class HullSpecifications
    {
        public float length; // meters (LOA - Length Overall)
        public float beam; // meters (maximum width)
        public float displacement; // kg (boat + crew weight)
        public float draft; // meters (depth below waterline)
        public float wettedSurfaceArea; // m² (hull area in contact with water)

        // Center of buoyancy and gravity
        public Vector3 centerOfBuoyancy; // Local position
        public Vector3 centerOfGravity; // Local position

        // Hull shape coefficients
        public float blockCoefficient = 0.45f; // How much of prismatic volume is displaced
        public float prismCoefficient = 0.55f; // Fineness of hull ends
    }

    /// <summary>
    /// Calculates hull hydrodynamics including buoyancy, heel, and drag
    /// </summary>
    public class HullDynamics
    {
        public HullSpecifications specs;

        private float currentHeelAngle = 0f; // degrees
        private float currentPitchAngle = 0f; // degrees
        private float currentTrimAngle = 0f; // fore/aft tilt

        public HullDynamics(HullSpecifications specifications)
        {
            specs = specifications;
        }

        /// <summary>
        /// Calculate buoyancy force and center of buoyancy
        /// </summary>
        public void CalculateBuoyancy(
            Transform boatTransform,
            Environment.WaterSystem waterSystem,
            out Vector3 buoyancyForce,
            out Vector3 buoyancyCenter)
        {
            // Sample multiple points on hull to determine submerged volume
            Vector3 worldCOB = boatTransform.TransformPoint(specs.centerOfBuoyancy);

            float waterHeight = waterSystem.GetWaterHeight(worldCOB);
            float submersion = waterHeight - worldCOB.y;

            if (submersion <= 0f)
            {
                // Hull is completely out of water
                buoyancyForce = Vector3.zero;
                buoyancyCenter = worldCOB;
                return;
            }

            // Calculate submerged volume (simplified)
            float submergedVolume = CalculateSubmergedVolume(submersion);

            // Buoyancy force (Archimedes)
            buoyancyForce = waterSystem.CalculateBuoyancyForce(submergedVolume);

            // Center of buoyancy shifts with heel
            buoyancyCenter = CalculateCenterOfBuoyancy(boatTransform, currentHeelAngle);
        }

        /// <summary>
        /// Calculate hull resistance forces
        /// </summary>
        public Vector3 CalculateHullDrag(
            Vector3 velocity,
            Environment.WaterSystem waterSystem,
            float heelAngle)
        {
            currentHeelAngle = heelAngle;

            // Adjust wetted surface area based on heel
            float effectiveWettedArea = CalculateWettedSurfaceArea(heelAngle);

            return waterSystem.CalculateHullDrag(
                velocity,
                effectiveWettedArea,
                specs.length,
                specs.beam
            );
        }

        /// <summary>
        /// Calculate heel angle from heeling moment
        /// </summary>
        public float CalculateHeelAngle(float heelingMoment, float rightingMoment, float deltaTime)
        {
            // Net moment
            float netMoment = heelingMoment - rightingMoment;

            // Simplified heel dynamics
            float momentOfInertia = specs.displacement * specs.beam * specs.beam / 12f;
            float angularAcceleration = netMoment / momentOfInertia;

            // Update heel angle (with damping)
            float dampingFactor = 0.9f;
            currentHeelAngle += angularAcceleration * deltaTime * dampingFactor;

            // Clamp heel angle (boats will capsize beyond ~90 degrees)
            currentHeelAngle = Mathf.Clamp(currentHeelAngle, -85f, 85f);

            return currentHeelAngle;
        }

        /// <summary>
        /// Calculate righting moment (resistance to heeling)
        /// </summary>
        public float CalculateRightingMoment(Vector3 buoyancyCenter, Transform boatTransform)
        {
            Vector3 worldCOG = boatTransform.TransformPoint(specs.centerOfGravity);

            // Righting moment = weight × horizontal separation between COG and COB
            Vector3 separation = buoyancyCenter - worldCOG;
            float leverArm = separation.x; // Horizontal distance

            float rightingMoment = specs.displacement * PhysicsConstants.GRAVITY * leverArm;

            return rightingMoment;
        }

        /// <summary>
        /// Get current heel angle
        /// </summary>
        public float GetHeelAngle()
        {
            return currentHeelAngle;
        }

        /// <summary>
        /// Set heel angle directly (for initialization)
        /// </summary>
        public void SetHeelAngle(float angle)
        {
            currentHeelAngle = Mathf.Clamp(angle, -85f, 85f);
        }

        /// <summary>
        /// Calculate effect of heel on sail performance
        /// Positive heel can be beneficial in some conditions, but excessive heel reduces performance
        /// </summary>
        public float GetHeelPerformanceFactor()
        {
            float absHeel = Mathf.Abs(currentHeelAngle);

            if (absHeel < 10f)
            {
                // Slight heel can be beneficial (reduces wetted surface)
                return 1.0f + (absHeel / 100f);
            }
            else if (absHeel < 25f)
            {
                // Moderate heel - neutral to slightly negative
                return 1.0f - ((absHeel - 10f) / 100f);
            }
            else
            {
                // Excessive heel - significant performance loss
                float excessHeel = absHeel - 25f;
                return Mathf.Max(0.6f, 1.0f - (excessHeel / 30f));
            }
        }

        private float CalculateSubmergedVolume(float submersionDepth)
        {
            // Simplified volume calculation
            // Full volume = displacement / water density
            float totalVolume = specs.displacement / PhysicsConstants.WATER_DENSITY;

            // Fraction submerged (assuming relatively constant cross-section)
            float submersionFraction = Mathf.Clamp01(submersionDepth / specs.draft);

            return totalVolume * submersionFraction;
        }

        private Vector3 CalculateCenterOfBuoyancy(Transform boatTransform, float heelAngle)
        {
            // COB shifts to leeward side when heeled
            Vector3 localCOB = specs.centerOfBuoyancy;

            // Approximate COB shift
            float heelRad = heelAngle * PhysicsConstants.DEGREES_TO_RADIANS;
            float cobShift = specs.beam * 0.2f * Mathf.Sin(heelRad);

            localCOB.x += cobShift;

            return boatTransform.TransformPoint(localCOB);
        }

        private float CalculateWettedSurfaceArea(float heelAngle)
        {
            // Wetted surface changes with heel
            float absHeel = Mathf.Abs(heelAngle);

            // Slight decrease at small angles, increase at large angles
            if (absHeel < 15f)
            {
                return specs.wettedSurfaceArea * (1f - absHeel / 150f);
            }
            else
            {
                return specs.wettedSurfaceArea * (0.9f + (absHeel - 15f) / 100f);
            }
        }

        /// <summary>
        /// Calculate hull speed (theoretical maximum based on waterline length)
        /// Hull speed ≈ 1.34 × √LWL (in knots)
        /// </summary>
        public float GetHullSpeed()
        {
            float hullSpeedKnots = 1.34f * Mathf.Sqrt(specs.length);
            return PhysicsConstants.KnotsToMetersPerSecond(hullSpeedKnots);
        }

        /// <summary>
        /// Check if boat is planing (29er can plane, ILCA and i420 typically don't)
        /// </summary>
        public bool IsPlaning(float speed)
        {
            float froudeNumber = speed / Mathf.Sqrt(PhysicsConstants.GRAVITY * specs.length);
            return froudeNumber > 0.4f; // Planing threshold
        }
    }
}
