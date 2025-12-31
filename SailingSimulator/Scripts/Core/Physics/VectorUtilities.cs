using UnityEngine;

namespace SailingSimulator.Core.Physics
{
    /// <summary>
    /// Utility functions for sailing physics vector calculations
    /// </summary>
    public static class VectorUtilities
    {
        /// <summary>
        /// Calculate apparent wind from true wind and boat velocity
        /// Apparent Wind = True Wind - Boat Velocity
        /// </summary>
        public static Vector3 CalculateApparentWind(Vector3 trueWind, Vector3 boatVelocity)
        {
            return trueWind - boatVelocity;
        }

        /// <summary>
        /// Calculate the angle between boat heading and wind direction
        /// Returns angle in degrees (0-180)
        /// </summary>
        public static float CalculateWindAngle(Vector3 boatForward, Vector3 windDirection)
        {
            float angle = Vector3.Angle(boatForward, windDirection);
            return angle;
        }

        /// <summary>
        /// Calculate signed angle to wind (-180 to 180)
        /// Negative = wind from port, Positive = wind from starboard
        /// </summary>
        public static float CalculateSignedWindAngle(Vector3 boatForward, Vector3 boatRight, Vector3 windDirection)
        {
            float angle = Vector3.Angle(boatForward, windDirection);
            float sign = Mathf.Sign(Vector3.Dot(boatRight, windDirection));
            return angle * sign;
        }

        /// <summary>
        /// Calculate Velocity Made Good (VMG) toward a target direction
        /// </summary>
        public static float CalculateVMG(Vector3 velocity, Vector3 targetDirection)
        {
            return Vector3.Dot(velocity, targetDirection.normalized);
        }

        /// <summary>
        /// Project a force onto a plane defined by its normal
        /// </summary>
        public static Vector3 ProjectOntoPlane(Vector3 force, Vector3 planeNormal)
        {
            return force - Vector3.Dot(force, planeNormal) * planeNormal;
        }

        /// <summary>
        /// Calculate lift and drag forces from a foil (sail, centerboard, rudder)
        /// </summary>
        public static void CalculateLiftDrag(
            Vector3 flowVelocity,
            Vector3 foilNormal,
            float area,
            float liftCoefficient,
            float dragCoefficient,
            float fluidDensity,
            out Vector3 lift,
            out Vector3 drag)
        {
            float dynamicPressure = PhysicsConstants.DynamicPressure(flowVelocity.magnitude, fluidDensity);

            // Lift is perpendicular to flow
            Vector3 liftDirection = Vector3.Cross(flowVelocity, Vector3.Cross(flowVelocity, foilNormal)).normalized;
            lift = liftDirection * (liftCoefficient * dynamicPressure * area);

            // Drag is opposite to flow
            drag = -flowVelocity.normalized * (dragCoefficient * dynamicPressure * area);
        }

        /// <summary>
        /// Calculate lift coefficient using thin airfoil theory approximation
        /// cl = 2π * sin(α) for small angles
        /// </summary>
        public static float CalculateLiftCoefficient(float angleOfAttackDegrees, float maxLiftCoefficient = 1.5f)
        {
            float angleRad = angleOfAttackDegrees * PhysicsConstants.DEGREES_TO_RADIANS;

            // Linear region (up to ~15 degrees)
            float linearCl = 2f * Mathf.PI * Mathf.Sin(angleRad);

            // Stall beyond critical angle (~15-20 degrees for sails)
            float criticalAngle = 15f;
            if (Mathf.Abs(angleOfAttackDegrees) > criticalAngle)
            {
                // Post-stall: reduced lift
                float stallFactor = Mathf.Clamp01(1f - (Mathf.Abs(angleOfAttackDegrees) - criticalAngle) / 30f);
                linearCl *= stallFactor;
            }

            return Mathf.Clamp(linearCl, -maxLiftCoefficient, maxLiftCoefficient);
        }

        /// <summary>
        /// Calculate drag coefficient (includes induced drag)
        /// cd = cd0 + k * cl^2
        /// </summary>
        public static float CalculateDragCoefficient(float liftCoefficient, float baseDragCoefficient, float inducedDragFactor = 0.05f)
        {
            return baseDragCoefficient + inducedDragFactor * liftCoefficient * liftCoefficient;
        }

        /// <summary>
        /// Smooth angle interpolation handling wraparound
        /// </summary>
        public static float SmoothAngle(float current, float target, float smoothTime, float deltaTime)
        {
            float delta = Mathf.DeltaAngle(current, target);
            return current + delta * Mathf.Clamp01(deltaTime / smoothTime);
        }
    }
}
