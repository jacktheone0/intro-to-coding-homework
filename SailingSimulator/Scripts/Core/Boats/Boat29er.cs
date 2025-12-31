using UnityEngine;
using SailingSimulator.Core.Physics;

namespace SailingSimulator.Core.Boats
{
    /// <summary>
    /// 29er (International 29er) implementation
    /// Two-person asymmetric spinnaker skiff with trapeze and continuous-line traveler
    /// High-performance planing boat with no backstay
    /// </summary>
    public class Boat29er : BaseBoat
    {
        [Header("29er Specifications")]
        [SerializeField] private Transform mainSailTransform;
        [SerializeField] private Transform jibSailTransform;
        [SerializeField] private Transform asymSpinnakerTransform;
        [SerializeField] private Transform boomTransform;
        [SerializeField] private Transform mastTransform;
        [SerializeField] private Transform daggerboardTransformRef;
        [SerializeField] private Transform rudderTransformRef;

        [Header("Sail Controls")]
        [SerializeField] private float mainsheetTension = 0.5f;
        [SerializeField] private float jibSheetTension = 0.5f;
        [SerializeField] private float vangTension = 0f;
        [SerializeField] private float cunninghamTension = 0f;
        [SerializeField] private float outhaul = 0.5f;

        [Header("Asymmetric Spinnaker Controls")]
        [SerializeField] private bool asymSpinnakerDeployed = false;
        [SerializeField] private float asymSpinnakerSheet = 0.5f;
        [SerializeField] private float asymSpinnakerTack = 0.5f; // No pole - tack line controls

        [Header("Crew")]
        [SerializeField] private bool crewOnTrapeze = false;
        [SerializeField] private bool skipperOnTrapeze = false; // 29er: both can trapeze!

        // 29er-specific systems
        private SailAerodynamics mainsail;
        private SailAerodynamics jib;
        private SailAerodynamics asymSpinnaker;
        private Traveler29erSystem traveler;
        private TrapezeSystem crewTrapeze;
        private TrapezeSystem skipperTrapeze;

        // 29er Specifications (real-world data)
        private const float HULL_LENGTH = 4.45f; // meters
        private const float HULL_BEAM = 1.77f; // meters
        private const float HULL_WEIGHT = 70f; // kg (very light!)
        private const float CREW_WEIGHT = 130f; // kg (lighter crew for skiff)
        private const float DISPLACEMENT = HULL_WEIGHT + CREW_WEIGHT;
        private const float MAIN_SAIL_AREA = 9.0f; // m²
        private const float JIB_AREA = 3.5f; // m²
        private const float ASYM_SPINNAKER_AREA = 14.5f; // m²
        private const float MAST_HEIGHT = 7.8f; // meters above deck

        // Skiff characteristics
        private bool isPlaning = false;
        private float planingSpeedThreshold = 12f; // knots

        protected override void InitializeBoat()
        {
            // Initialize hull specifications (skiff design)
            hullSpecs = new HullSpecifications
            {
                length = HULL_LENGTH,
                beam = HULL_BEAM,
                displacement = DISPLACEMENT,
                draft = 0.9f, // Shallower than i420
                wettedSurfaceArea = 4.8f, // Less wetted surface for speed
                centerOfBuoyancy = new Vector3(0f, -0.3f, 0f),
                centerOfGravity = new Vector3(0f, -0.2f, -0.1f) // Slightly aft
            };

            hullDynamics = new HullDynamics(hullSpecs);

            // Initialize sails (Laminate material for 29er - high performance)
            mainsail = new SailAerodynamics(
                SailType.Mainsail,
                SailMaterial.Laminate,
                MAIN_SAIL_AREA
            );

            jib = new SailAerodynamics(
                SailType.Jib,
                SailMaterial.Laminate,
                JIB_AREA
            );

            asymSpinnaker = new SailAerodynamics(
                SailType.AsymmetricSpinnaker,
                SailMaterial.Laminate,
                ASYM_SPINNAKER_AREA
            );

            // Initialize daggerboard (note: daggerboard, not centerboard - fully up/down)
            centerboard = new CenterboardDynamics(
                0.4f, // area
                3.2f,  // higher aspect ratio for efficiency
                0.9f   // max depth
            );

            // Initialize rudder (larger for control at speed)
            rudder = new RudderDynamics(0.25f, 3.5f);

            // Initialize 29er continuous-line traveler system
            traveler = new Traveler29erSystem();

            // Initialize trapeze systems (both crew can trapeze on 29er)
            Vector3 mastAttachment = new Vector3(0f, 5.0f, 0.2f);
            crewTrapeze = new TrapezeSystem(mastAttachment, 65f);
            skipperTrapeze = new TrapezeSystem(mastAttachment, 65f);

            // Initialize crew (2 people: skipper and crew)
            crewSystem = new CrewWeightSystem(2, HULL_LENGTH, HULL_BEAM);
            crewSystem.InitializeCrew(
                ("Skipper", 65f),
                ("Crew", 65f)
            );

            Debug.Log("29er initialized - High-performance skiff with asymmetric spinnaker, dual trapeze, and continuous-line traveler");
        }

        protected override void UpdatePhysics(float deltaTime)
        {
            // Check if planing
            isPlaning = hullDynamics.IsPlaning(rb.velocity.magnitude);

            base.UpdatePhysics(deltaTime);

            // Skiff-specific dynamics
            if (isPlaning)
            {
                // Reduce hull drag when planing (hull lifts out of water)
                ApplyPlaningEffects();
            }
        }

