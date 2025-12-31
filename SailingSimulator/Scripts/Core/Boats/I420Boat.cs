using UnityEngine;
using SailingSimulator.Core.Physics;

namespace SailingSimulator.Core.Boats
{
    /// <summary>
    /// i420 (International 420) implementation
    /// Two-person symmetric spinnaker boat with trapeze and track traveler
    /// </summary>
    public class I420Boat : BaseBoat
    {
        [Header("i420 Specifications")]
        [SerializeField] private Transform mainSailTransform;
        [SerializeField] private Transform jibSailTransform;
        [SerializeField] private Transform spinnakerTransform;
        [SerializeField] private Transform boomTransform;
        [SerializeField] private Transform mastTransform;
        [SerializeField] private Transform centerboardTransformRef;
        [SerializeField] private Transform rudderTransformRef;

        [Header("Sail Controls")]
        [SerializeField] private float mainsheetTension = 0.5f;
        [SerializeField] private float jibSheetTension = 0.5f;
        [SerializeField] private float vangTension = 0f;
        [SerializeField] private float cunninghamTension = 0f;
        [SerializeField] private float outhaul = 0.5f;

        [Header("Spinnaker Controls")]
        [SerializeField] private bool spinnakerDeployed = false;
        [SerializeField] private float spinnakerSheet = 0.5f;
        [SerializeField] private float spinnakerGuy = 0.5f;
        [SerializeField] private float poleHeight = 0.5f;

        [Header("Crew")]
        [SerializeField] private bool crewOnTrapeze = false;

        // i420-specific systems
        private SailAerodynamics mainsail;
        private SailAerodynamics jib;
        private SailAerodynamics spinnaker;
        private I420TravelerSystem traveler;
        private TrapezeSystem trapeze;

        // i420 Specifications (real-world data)
        private const float HULL_LENGTH = 4.2f; // meters
        private const float HULL_BEAM = 1.63f; // meters
        private const float HULL_WEIGHT = 100f; // kg
        private const float CREW_WEIGHT = 140f; // kg (2 crew @ 70kg each)
        private const float DISPLACEMENT = HULL_WEIGHT + CREW_WEIGHT;
        private const float MAIN_SAIL_AREA = 7.71f; // m²
        private const float JIB_AREA = 2.97f; // m²
        private const float SPINNAKER_AREA = 13f; // m²
        private const float MAST_HEIGHT = 6.3f; // meters above deck

        protected override void InitializeBoat()
        {
            // Initialize hull specifications
            hullSpecs = new HullSpecifications
            {
                length = HULL_LENGTH,
                beam = HULL_BEAM,
                displacement = DISPLACEMENT,
                draft = 1.1f,
                wettedSurfaceArea = 5.2f,
                centerOfBuoyancy = new Vector3(0f, -0.35f, 0f),
                centerOfGravity = new Vector3(0f, -0.25f, 0.1f)
            };

            hullDynamics = new HullDynamics(hullSpecs);

            // Initialize sails (Dacron material for i420)
            mainsail = new SailAerodynamics(
                SailType.Mainsail,
                SailMaterial.Dacron,
                MAIN_SAIL_AREA
            );

            jib = new SailAerodynamics(
                SailType.Jib,
                SailMaterial.Dacron,
                JIB_AREA
            );

            spinnaker = new SailAerodynamics(
                SailType.SymmetricSpinnaker,
                SailMaterial.Dacron,
                SPINNAKER_AREA
            );

            // Initialize centerboard
            centerboard = new CenterboardDynamics(
                0.45f, // area
                2.8f,  // aspect ratio
                1.1f   // max depth
            );

            // Initialize rudder
            rudder = new RudderDynamics(0.2f, 3.2f);

            // Initialize i420 track traveler system
            traveler = new I420TravelerSystem();

            // Initialize trapeze (attached to mast)
            Vector3 mastAttachment = new Vector3(0f, 4.0f, 0.3f);
            trapeze = new TrapezeSystem(mastAttachment, 70f); // Crew weight

            // Initialize crew (2 people: skipper and crew)
            crewSystem = new CrewWeightSystem(2, HULL_LENGTH, HULL_BEAM);
            crewSystem.InitializeCrew(
                ("Skipper", 70f),
                ("Crew", 70f)
            );

            Debug.Log("i420 initialized - Two-person boat with jib, symmetric spinnaker, trapeze, and track traveler");
        }

