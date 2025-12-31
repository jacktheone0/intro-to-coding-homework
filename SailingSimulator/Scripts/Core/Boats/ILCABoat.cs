using UnityEngine;
using SailingSimulator.Core.Physics;

namespace SailingSimulator.Core.Boats
{
    /// <summary>
    /// ILCA (Laser) Full Rig implementation
    /// Single-handed, single-sail boat with simple bridle traveler
    /// </summary>
    public class ILCABoat : BaseBoat
    {
        [Header("ILCA Specifications")]
        [SerializeField] private Transform mainSailTransform;
        [SerializeField] private Transform boomTransform;
        [SerializeField] private Transform mastTransform;
        [SerializeField] private Transform centerboardTransformRef;
        [SerializeField] private Transform rudderTransformRef;

        [Header("Sail Controls")]
        [SerializeField] private float mainsheetTension = 0.5f; // 0-1
        [SerializeField] private float vangTension = 0f; // 0-1
        [SerializeField] private float cunninghamTension = 0f; // 0-1
        [SerializeField] private float outhaul = 0.5f; // 0-1

        // ILCA-specific systems
        private SailAerodynamics mainsail;
        private ILCATravelerSystem traveler;

        // ILCA Full Rig Specifications (real-world data)
        private const float HULL_LENGTH = 4.23f; // meters
        private const float HULL_BEAM = 1.39f; // meters
        private const float HULL_WEIGHT = 59f; // kg
        private const float SAILOR_WEIGHT = 80f; // kg (typical for full rig)
        private const float DISPLACEMENT = HULL_WEIGHT + SAILOR_WEIGHT;
        private const float SAIL_AREA = 7.06f; // m²
        private const float MAST_HEIGHT = 8.03f; // meters above deck

        protected override void InitializeBoat()
        {
            // Initialize hull specifications
            hullSpecs = new HullSpecifications
            {
                length = HULL_LENGTH,
                beam = HULL_BEAM,
                displacement = DISPLACEMENT,
                draft = 0.8f,
                wettedSurfaceArea = 4.5f,
                centerOfBuoyancy = new Vector3(0f, -0.3f, 0f),
                centerOfGravity = new Vector3(0f, -0.2f, 0f)
            };

            // Initialize hull dynamics
            hullDynamics = new HullDynamics(hullSpecs);

            // Initialize mainsail (Mylar material for ILCA)
            mainsail = new SailAerodynamics(
                SailType.Mainsail,
                SailMaterial.Mylar,
                SAIL_AREA
            );

            // Initialize centerboard
            centerboard = new CenterboardDynamics(
                0.35f, // area in m²
                2.5f,  // aspect ratio
                0.8f   // max depth
            );

            // Initialize rudder
            rudder = new RudderDynamics(
                0.15f, // area in m²
                3.0f   // aspect ratio
            );

            // Initialize ILCA traveler system
            traveler = new ILCATravelerSystem();

            // Initialize crew (single-handed)
            crewSystem = new CrewWeightSystem(1, HULL_LENGTH, HULL_BEAM);
            crewSystem.InitializeCrew(("Sailor", SAILOR_WEIGHT));

            Debug.Log("ILCA Laser Full Rig initialized - Single-handed sailing boat with Mylar sail");
        }

        protected override void CalculateSailForces(out Vector3 force, out Vector3 torque)
        {
            force = Vector3.zero;
            torque = Vector3.zero;

            // Update sail controls
            mainsail.sheetTension = mainsheetTension;
            mainsail.vangTension = vangTension;
            mainsail.cunninghamTension = cunninghamTension;
            mainsail.outhaul = outhaul;

            // Calculate boom angle including traveler effect
            float boomAngle = CalculateBoomAngle();

            // Sail normal direction
            Quaternion boomRotation = Quaternion.AngleAxis(boomAngle, Vector3.up);
            Vector3 sailNormal = boomRotation * transform.right;

            // Calculate sail forces at center of effort height
            float centerOfEffortHeight = GetCenterOfEffortHeight();

            mainsail.CalculateForces(
                apparentWind,
                sailNormal,
                boomTransform.forward,
                centerOfEffortHeight,
                out Vector3 sailLift,
                out Vector3 sailDrag
            );

            force = sailLift + sailDrag;

            // Calculate torque (turning moment from sail)
            Vector3 centerOfEffort = transform.position + Vector3.up * centerOfEffortHeight;
            Vector3 leverArm = centerOfEffort - rb.worldCenterOfMass;
            torque = Vector3.Cross(leverArm, force);

            // Apply heel performance factor
            float heelFactor = hullDynamics.GetHeelPerformanceFactor();
            force *= heelFactor;
        }

