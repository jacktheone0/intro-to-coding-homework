using UnityEngine;

namespace SailingSimulator.Core.Physics
{
    /// <summary>
    /// Calculates forces from underwater foils (centerboard, daggerboard, rudder)
    /// </summary>
    public class FoilDynamics
    {
        // Foil properties
        public float area; // m²
        public float aspectRatio; // span² / area
        public float maxAngleOfAttack = 20f; // degrees before stall

        public FoilDynamics(float foilArea, float foilAspectRatio)
        {
            area = foilArea;
            aspectRatio = foilAspectRatio;
        }

        /// <summary>
        /// Calculate lift and drag forces from a foil
        /// </summary>
        public void CalculateForces(
            Vector3 waterFlow,
            Vector3 foilNormal,
            Vector3 foilUp,
            float depth,
            out Vector3 lift,
            out Vector3 drag)
        {
            lift = Vector3.zero;
            drag = Vector3.zero;

            float flowSpeed = waterFlow.magnitude;
            if (flowSpeed < 0.1f || depth < 0.01f)
                return;

            // Calculate angle of attack
            Vector3 flowDirection = waterFlow.normalized;
            float angleOfAttack = Vector3.SignedAngle(flowDirection, foilNormal, foilUp);

            // Get lift and drag coefficients
            float cl = VectorUtilities.CalculateLiftCoefficient(angleOfAttack, GetMaxLiftCoefficient());
            float cd = VectorUtilities.CalculateDragCoefficient(cl, PhysicsConstants.FOIL_DRAG_COEFFICIENT);

            // Apply depth factor (foils less effective near surface)
            float depthFactor = Mathf.Clamp01(depth / 0.5f);

            // Calculate forces
            VectorUtilities.CalculateLiftDrag(
                waterFlow,
                foilNormal,
                area * depthFactor,
                cl,
                cd,
                PhysicsConstants.WATER_DENSITY,
                out lift,
                out drag
            );
        }

        /// <summary>
        /// Get maximum lift coefficient based on foil type and aspect ratio
        /// </summary>
        private float GetMaxLiftCoefficient()
        {
            // Higher aspect ratio foils are more efficient
            return 1.2f + (aspectRatio / 10f);
        }

        /// <summary>
        /// Calculate induced drag from finite span (3D effects)
        /// </summary>
        private float CalculateInducedDragFactor()
        {
            // Induced drag: CDi = CL² / (π * AR * e)
            // where e is Oswald efficiency factor (~0.9 for foils)
            return 1f / (Mathf.PI * aspectRatio * 0.9f);
        }
    }

    /// <summary>
    /// Rudder-specific foil with steering angle
    /// </summary>
    public class RudderDynamics : FoilDynamics
    {
        public float steeringAngle; // Current rudder angle in degrees

        public RudderDynamics(float foilArea, float foilAspectRatio) : base(foilArea, foilAspectRatio)
        {
        }

        /// <summary>
        /// Calculate rudder forces including steering deflection
        /// </summary>
        public void CalculateRudderForces(
            Vector3 waterFlow,
            Transform rudderTransform,
            float depth,
            out Vector3 lift,
            out Vector3 drag)
        {
            // Apply steering angle to rudder orientation
            Quaternion steeringRotation = Quaternion.AngleAxis(steeringAngle, rudderTransform.up);
            Vector3 rudderNormal = steeringRotation * rudderTransform.right;

            CalculateForces(waterFlow, rudderNormal, rudderTransform.up, depth, out lift, out drag);
        }
    }

    /// <summary>
    /// Centerboard/daggerboard with adjustable depth
    /// </summary>
    public class CenterboardDynamics : FoilDynamics
    {
        public float maxDepth = 1.5f; // Maximum extension depth
        public float currentExtension = 1.0f; // 0-1, how much is deployed

        public CenterboardDynamics(float foilArea, float foilAspectRatio, float maxDepth) : base(foilArea, foilAspectRatio)
        {
            this.maxDepth = maxDepth;
        }

        /// <summary>
        /// Calculate centerboard forces with variable deployment
        /// </summary>
        public void CalculateCenterboardForces(
            Vector3 waterFlow,
            Transform centerboardTransform,
            float hullDepth,
            out Vector3 lift,
            out Vector3 drag)
        {
            // Effective depth based on deployment
            float effectiveDepth = maxDepth * currentExtension;
            float submersedDepth = Mathf.Min(effectiveDepth, hullDepth);

            // Effective area scales with deployment
            float originalArea = area;
            area = originalArea * currentExtension;

            CalculateForces(
                waterFlow,
                centerboardTransform.right,
                centerboardTransform.up,
                submersedDepth,
                out lift,
                out drag
            );

            // Restore original area
            area = originalArea;
        }

        /// <summary>
        /// Set centerboard extension (0 = fully raised, 1 = fully lowered)
        /// </summary>
        public void SetExtension(float extension)
        {
            currentExtension = Mathf.Clamp01(extension);
        }
    }
}
