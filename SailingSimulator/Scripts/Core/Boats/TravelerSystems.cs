using UnityEngine;

namespace SailingSimulator.Core.Boats
{
    /// <summary>
    /// Base class for traveler systems
    /// Controls mainsheet angle athwartships
    /// </summary>
    public abstract class TravelerSystem
    {
        public float position = 0f; // -1 (port) to +1 (starboard), 0 = centered
        public float maxTravel; // meters, total travel distance
        protected float adjustmentSpeed = 1.0f; // How fast it can be adjusted

        public TravelerSystem(float maxTravelDistance)
        {
            maxTravel = maxTravelDistance;
        }

        /// <summary>
        /// Update traveler position
        /// </summary>
        public virtual void Update(float input, float deltaTime)
        {
            position += input * adjustmentSpeed * deltaTime;
            position = Mathf.Clamp(position, -1f, 1f);
        }

        /// <summary>
        /// Get physical position in world space
        /// </summary>
        public Vector3 GetWorldPosition(Transform boatTransform, Vector3 travelerMountPoint)
        {
            // Position along traveler track
            Vector3 offset = boatTransform.right * (position * maxTravel / 2f);
            return boatTransform.TransformPoint(travelerMountPoint) + offset;
        }

        /// <summary>
        /// Get effect on mainsheet angle
        /// </summary>
        public abstract float GetMainsheetAngleModifier();

        /// <summary>
        /// Set position directly (for quick adjustments)
        /// </summary>
        public void SetPosition(float pos)
        {
            position = Mathf.Clamp(pos, -1f, 1f);
        }
    }

    /// <summary>
    /// ILCA Traveler System
    /// Simple bridle with 4:1 purchase, limited travel
    /// </summary>
    public class ILCATravelerSystem : TravelerSystem
    {
        private const float ILCA_TRAVEL_DISTANCE = 0.4f; // ~12-18 inches (0.3-0.45m)
        private const float PURCHASE_RATIO = 4.0f; // 4:1 mechanical advantage

        public ILCATravelerSystem() : base(ILCA_TRAVEL_DISTANCE)
        {
            // Moderate adjustment speed due to purchase system
            adjustmentSpeed = 0.3f;
        }

        public override void Update(float input, float deltaTime)
        {
            // Account for purchase ratio - need to pull more line to move car less
            float effectiveInput = input / PURCHASE_RATIO;
            base.Update(effectiveInput, deltaTime);
        }

        public override float GetMainsheetAngleModifier()
        {
            // Small travel = small angle changes (±5 degrees max)
            return position * 5f;
        }

        /// <summary>
        /// ILCA traveler is primarily used upwind for fine-tuning
        /// </summary>
        public bool IsOptimalForCondition(float apparentWindAngle)
        {
            return Mathf.Abs(apparentWindAngle) < 60f; // Upwind angles
        }
    }

    /// <summary>
    /// i420 Traveler System
    /// Track-and-car system with wide travel across rear beam
    /// </summary>
    public class I420TravelerSystem : TravelerSystem
    {
        private const float I420_TRAVEL_DISTANCE = 1.4f; // ~4-5 feet (1.2-1.5m)
        private const float CAR_FRICTION = 0.1f;

        // Twin control lines
        private float portLineLength = 0f;
        private float starboardLineLength = 0f;

        public I420TravelerSystem() : base(I420_TRAVEL_DISTANCE)
        {
            // Fast adjustment - can be quickly repositioned
            adjustmentSpeed = 1.5f;
        }

        public override void Update(float input, float deltaTime)
        {
            // Fast, responsive control
            base.Update(input, deltaTime);

            // Simulate slight friction
            if (Mathf.Abs(input) < 0.01f)
            {
                // Damp position when no input (friction holds car)
                position *= (1f - CAR_FRICTION * deltaTime);
            }
        }

        public override float GetMainsheetAngleModifier()
        {
            // Wide travel = large angle changes (±20 degrees)
            return position * 20f;
        }

        /// <summary>
        /// i420 traveler is used across all points of sail
        /// </summary>
        public float GetOptimalPosition(float apparentWindAngle, float windSpeed)
        {
            float absAngle = Mathf.Abs(apparentWindAngle);

            if (absAngle < 50f)
            {
                // Upwind: centered to close slot
                return 0f;
            }
            else if (absAngle < 90f)
            {
                // Reaching: slightly to leeward
                return -0.3f;
            }
            else
            {
                // Downwind: eased to leeward to open leech
                return -0.6f;
            }
        }
    }

    /// <summary>
    /// 29er Traveler System
    /// Continuous line system with cam cleat for instant adjustment
    /// </summary>
    public class Traveler29erSystem : TravelerSystem
    {
        private const float NINER_TRAVEL_DISTANCE = 1.6f; // ~5-6 feet (1.5-1.8m)
        private bool camCleatEngaged = true;

        public Traveler29erSystem() : base(NINER_TRAVEL_DISTANCE)
        {
            // Very fast adjustment with continuous line
            adjustmentSpeed = 3.0f;
        }

        public override void Update(float input, float deltaTime)
        {
            if (!camCleatEngaged && Mathf.Abs(input) > 0.01f)
            {
                // Released cam cleat - very fast adjustment
                position += input * adjustmentSpeed * deltaTime;
                position = Mathf.Clamp(position, -1f, 1f);
            }
            else if (camCleatEngaged)
            {
                // Cam cleat holds position
                // No movement unless cleat is released
            }
        }

        /// <summary>
        /// Release cam cleat for adjustment
        /// </summary>
        public void ReleaseCamCleat()
        {
            camCleatEngaged = false;
        }

        /// <summary>
        /// Engage cam cleat to lock position
        /// </summary>
        public void EngageCamCleat()
        {
            camCleatEngaged = true;
        }

        public override float GetMainsheetAngleModifier()
        {
            // Very wide travel = large angle changes (±25 degrees)
            return position * 25f;
        }

        /// <summary>
        /// 29er requires constant traveler adjustment for planing control
        /// </summary>
        public float GetDynamicPosition(float boatSpeed, float heelAngle, float apparentWindAngle)
        {
            float absAngle = Mathf.Abs(apparentWindAngle);

            // Aggressive adjustments for skiff sailing
            if (boatSpeed > 15f) // Planing
            {
                // Ease traveler to depower and maintain control
                if (Mathf.Abs(heelAngle) > 15f)
                {
                    return -0.7f; // Significant ease to reduce power
                }
                else
                {
                    return -0.3f;
                }
            }
            else if (absAngle < 60f) // Upwind
            {
                return 0.1f; // Slightly to windward for pointing
            }
            else if (absAngle < 120f) // Reaching
            {
                return -0.4f;
            }
            else // Running
            {
                return -0.8f; // Well eased for downwind
            }
        }
    }
}
