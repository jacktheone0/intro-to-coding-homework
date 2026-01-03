using UnityEngine;

namespace SailingSimulator.Core.Boats
{
    /// <summary>
    /// Adjusts the center of mass to prevent boat from tipping over
    /// Attach this to your boat GameObject
    /// </summary>
    public class BoatStabilizer : MonoBehaviour
    {
        [Header("Center of Mass")]
        [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0, -0.5f, 0);
        [SerializeField] private bool showCenterOfMass = true;

        [Header("Stability Settings")]
        [SerializeField] private bool constrainRoll = true; // Prevent tipping sideways
        [SerializeField] private bool constrainPitch = true; // Prevent tipping forward/back
        [SerializeField] private float maxRollAngle = 45f; // Maximum heel angle
        [SerializeField] private float stabilityForce = 10f; // Force to upright boat

        private Rigidbody rb;

        void Start()
        {
            rb = GetComponent<Rigidbody>();

            if (rb != null)
            {
                // Lower center of mass to make boat more stable
                rb.centerOfMass = centerOfMassOffset;

                Debug.Log($"Center of Mass set to: {rb.centerOfMass}");
            }
        }

        void FixedUpdate()
        {
            if (rb == null) return;

            // Apply stabilization if enabled
            if (constrainRoll || constrainPitch)
            {
                ApplyStabilization();
            }
        }

        private void ApplyStabilization()
        {
            Vector3 currentRotation = transform.eulerAngles;

            // Convert to -180 to 180 range
            float rollAngle = currentRotation.z;
            if (rollAngle > 180) rollAngle -= 360;

            float pitchAngle = currentRotation.x;
            if (pitchAngle > 180) pitchAngle -= 360;

            // Apply counter-torque to limit roll
            if (constrainRoll && Mathf.Abs(rollAngle) > maxRollAngle)
            {
                float rollCorrection = -rollAngle * stabilityForce;
                rb.AddTorque(transform.forward * rollCorrection);
            }

            // Apply counter-torque to limit pitch
            if (constrainPitch && Mathf.Abs(pitchAngle) > 15f)
            {
                float pitchCorrection = -pitchAngle * stabilityForce;
                rb.AddTorque(transform.right * pitchCorrection);
            }

            // Dampen angular velocity to prevent oscillation
            rb.angularVelocity *= 0.95f;
        }

        void OnDrawGizmos()
        {
            if (!showCenterOfMass) return;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Show center of mass
                Gizmos.color = Color.yellow;
                Vector3 worldCOM = transform.TransformPoint(rb.centerOfMass);
                Gizmos.DrawSphere(worldCOM, 0.1f);
                Gizmos.DrawLine(transform.position, worldCOM);

                // Label
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(worldCOM, "Center of Mass");
                #endif
            }
        }
    }
}
