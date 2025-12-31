using UnityEngine;
using SailingSimulator.Core.Physics;
using SailingSimulator.Core.Environment;

namespace SailingSimulator.Core.Boats
{
    /// <summary>
    /// Abstract base class for all sailing boats
    /// Integrates all physics systems and provides common functionality
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public abstract class BaseBoat : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected WindSystem windSystem;
        [SerializeField] protected WaterSystem waterSystem;

        [Header("Boat Specifications")]
        [SerializeField] protected HullSpecifications hullSpecs;

        [Header("Controls")]
        [SerializeField] protected float rudderAngle = 0f; // -30 to 30 degrees
        [SerializeField] protected float centerboardExtension = 1.0f; // 0-1

        // Physics components
        protected Rigidbody rb;
        protected HullDynamics hullDynamics;
        protected RudderDynamics rudder;
        protected CenterboardDynamics centerboard;
        protected CrewWeightSystem crewSystem;

        // State
        protected Vector3 apparentWind;
        protected float currentHeelAngle;
        protected float boatSpeedKnots;

        // Performance data
        protected float vmgUpwind;
        protected float vmgDownwind;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();

            // Initialize physics systems
            hullDynamics = new HullDynamics(hullSpecs);

            // Find environment systems if not assigned
            if (windSystem == null)
                windSystem = FindObjectOfType<WindSystem>();