        protected override void CalculateSailForces(out Vector3 force, out Vector3 torque)
        {
            force = Vector3.zero;
            torque = Vector3.zero;

            // Mainsail
            UpdateMainsailForces(out Vector3 mainForce, out Vector3 mainTorque);
            force += mainForce;
            torque += mainTorque;

            // Jib (always deployed on i420)
            UpdateJibForces(out Vector3 jibForce, out Vector3 jibTorque);
            force += jibForce;
            torque += jibTorque;

            // Spinnaker (if deployed)
            if (spinnakerDeployed)
            {
                UpdateSpinnakerForces(out Vector3 spinForce, out Vector3 spinTorque);
                force += spinForce;
                torque += spinTorque;
            }

            // Apply heel performance factor
            float heelFactor = hullDynamics.GetHeelPerformanceFactor();
            force *= heelFactor;
        }

        private void UpdateMainsailForces(out Vector3 force, out Vector3 torque)
        {
            mainsail.sheetTension = mainsheetTension;
            mainsail.vangTension = vangTension;
            mainsail.cunninghamTension = cunninghamTension;
            mainsail.outhaul = outhaul;

            float boomAngle = CalculateBoomAngle();
            Quaternion boomRotation = Quaternion.AngleAxis(boomAngle, Vector3.up);
            Vector3 sailNormal = boomRotation * transform.right;

            mainsail.CalculateForces(
                apparentWind,
                sailNormal,
                boomTransform.forward,
                MAST_HEIGHT * 0.45f,
                out Vector3 lift,
                out Vector3 drag
            );

            force = lift + drag;

            Vector3 coe = transform.position + Vector3.up * (MAST_HEIGHT * 0.45f);
            Vector3 leverArm = coe - rb.worldCenterOfMass;
            torque = Vector3.Cross(leverArm, force);
        }

        private void UpdateJibForces(out Vector3 force, out Vector3 torque)
        {
            jib.sheetTension = jibSheetTension;

            // Jib angle (typically sheeted tighter than main)
            float apparentWindAngle = GetApparentWindAngle();
            float jibAngle = jib.CalculateOptimalTrimAngle(Mathf.Abs(apparentWindAngle));

            Quaternion jibRotation = Quaternion.AngleAxis(jibAngle, Vector3.up);
            Vector3 jibNormal = jibRotation * transform.right;

            jib.CalculateForces(
                apparentWind,
                jibNormal,
                transform.forward,
                MAST_HEIGHT * 0.35f,
                out Vector3 lift,
                out Vector3 drag
            );

            force = lift + drag;

            Vector3 coe = transform.position + Vector3.up * (MAST_HEIGHT * 0.35f) + transform.forward * 1.2f;
            Vector3 leverArm = coe - rb.worldCenterOfMass;
            torque = Vector3.Cross(leverArm, force);
        }

        private void UpdateSpinnakerForces(out Vector3 force, out Vector3 torque)
        {
            spinnaker.sheetTension = spinnakerSheet;

            // Spinnaker operates at broad angles (running/reaching)
            float spinAngle = Mathf.Lerp(60f, 100f, spinnakerGuy);

            Quaternion spinRotation = Quaternion.AngleAxis(spinAngle, Vector3.up);
            Vector3 spinNormal = spinRotation * transform.right;

            spinnaker.CalculateForces(
                apparentWind,
                spinNormal,
                transform.forward,
                MAST_HEIGHT * 0.55f,
                out Vector3 lift,
                out Vector3 drag
            );

            force = lift + drag;

            Vector3 coe = transform.position + Vector3.up * (MAST_HEIGHT * 0.55f) + transform.forward * 1.5f;
            Vector3 leverArm = coe - rb.worldCenterOfMass;
            torque = Vector3.Cross(leverArm, force);
        }

