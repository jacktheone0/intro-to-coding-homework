using UnityEngine;
using SailingSimulator.Core.Physics;

namespace SailingSimulator.Core.Environment
{
    /// <summary>
    /// Manages water physics including waves, currents, and surface interaction
    /// </summary>
    public class WaterSystem : MonoBehaviour
    {
        [Header("Water Properties")]
        [SerializeField] private float waterLevel = 0f; // Y coordinate of water surface
        [SerializeField] private float waterDensity = PhysicsConstants.WATER_DENSITY;

        [Header("Wave Settings")]
        [SerializeField] private bool enableWaves = true;
        [SerializeField] private float waveHeight = 0.5f; // meters
        [SerializeField] private float waveLength = 10f; // meters
        [SerializeField] private float wavePeriod = 3f; // seconds
        [SerializeField] private Vector2 waveDirection = new Vector2(0f, 1f); // normalized

        [Header("Current Settings")]
        [SerializeField] private bool enableCurrent = false;
        [SerializeField] private Vector3 currentVelocity = Vector3.zero; // m/s

        private float wavePhaseAccumulator;

        void Update()
        {
            wavePhaseAccumulator += Time.deltaTime;
        }

        /// <summary>
        /// Get water height at a specific position
        /// </summary>
        public float GetWaterHeight(Vector3 position)
        {
            if (!enableWaves)
                return waterLevel;

            // Simple sinusoidal wave
            float waveNumber = 2f * Mathf.PI / waveLength;
            float omega = 2f * Mathf.PI / wavePeriod;

            Vector2 pos2D = new Vector2(position.x, position.z);
            float phase = Vector2.Dot(pos2D, waveDirection.normalized) * waveNumber - omega * wavePhaseAccumulator;

            float height = waterLevel + waveHeight * Mathf.Sin(phase);

            return height;
        }

        /// <summary>
        /// Get water surface normal at a specific position (for wave slopes)
        /// </summary>
        public Vector3 GetWaterNormal(Vector3 position)
        {
            if (!enableWaves)
                return Vector3.up;

            // Calculate gradient for surface normal
            float delta = 0.1f;
            float h0 = GetWaterHeight(position);
            float hx = GetWaterHeight(position + Vector3.right * delta);
            float hz = GetWaterHeight(position + Vector3.forward * delta);

            Vector3 tangentX = new Vector3(delta, hx - h0, 0f);
            Vector3 tangentZ = new Vector3(0f, hz - h0, delta);

            return Vector3.Cross(tangentZ, tangentX).normalized;
        }

        /// <summary>
        /// Get current velocity at a specific position
        /// </summary>
        public Vector3 GetCurrentVelocity(Vector3 position)
        {
            if (!enableCurrent)
                return Vector3.zero;

            // Future: Add spatial variation, tidal flows, etc.
            return currentVelocity;
        }

        /// <summary>
        /// Calculate buoyancy force on a submerged volume
        /// </summary>
        public Vector3 CalculateBuoyancyForce(float submergedVolume)
        {
            // Archimedes principle: F = ρ * V * g
            float forceMagnitude = waterDensity * submergedVolume * PhysicsConstants.GRAVITY;
            return Vector3.up * forceMagnitude;
        }

        /// <summary>
        /// Calculate drag force on a hull moving through water
        /// Includes friction drag, form drag, and wave-making drag
        /// </summary>
        public Vector3 CalculateHullDrag(Vector3 velocity, float wettedSurfaceArea, float hullLength, float beamWidth)
        {
            float speed = velocity.magnitude;
            if (speed < 0.01f)
                return Vector3.zero;

            // Friction drag (skin friction)
            float reynoldsNumber = speed * hullLength / 0.000001f; // kinematic viscosity of water
            float frictionCoefficient = 0.075f / Mathf.Pow(Mathf.Log10(reynoldsNumber) - 2f, 2f);
            float frictionDrag = 0.5f * waterDensity * speed * speed * wettedSurfaceArea * frictionCoefficient;

            // Form drag (pressure drag)
            float formDragCoefficient = PhysicsConstants.HULL_FORM_DRAG_BASE;
            float formDrag = 0.5f * waterDensity * speed * speed * (beamWidth * 0.5f) * formDragCoefficient;

            // Wave-making drag (increases dramatically with speed)
            float froudeNumber = speed / Mathf.Sqrt(PhysicsConstants.GRAVITY * hullLength);
            float waveDragCoefficient = 0.5f * Mathf.Pow(froudeNumber, 4f);
            float waveDrag = 0.5f * waterDensity * speed * speed * (hullLength * beamWidth) * waveDragCoefficient;

            float totalDrag = frictionDrag + formDrag + waveDrag;

            return -velocity.normalized * totalDrag;
        }

        /// <summary>
        /// Check if a point is underwater
        /// </summary>
        public bool IsUnderwater(Vector3 position)
        {
            return position.y < GetWaterHeight(position);
        }

        /// <summary>
        /// Get submerged depth at a position
        /// </summary>
        public float GetSubmersionDepth(Vector3 position)
        {
            float waterHeight = GetWaterHeight(position);
            return Mathf.Max(0f, waterHeight - position.y);
        }

        /// <summary>
        /// Set current conditions (for practice mode)
        /// </summary>
        public void SetCurrentVelocity(Vector3 velocity)
        {
            currentVelocity = velocity;
            enableCurrent = velocity.magnitude > 0.01f;
        }

        /// <summary>
        /// Set wave conditions (for practice mode)
        /// </summary>
        public void SetWaveConditions(float height, float length, float period)
        {
            waveHeight = height;
            waveLength = length;
            wavePeriod = period;
            enableWaves = height > 0.01f;
        }

        // Editor visualization
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Gizmos.DrawCube(new Vector3(0f, waterLevel, 0f), new Vector3(100f, 0.1f, 100f));

            if (enableCurrent && Application.isPlaying)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, currentVelocity * 5f);
            }
        }
    }
}