            if (waterSystem == null)
                waterSystem = FindObjectOfType<WaterSystem>();
        }

        protected virtual void Start()
        {
            InitializeBoat();
        }

        protected virtual void FixedUpdate()
        {
            UpdatePhysics(Time.fixedDeltaTime);
        }

        /// <summary>
        /// Initialize boat-specific systems
        /// Override in derived classes
        /// </summary>
        protected abstract void InitializeBoat();

        /// <summary>
        /// Main physics update
        /// </summary>
        protected virtual void UpdatePhysics(float deltaTime)
        {
            // 1. Calculate apparent wind
            Vector3 trueWind = windSystem.GetWindAtPosition(transform.position, GetMastHeight());
            Vector3 boatVelocity = rb.velocity;
            apparentWind = VectorUtilities.CalculateApparentWind(trueWind, boatVelocity);

            // 2. Calculate forces
            Vector3 totalForce = Vector3.zero;
            Vector3 totalTorque = Vector3.zero;

            // Sail forces
            CalculateSailForces(out Vector3 sailForce, out Vector3 sailTorque);
            totalForce += sailForce;
            totalTorque += sailTorque;

            // Hull drag
            Vector3 hullDrag = hullDynamics.CalculateHullDrag(
                rb.velocity,
                waterSystem,
                currentHeelAngle
            );
            totalForce += hullDrag;

            // Foil forces (centerboard and rudder)
            CalculateFoilForces(out Vector3 foilForce, out Vector3 foilTorque);
            totalForce += foilForce;
            totalTorque += foilTorque;

            // Buoyancy
            hullDynamics.CalculateBuoyancy(
                transform,
                waterSystem,
                out Vector3 buoyancy,
                out Vector3 buoyancyCenter
            );
            totalForce += buoyancy;

            // Weight
            float totalWeight = hullSpecs.displacement * PhysicsConstants.GRAVITY;
            totalForce += Vector3.down * totalWeight;

            // 3. Calculate heel
            float heelingMoment = CalculateHeelingMoment(sailForce);
            float rightingMoment = CalculateRightingMoment(buoyancyCenter);
            currentHeelAngle = hullDynamics.CalculateHeelAngle(heelingMoment, rightingMoment, deltaTime);

            // Apply heel to boat
            ApplyHeel(currentHeelAngle);

            // 4. Apply forces
            rb.AddForce(totalForce);
            rb.AddTorque(totalTorque);

            // 5. Update performance data
            UpdatePerformanceData();
        }

        /// <summary>
        /// Calculate forces from all sails
        /// Override in derived classes for multi-sail boats
        /// </summary>
        protected abstract void CalculateSailForces(out Vector3 force, out Vector3 torque);

        /// <summary>
        /// Calculate forces from centerboard and rudder
        /// </summary>
        protected virtual void CalculateFoilForces(out Vector3 force, out Vector3 torque)
        {
            force = Vector3.zero;
            torque = Vector3.zero;

            // Water flow relative to boat
            Vector3 waterFlow = -(rb.velocity - waterSystem.GetCurrentVelocity(transform.position));

            // Centerboard
            if (centerboard != null)
            {
                Transform centerboardTransform = GetCenterboardTransform();
                float depth = waterSystem.GetSubmersionDepth(centerboardTransform.position);

                centerboard.CalculateCenterboardForces(
                    waterFlow,
                    centerboardTransform,
                    depth,
                    out Vector3 cbLift,
                    out Vector3 cbDrag
                );

                force += cbLift + cbDrag;

                // Torque from centerboard (lateral force creates turning moment)
                Vector3 leverArm = centerboardTransform.position - rb.worldCenterOfMass;
                torque += Vector3.Cross(leverArm, cbLift + cbDrag);
            }

            // Rudder
            if (rudder != null)
            {
                Transform rudderTransform = GetRudderTransform();
                float depth = waterSystem.GetSubmersionDepth(rudderTransform.position);

                rudder.steeringAngle = rudderAngle;
                rudder.CalculateRudderForces(
                    waterFlow,
                    rudderTransform,
                    depth,
                    out Vector3 rudderLift,
                    out Vector3 rudderDrag
                );

                force += rudderLift + rudderDrag;

                // Torque from rudder
                Vector3 leverArm = rudderTransform.position - rb.worldCenterOfMass;
                torque += Vector3.Cross(leverArm, rudderLift + rudderDrag);
            }
        }

        /// <summary>
        /// Calculate heeling moment from sail forces
        /// </summary>
        protected virtual float CalculateHeelingMoment(Vector3 sailForce)
        {
            // Heeling moment = lateral force × height above center of gravity
            float lateralForce = Vector3.Dot(sailForce, transform.right);
            float heightAboveCOG = GetCenterOfEffortHeight() - hullSpecs.centerOfGravity.y;

            return lateralForce * heightAboveCOG;
        }

        /// <summary>
        /// Calculate righting moment from buoyancy and crew weight
        /// </summary>
        protected virtual float CalculateRightingMoment(Vector3 buoyancyCenter)
        {
            float hullRighting = hullDynamics.CalculateRightingMoment(buoyancyCenter, transform);

            float crewRighting = 0f;
            if (crewSystem != null)
            {
                crewRighting = crewSystem.CalculateTotalRightingMoment(hullSpecs.centerOfGravity);
            }

            return hullRighting + crewRighting;
        }

        /// <summary>
        /// Apply heel angle to boat transform
        /// </summary>
        protected virtual void ApplyHeel(float heelAngle)
        {
            Quaternion heelRotation = Quaternion.Euler(0f, transform.eulerAngles.y, -heelAngle);

            // Smooth transition
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                heelRotation,
                Time.fixedDeltaTime * 5f
            );
        }

        /// <summary>
        /// Update performance metrics
        /// </summary>
        protected virtual void UpdatePerformanceData()
        {
            boatSpeedKnots = PhysicsConstants.MetersPerSecondToKnots(rb.velocity.magnitude);

            // Calculate VMG
            Vector3 windDirection = windSystem.GetTrueWind().normalized;
            vmgUpwind = VectorUtilities.CalculateVMG(rb.velocity, windDirection);
            vmgDownwind = VectorUtilities.CalculateVMG(rb.velocity, -windDirection);
        }

        // Control methods - to be called by input system

        /// <summary>
        /// Set rudder angle
        /// </summary>
        public virtual void SetRudderAngle(float angle)
        {
            rudderAngle = Mathf.Clamp(angle, -30f, 30f);
        }

        /// <summary>
        /// Set centerboard extension
        /// </summary>
        public virtual void SetCenterboardExtension(float extension)
        {
            centerboardExtension = Mathf.Clamp01(extension);
            if (centerboard != null)
            {
                centerboard.SetExtension(centerboardExtension);
            }
        }

        // Getters for UI and instruments

        public float GetBoatSpeedKnots() => boatSpeedKnots;
        public float GetHeelAngle() => currentHeelAngle;
        public Vector3 GetApparentWind() => apparentWind;
        public float GetVMGUpwind() => vmgUpwind;
        public float GetVMGDownwind() => vmgDownwind;

        public float GetApparentWindAngle()
        {
            return VectorUtilities.CalculateSignedWindAngle(
                transform.forward,
                transform.right,
                apparentWind
            );
        }

        public float GetApparentWindSpeed()
        {
            return PhysicsConstants.MetersPerSecondToKnots(apparentWind.magnitude);
        }

        public float GetTrueWindAngle()
        {
            Vector3 trueWind = windSystem.GetTrueWind();
            return VectorUtilities.CalculateSignedWindAngle(
                transform.forward,
                transform.right,
                trueWind
            );
        }

        // Abstract methods for boat-specific properties

        protected abstract float GetMastHeight();
        protected abstract float GetCenterOfEffortHeight();
        protected abstract Transform GetCenterboardTransform();
        protected abstract Transform GetRudderTransform();

        // Visualization
        protected virtual void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            // Draw velocity vector
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, rb.velocity);

            // Draw apparent wind
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up * 2f, apparentWind.normalized * 3f);

            // Draw heel axis
            Gizmos.color = Color.red;
            Vector3 heelAxis = transform.forward * 2f;
            Gizmos.DrawRay(transform.position, heelAxis);
        }
    }
}