        private float CalculateBoomAngle()
        {
            float baseAngle = Mathf.Lerp(80f, 10f, mainsheetTension);
            float travelerModifier = traveler.GetMainsheetAngleModifier();
            return baseAngle + travelerModifier;
        }

        protected override float CalculateRightingMoment(Vector3 buoyancyCenter)
        {
            float hullRighting = hullDynamics.CalculateRightingMoment(buoyancyCenter, transform);

            float crewRighting = 0f;
            if (crewSystem != null)
            {
                crewRighting = crewSystem.CalculateTotalRightingMoment(hullSpecs.centerOfGravity);
            }

            // Add trapeze righting moment if crew is on wire
            float trapezeRighting = 0f;
            if (crewOnTrapeze && trapeze != null)
            {
                trapeze.isActive = true;
                trapezeRighting = trapeze.CalculateRightingMoment(currentHeelAngle);
            }
            else if (trapeze != null)
            {
                trapeze.isActive = false;
            }

            return hullRighting + crewRighting + trapezeRighting;
        }

        // Control methods

        public void SetMainsheetTension(float tension)
        {
            mainsheetTension = Mathf.Clamp01(tension);
        }

        public void SetJibSheetTension(float tension)
        {
            jibSheetTension = Mathf.Clamp01(tension);
        }

        public void SetVangTension(float tension)
        {
            vangTension = Mathf.Clamp01(tension);
        }

        public void AdjustTraveler(float input)
        {
            traveler.Update(input, Time.deltaTime);
        }

        public void DeploySpinnaker(bool deploy)
        {
            spinnakerDeployed = deploy;
        }

        public void SetSpinnakerSheet(float tension)
        {
            spinnakerSheet = Mathf.Clamp01(tension);
        }

        public void SetSpinnakerGuy(float position)
        {
            spinnakerGuy = Mathf.Clamp01(position);
        }

        public void SetCrewOnTrapeze(bool onTrapeze)
        {
            crewOnTrapeze = onTrapeze;
        }

        public void SetTrapezeHeight(float height)
        {
            if (trapeze != null)
            {
                trapeze.SetRingHeight(height);
            }
        }

        public void SetSkipperPosition(float athwartships, float foreAft)
        {
            if (crewSystem.crew.Length > 0)
            {
                crewSystem.crew[0].athwartshipsPosition = Mathf.Clamp(athwartships, -1f, 1f);
                crewSystem.crew[0].foreAftPosition = Mathf.Clamp(foreAft, -1f, 1f);
                crewSystem.UpdateCrewPositions();
            }
        }

        public void SetCrewPosition(float athwartships, float foreAft)
        {
            if (crewSystem.crew.Length > 1)
            {
                crewSystem.crew[1].athwartshipsPosition = Mathf.Clamp(athwartships, -1f, 1f);
                crewSystem.crew[1].foreAftPosition = Mathf.Clamp(foreAft, -1f, 1f);
                crewSystem.UpdateCrewPositions();
            }
        }

        // Required implementations
        protected override float GetMastHeight() => MAST_HEIGHT;
        protected override float GetCenterOfEffortHeight() => MAST_HEIGHT * 0.45f;
        protected override Transform GetCenterboardTransform() => centerboardTransformRef;
        protected override Transform GetRudderTransform() => rudderTransformRef;

        // Getters
        public float GetTravelerPosition() => traveler.position;
        public bool IsSpinnakerDeployed() => spinnakerDeployed;
        public bool IsCrewOnTrapeze() => crewOnTrapeze;
    }
}
