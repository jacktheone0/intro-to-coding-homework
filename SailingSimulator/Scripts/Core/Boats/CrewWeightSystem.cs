using UnityEngine;

namespace SailingSimulator.Core.Boats
{
    /// <summary>
    /// Crew member with weight and position
    /// </summary>
    [System.Serializable]
    public class CrewMember
    {
        public string name;
        public float weight = 70f; // kg
        public Vector3 localPosition; // Position relative to boat

        // Position controls
        public float foreAftPosition = 0f; // -1 (stern) to 1 (bow)
        public float athwartshipsPosition = 0f; // -1 (port) to 1 (starboard)
        public bool onTrapeze = false;
        public float trapezeHeight = 1.0f; // 0-1, height of trapeze ring

        public CrewMember(string crewName, float crewWeight)
        {
            name = crewName;
            weight = crewWeight;
        }

        /// <summary>
        /// Update local position based on control inputs
        /// </summary>
        public void UpdatePosition(float cockpitLength, float cockpitBeam, float railHeight)
        {
            // Calculate position in cockpit
            float x = athwartshipsPosition * (cockpitBeam / 2f);
            float z = foreAftPosition * (cockpitLength / 2f);

            // Hiking: crew moves outboard
            if (Mathf.Abs(athwartshipsPosition) > 0.7f)
            {
                // On rail
                localPosition = new Vector3(x, railHeight, z);
            }
            else
            {
                // In cockpit
                localPosition = new Vector3(x, 0.3f, z);
            }
        }

        /// <summary>
        /// Calculate righting moment from crew weight
        /// </summary>
        public float CalculateRightingMoment(Vector3 boatCenterline)
        {
            // Lever arm is horizontal distance from centerline
            float leverArm = localPosition.x - boatCenterline.x;

            return weight * Physics.PhysicsConstants.GRAVITY * leverArm;
        }
    }

    /// <summary>
    /// Manages crew weight distribution and its effects on boat performance
    /// </summary>
    public class CrewWeightSystem
    {
        public CrewMember[] crew;
        private float totalCrewWeight;

        // Boat dimensions
        private float cockpitLength;
        private float cockpitBeam;
        private float railHeight;

        public CrewWeightSystem(int crewCount, float boatLength, float boatBeam)
        {
            crew = new CrewMember[crewCount];
            cockpitLength = boatLength * 0.6f; // Approximate cockpit size
            cockpitBeam = boatBeam;
            railHeight = 0.5f; // Height of gunwale for hiking
        }

        /// <summary>
        /// Initialize crew members
        /// </summary>
        public void InitializeCrew(params (string name, float weight)[] crewData)
        {
            totalCrewWeight = 0f;

            for (int i = 0; i < crew.Length && i < crewData.Length; i++)
            {
                crew[i] = new CrewMember(crewData[i].name, crewData[i].weight);
                totalCrewWeight += crewData[i].weight;
            }
        }

        /// <summary>
        /// Update all crew positions
        /// </summary>
        public void UpdateCrewPositions()
        {
            foreach (var crewMember in crew)
            {
                if (crewMember != null)
                {
                    crewMember.UpdatePosition(cockpitLength, cockpitBeam, railHeight);
                }
            }
        }

        /// <summary>
        /// Calculate total righting moment from crew
        /// </summary>
        public float CalculateTotalRightingMoment(Vector3 boatCenterline)
        {
            float totalMoment = 0f;

            foreach (var crewMember in crew)
            {
                if (crewMember != null)
                {
                    totalMoment += crewMember.CalculateRightingMoment(boatCenterline);
                }
            }

            return totalMoment;
        }

        /// <summary>
        /// Calculate combined center of gravity for crew
        /// </summary>
        public Vector3 CalculateCrewCenterOfGravity()
        {
            if (totalCrewWeight < 0.1f)
                return Vector3.zero;

            Vector3 weightedSum = Vector3.zero;

            foreach (var crewMember in crew)
            {
                if (crewMember != null)
                {
                    weightedSum += crewMember.localPosition * crewMember.weight;
                }
            }

            return weightedSum / totalCrewWeight;
        }