        private float CalculateBoomAngle()
        {
            // Base angle from mainsheet tension
            float baseAngle = Mathf.Lerp(80f, 10f, mainsheetTension);

            // Traveler modifies the angle
            float travelerModifier = traveler.GetMainsheetAngleModifier();

            // Calculate optimal trim for current conditions
            float apparentWindAngle = GetApparentWindAngle();
            float optimalTrim = mainsail.CalculateOptimalTrimAngle(Mathf.Abs(apparentWindAngle));

            // Blend between manual control and optimal
            float finalAngle = baseAngle + travelerModifier;

            return finalAngle;
        }

        // Control methods

        /// <summary>
        /// Set mainsheet tension (0 = eased, 1 = tight)
        /// </summary>
        public void SetMainsheetTension(float tension)
        {
            mainsheetTension = Mathf.Clamp01(tension);
        }

        /// <summary>
        /// Set vang tension (boom downward force)
        /// </summary>
        public void SetVangTension(float tension)
        {
            vangTension = Mathf.Clamp01(tension);
        }

        /// <summary>
        /// Set cunningham tension (luff tension)
        /// </summary>
        public void SetCunninghamTension(float tension)
        {
            cunninghamTension = Mathf.Clamp01(tension);
        }

        /// <summary>
        /// Set outhaul tension (foot tension)
        /// </summary>
        public void SetOuthaul(float tension)
        {
            outhaul = Mathf.Clamp01(tension);
        }

        /// <summary>
        /// Adjust traveler position
        /// </summary>
        public void AdjustTraveler(float input)
        {
            traveler.Update(input, Time.deltaTime);
        }

        /// <summary>
        /// Set crew weight position
        /// </summary>
        public void SetCrewPosition(float athwartships, float foreAft)
        {
            if (crewSystem.crew.Length > 0)
            {
                crewSystem.crew[0].athwartshipsPosition = Mathf.Clamp(athwartships, -1f, 1f);
                crewSystem.crew[0].foreAftPosition = Mathf.Clamp(foreAft, -1f, 1f);
                crewSystem.UpdateCrewPositions();
            }
        }

        /// <summary>
        /// Apply tuning preset for conditions
        /// </summary>
        public void ApplyTuningPreset(WindCondition condition)
        {
            switch (condition)
            {
                case WindCondition.Light: // < 8 knots
                    vangTension = 0.1f;
                    cunninghamTension = 0f;
                    outhaul = 0.3f; // Fuller sail
                    centerboardExtension = 1.0f; // Full down
                    break;

                case WindCondition.Medium: // 8-15 knots
                    vangTension = 0.4f;
                    cunninghamTension = 0.2f;
                    outhaul = 0.5f;
                    centerboardExtension = 1.0f;
                    break;

                case WindCondition.Heavy: // > 15 knots
                    vangTension = 0.8f;
                    cunninghamTension = 0.6f;
                    outhaul = 0.8f; // Flatter sail
                    centerboardExtension = 0.7f; // Can raise slightly in heavy air
                    break;
            }
        }

        // Required abstract method implementations

        protected override float GetMastHeight()
        {
            return MAST_HEIGHT;
        }

        protected override float GetCenterOfEffortHeight()
        {
            // Center of effort is roughly 40% up the mast for a triangular sail
            return MAST_HEIGHT * 0.4f;
        }

        protected override Transform GetCenterboardTransform()
        {
            return centerboardTransformRef;
        }

        protected override Transform GetRudderTransform()
        {
            return rudderTransformRef;
        }

        // Getters for UI

        public float GetMainsheetTension() => mainsheetTension;
        public float GetVangTension() => vangTension;
        public float GetCunninghamTension() => cunninghamTension;
        public float GetOuthaul() => outhaul;
        public float GetTravelerPosition() => traveler.position;
        public bool IsSailLuffing() => mainsail.isLuffing;
        public float GetLuffAmount() => mainsail.luffAmount;

        // Visualization
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (!Application.isPlaying)
                return;

            // Draw sail force
            if (mainsail != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 coe = transform.position + Vector3.up * GetCenterOfEffortHeight();
                // Sail force visualization would go here
            }

            // Draw traveler position
            if (traveler != null)
            {
                Gizmos.color = Color.magenta;
                Vector3 travelerPos = traveler.GetWorldPosition(transform, new Vector3(0f, 0.3f, -1.5f));
                Gizmos.DrawSphere(travelerPos, 0.1f);
            }
        }
    }

    public enum WindCondition
    {
        Light,
        Medium,
        Heavy
    }
}
