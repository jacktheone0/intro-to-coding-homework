using UnityEngine;

namespace SailingSimulator.Core.Physics
{
    /// <summary>
    /// Physical constants and unit conversions for sailing simulation
    /// </summary>
    public static class PhysicsConstants
    {
        // Fluid properties
        public const float WATER_DENSITY = 1025f; // kg/m³ (saltwater)
        public const float AIR_DENSITY = 1.225f; // kg/m³ (at sea level, 15°C)
        public const float GRAVITY = 9.81f; // m/s²

        // Unit conversions
        public const float KNOTS_TO_MS = 0.514444f; // Convert knots to m/s
        public const float MS_TO_KNOTS = 1.94384f; // Convert m/s to knots
        public const float DEGREES_TO_RADIANS = Mathf.PI / 180f;
        public const float RADIANS_TO_DEGREES = 180f / Mathf.PI;

        // Wind system constants
        public const float WIND_GRADIENT_EXPONENT = 0.11f; // Wind shear exponent over water
        public const float REFERENCE_HEIGHT = 10f; // Reference height for wind measurement (meters)
        public const float GUST_FACTOR_MIN = 0.7f; // Minimum wind speed multiplier
        public const float GUST_FACTOR_MAX = 1.4f; // Maximum wind speed multiplier

        // Wave system constants
        public const float WAVE_GRAVITY = 9.81f;
        public const float DEEP_WATER_THRESHOLD = 0.5f; // wavelength/depth ratio

        // Drag coefficients (approximate)
        public const float HULL_FRICTION_COEFFICIENT = 0.005f;
        public const float HULL_FORM_DRAG_BASE = 0.05f;
        public const float FOIL_DRAG_COEFFICIENT = 0.01f;

        /// <summary>
        /// Convert knots to meters per second
        /// </summary>
        public static float KnotsToMetersPerSecond(float knots)
        {
            return knots * KNOTS_TO_MS;
        }

        /// <summary>
        /// Convert meters per second to knots
        /// </summary>
        public static float MetersPerSecondToKnots(float ms)
        {
            return ms * MS_TO_KNOTS;
        }

        /// <summary>
        /// Calculate wind speed at a given height using wind gradient formula
        /// </summary>
        public static float WindSpeedAtHeight(float referenceSpeed, float height)
        {
            return referenceSpeed * Mathf.Pow(height / REFERENCE_HEIGHT, WIND_GRADIENT_EXPONENT);
        }

        /// <summary>
        /// Calculate dynamic pressure: 0.5 * rho * v^2
        /// </summary>
        public static float DynamicPressure(float velocity, float density)
        {
            return 0.5f * density * velocity * velocity;
        }
    }
}