        /// <summary>
        /// Get total crew weight
        /// </summary>
        public float GetTotalWeight()
        {
            return totalCrewWeight;
        }

        /// <summary>
        /// Set crew weight distribution for different wind conditions
        /// Based on real sailing photos and techniques
        /// </summary>
        public void ApplyWeightPreset(WeightPreset preset, float windStrength)
        {
            switch (preset)
            {
                case WeightPreset.LightAirUpwind:
                    // Light air: crew sits to leeward, forward
                    SetCrewPositions(-0.2f, 0.3f); // Leeward, slightly forward
                    break;

                case WeightPreset.MediumAirUpwind:
                    // Medium air: crew hikes on rail, centered
                    SetCrewPositions(0.9f, 0.0f); // Windward rail, centered
                    break;

                case WeightPreset.HeavyAirUpwind:
                    // Heavy air: crew fully hiked, aft
                    SetCrewPositions(1.0f, -0.2f); // Full hike, slightly aft
                    break;

                case WeightPreset.ReachingMedium:
                    // Reaching: crew centered athwartships, aft
                    SetCrewPositions(0.3f, -0.1f);
                    break;

                case WeightPreset.RunningLight:
                    // Running: crew forward to prevent bow-up
                    SetCrewPositions(0.0f, 0.4f); // Centered, forward
                    break;

                case WeightPreset.RunningHeavy:
                    // Heavy running: crew aft and to windward
                    SetCrewPositions(0.4f, -0.3f);
                    break;
            }
        }

        private void SetCrewPositions(float athwartships, float foreAft)
        {
            foreach (var crewMember in crew)
            {
                if (crewMember != null)
                {
                    crewMember.athwartshipsPosition = athwartships;
                    crewMember.foreAftPosition = foreAft;
                }
            }
        }
    }

    /// <summary>
    /// Preset weight distributions for different conditions
    /// </summary>
    public enum WeightPreset
    {
        LightAirUpwind,
        MediumAirUpwind,
        HeavyAirUpwind,
        ReachingMedium,
        RunningLight,
        RunningHeavy
    }

    /// <summary>
    /// Trapeze system for i420 and 29er
    /// </summary>
    public class TrapezeSystem
    {
        public bool isActive = false;
        public float wireLength = 2.5f; // meters
        public float ringHeight = 1.0f; // 0-1
        public float crewWeight = 70f; // kg

        private Vector3 wireAttachmentPoint; // On mast
        private Vector3 crewPosition;

        public TrapezeSystem(Vector3 mastAttachment, float crewMass)
        {
            wireAttachmentPoint = mastAttachment;
            crewWeight = crewMass;
        }

        /// <summary>
        /// Calculate trapeze righting moment
        /// Trapeze provides significantly more righting moment than hiking
        /// </summary>
        public float CalculateRightingMoment(float boatHeelAngle)
        {
            if (!isActive)
                return 0f;

            // Crew is suspended outside boat
            float effectiveWireLength = wireLength * ringHeight;

            // Calculate horizontal lever arm
            // Crew position depends on heel angle and wire length
            float heelRad = boatHeelAngle * Physics.PhysicsConstants.DEGREES_TO_RADIANS;
            float leverArm = effectiveWireLength * Mathf.Cos(heelRad);

            return crewWeight * Physics.PhysicsConstants.GRAVITY * leverArm;
        }

        /// <summary>
        /// Get crew position when on trapeze
        /// </summary>
        public Vector3 GetCrewPosition(float boatHeelAngle)
        {
            if (!isActive)
                return Vector3.zero;

            float effectiveWireLength = wireLength * ringHeight;
            float heelRad = boatHeelAngle * Physics.PhysicsConstants.DEGREES_TO_RADIANS;

            // Crew hangs from wire
            crewPosition = wireAttachmentPoint + new Vector3(
                effectiveWireLength * Mathf.Sin(heelRad),
                -effectiveWireLength * Mathf.Cos(heelRad),
                0f
            );

            return crewPosition;
        }

        /// <summary>
        /// Set trapeze wire ring height
        /// </summary>
        public void SetRingHeight(float height)
        {
            ringHeight = Mathf.Clamp01(height);
        }
    }
}