        private void ApplyPlaningEffects()
        {
            // When planing, effective wetted surface reduces dramatically
            // This is handled in hull dynamics, but we can add additional effects

            // Slightly raise center of gravity (hull riding on plane)
            // This affects stability
        }

        protected override void CalculateSailForces(out Vector3 force, out Vector3 torque)
        {
            force = Vector3.zero;
            torque = Vector3.zero;

            // Mainsail
            UpdateMainsailForces(out Vector3 mainForce, out Vector3 mainTorque);
            force += mainForce;
            torque += mainTorque;

            // Jib
            UpdateJibForces(out Vector3 jibForce, out Vector3 jibTorque);
            force += jibForce;
            torque += jibTorque;

            // Asymmetric Spinnaker (if deployed)
            if (asymSpinnakerDeployed)
            {
                UpdateAsymSpinnakerForces(out Vector3 spinForce, out Vector3 spinTorque);
                force += spinForce;
                torque += spinTorque;
            }

            // Apply heel performance factor
            float heelFactor = hullDynamics.GetHeelPerformanceFactor();
            force *= heelFactor;

            // Skiffs are more sensitive to heel - apply additional factor
            if (Mathf.Abs(currentHeelAngle) > 20f)
            {
                force *= 0.85f; // Significant power loss when over-heeled
            }
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

            Vector3 coe = transform.position + Vector3.up * (MAST_HEIGHT * 0.35f) + transform.forward * 1.3f;
            Vector3 leverArm = coe - rb.worldCenterOfMass;
            torque = Vector3.Cross(leverArm, force);
        }

        private void UpdateAsymSpinnakerForces(out Vector3 force, out Vector3 torque)
        {
            asymSpinnaker.sheetTension = asymSpinnakerSheet;

            // Asymmetric spinnaker: tack is fixed to bow, flies off to leeward
            // Angle controlled by sheet and tack line
            float spinAngle = Mathf.Lerp(45f, 90f, 1f - asymSpinnakerSheet);

            Quaternion spinRotation = Quaternion.AngleAxis(spinAngle, Vector3.up);
            Vector3 spinNormal = spinRotation * transform.right;

            asymSpinnaker.CalculateForces(
                apparentWind,
                spinNormal,
                transform.forward,
                MAST_HEIGHT * 0.6f,
                out Vector3 lift,
                out Vector3 drag
            );

            force = lift + drag;

            // Tack is at bow, clew is outboard
            Vector3 coe = transform.position + Vector3.up * (MAST_HEIGHT * 0.6f) + transform.forward * 2.0f;
            Vector3 leverArm = coe - rb.worldCenterOfMass;
            torque = Vector3.Cross(leverArm, force);
        }

        private float CalculateBoomAngle()
        {
            float baseAngle = Mathf.Lerp(80f, 10f, mainsheetTension);
            float travelerModifier = traveler.GetMainsheetAngleModifier();

            // Dynamic traveler adjustment for planing conditions
            if (isPlaning)
            {
                float dynamicPosition = traveler.GetDynamicPosition(
                    boatSpeedKnots,
                    currentHeelAngle,
                    GetApparentWindAngle()
                );
                // Blend toward dynamic position
                traveler.SetPosition(Mathf.Lerp(traveler.position, dynamicPosition, Time.deltaTime * 2f));
            }

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

            // Trapeze righting moments
            float trapezeRighting = 0f;

            if (crewOnTrapeze && crewTrapeze != null)
            {
                crewTrapeze.isActive = true;
                trapezeRighting += crewTrapeze.CalculateRightingMoment(currentHeelAngle);
            }
            else if (crewTrapeze != null)
            {
                crewTrapeze.isActive = false;
            }

            if (skipperOnTrapeze && skipperTrapeze != null)
            {
                skipperTrapeze.isActive = true;
                trapezeRighting += skipperTrapeze.CalculateRightingMoment(currentHeelAngle);
            }
            else if (skipperTrapeze != null)
            {
                skipperTrapeze.isActive = false;
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

        public void AdjustTraveler(float input)
        {
            // Release cam cleat to adjust
            if (Mathf.Abs(input) > 0.01f)
            {
                traveler.ReleaseCamCleat();
                traveler.Update(input, Time.deltaTime);
            }
            else
            {
                traveler.EngageCamCleat();
            }
        }

        public void DeployAsymSpinnaker(bool deploy)
        {
            asymSpinnakerDeployed = deploy;
        }

        public void SetAsymSpinnakerSheet(float tension)
        {
            asymSpinnakerSheet = Mathf.Clamp01(tension);
        }

        public void SetAsymSpinnakerTack(float position)
        {
            asymSpinnakerTack = Mathf.Clamp01(position);
        }

        public void SetCrewOnTrapeze(bool onTrapeze)
        {
            crewOnTrapeze = onTrapeze;
        }

        public void SetSkipperOnTrapeze(bool onTrapeze)
        {
            skipperOnTrapeze = onTrapeze;
        }

        public void SetDaggerboardExtension(float extension)
        {
            SetCenterboardExtension(extension);
        }

        // Required implementations
        protected override float GetMastHeight() => MAST_HEIGHT;
        protected override float GetCenterOfEffortHeight() => MAST_HEIGHT * 0.5f;
        protected override Transform GetCenterboardTransform() => daggerboardTransformRef;
        protected override Transform GetRudderTransform() => rudderTransformRef;

        // Getters
        public float GetTravelerPosition() => traveler.position;
        public bool IsAsymSpinnakerDeployed() => asymSpinnakerDeployed;
        public bool IsPlaning() => isPlaning;
        public bool IsCrewOnTrapeze() => crewOnTrapeze;
        public bool IsSkipperOnTrapeze() => skipperOnTrapeze;
    }
}
