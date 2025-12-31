using UnityEngine;
using SailingSimulator.Core.Physics;

namespace SailingSimulator.Core.Environment
{
    /// <summary>
    /// Manages wind simulation including gusts, shifts, gradient, and turbulence
    /// </summary>
    public class WindSystem : MonoBehaviour
    {
        [Header("Base Wind Settings")]
        [SerializeField] private float baseWindSpeed = 10f; // knots
        [SerializeField] private float baseWindDirection = 0f; // degrees (0 = north, 90 = east)

        [Header("Wind Variation")]
        [SerializeField] private bool enableGusts = true;
        [SerializeField] private float gustPeriod = 15f; // seconds
        [SerializeField] private float gustStrength = 0.3f; // 0-1, how much wind varies

        [Header("Wind Shifts")]
        [SerializeField] private bool enableOscillatingShifts = true;
        [SerializeField] private float shiftPeriod = 45f; // seconds
        [SerializeField] private float shiftAmplitude = 15f; // degrees

        [SerializeField] private bool enablePersistentShifts = false;
        [SerializeField] private float persistentShiftRate = 2f; // degrees per minute

        [Header("Advanced")]
        [SerializeField] private bool enableTurbulence = true;
        [SerializeField] private float turbulenceScale = 0.1f;
        [SerializeField] private float turbulenceSpeed = 0.5f;

        // Runtime state
        private float currentWindSpeed;
        private float currentWindDirection;
        private float timeAccumulator;
        private float persistentShiftAccumulator;

        // Cached values
        private Vector3 currentWindVelocity;

        void Start()
        {
            currentWindSpeed = baseWindSpeed;
            currentWindDirection = baseWindDirection;
            UpdateWindVelocity();
        }

        void Update()
        {
            timeAccumulator += Time.deltaTime;

            // Calculate wind speed variations (gusts)
            if (enableGusts)
            {
                float gustNoise = Mathf.PerlinNoise(timeAccumulator / gustPeriod, 0f);
                float gustFactor = Mathf.Lerp(
                    PhysicsConstants.GUST_FACTOR_MIN,
                    PhysicsConstants.GUST_FACTOR_MAX,
                    gustNoise
                );
                currentWindSpeed = baseWindSpeed * gustFactor;
            }
            else
            {
                currentWindSpeed = baseWindSpeed;
            }

            // Calculate wind direction shifts
            float directionShift = 0f;

            // Oscillating shifts (periodic)
            if (enableOscillatingShifts)
            {
                directionShift += Mathf.Sin(timeAccumulator * 2f * Mathf.PI / shiftPeriod) * shiftAmplitude;
            }

            // Persistent shifts (gradual change)
            if (enablePersistentShifts)
            {
                persistentShiftAccumulator += persistentShiftRate * (Time.deltaTime / 60f);
                directionShift += persistentShiftAccumulator;
            }

            // Add turbulence (small random variations)
            if (enableTurbulence)
            {
                float turbulence = Mathf.PerlinNoise(
                    timeAccumulator * turbulenceSpeed,
                    timeAccumulator * turbulenceSpeed * 0.7f
                ) - 0.5f;
                directionShift += turbulence * turbulenceScale * 10f;
            }

            currentWindDirection = baseWindDirection + directionShift;

            UpdateWindVelocity();
        }

        private void UpdateWindVelocity()
        {
            float windSpeedMS = PhysicsConstants.KnotsToMetersPerSecond(currentWindSpeed);
            float radians = currentWindDirection * Mathf.Deg2Rad;

            // Unity coordinate system: Z = forward (north), X = right (east)
            currentWindVelocity = new Vector3(
                Mathf.Sin(radians) * windSpeedMS,
                0f,
                Mathf.Cos(radians) * windSpeedMS
            );
        }

        /// <summary>
        /// Get wind velocity at a specific position and height
        /// Includes wind gradient (wind speed increases with height)
        /// </summary>
        public Vector3 GetWindAtPosition(Vector3 position, float height)
        {
            // Apply wind gradient
            float heightFactor = PhysicsConstants.WindSpeedAtHeight(1f, height);

            // Future: Add spatial variation, blanketing zones, etc.
            Vector3 wind = currentWindVelocity * heightFactor;

            return wind;
        }

        /// <summary>
        /// Get current true wind velocity at reference height (10m)
        /// </summary>
        public Vector3 GetTrueWind()
        {
            return currentWindVelocity;
        }

        /// <summary>
        /// Get current wind speed in knots
        /// </summary>
        public float GetWindSpeedKnots()
        {
            return currentWindSpeed;
        }

        /// <summary>
        /// Get current wind direction in degrees
        /// </summary>
        public float GetWindDirectionDegrees()
        {
            return currentWindDirection;
        }

        /// <summary>
        /// Set wind conditions (for practice mode)
        /// </summary>
        public void SetWindConditions(float speedKnots, float directionDegrees)
        {
            baseWindSpeed = speedKnots;
            baseWindDirection = directionDegrees;
            currentWindSpeed = speedKnots;
            currentWindDirection = directionDegrees;
            UpdateWindVelocity();
        }

        /// <summary>
        /// Check if a boat is in another boat's wind shadow (blanketing)
        /// </summary>
        public bool IsInWindShadow(Vector3 position, Vector3 shadowSourcePosition, Vector3 shadowSourceForward, float shadowLength = 30f)
        {
            Vector3 toPosition = position - shadowSourcePosition;

            // Check if position is downwind of shadow source
            float downwindDot = Vector3.Dot(toPosition.normalized, -currentWindVelocity.normalized);

            if (downwindDot > 0.7f) // Within ~45 degree cone downwind
            {
                float distance = toPosition.magnitude;
                return distance < shadowLength;
            }

            return false;
        }

        // Editor visualization
        void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Vector3 windArrow = currentWindVelocity.normalized * 5f;
                Gizmos.DrawRay(transform.position, windArrow);
                Gizmos.DrawSphere(transform.position + windArrow, 0.3f);
            }
        }
    }
}
