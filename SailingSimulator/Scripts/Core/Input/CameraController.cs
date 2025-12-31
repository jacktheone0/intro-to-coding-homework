using UnityEngine;

namespace SailingSimulator.Core.Input
{
    /// <summary>
    /// Camera modes for different viewing perspectives
    /// </summary>
    public enum CameraMode
    {
        FirstPerson,    // Cockpit view from skipper position
        ThirdPersonChase, // Following boat from behind/side
        Broadcast,      // Dynamic cinematic angles
        TacticalOverhead // Bird's eye view for strategy
    }

    /// <summary>
    /// Manages camera positioning and view modes for sailing simulator
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform boatTransform;
        [SerializeField] private Transform skipperSeatTransform;

        [Header("Camera Settings")]
        [SerializeField] private CameraMode currentMode = CameraMode.ThirdPersonChase;
        [SerializeField] private float transitionSpeed = 5f;

        [Header("First Person Settings")]
        [SerializeField] private Vector3 firstPersonOffset = new Vector3(0f, 0.8f, -0.5f);
        [SerializeField] private float firstPersonFOV = 75f;
        [SerializeField] private bool firstPersonHeadLook = true;
        [SerializeField] private float headLookSpeed = 3f;

        [Header("Third Person Settings")]
        [SerializeField] private float thirdPersonDistance = 8f;
        [SerializeField] private float thirdPersonHeight = 3f;
        [SerializeField] private float thirdPersonAngle = 15f; // degrees above horizon
        [SerializeField] private float thirdPersonFOV = 60f;
        [SerializeField] private float followSmoothness = 5f;

        [Header("Broadcast Settings")]
        [SerializeField] private float broadcastDistance = 12f;
        [SerializeField] private float broadcastHeight = 4f;
        [SerializeField] private float broadcastFOV = 50f;
        [SerializeField] private float cinematicSpeed = 0.5f;

        [Header("Tactical Settings")]
        [SerializeField] private float tacticalHeight = 30f;
        [SerializeField] private float tacticalAngle = 70f; // degrees down from horizontal
        [SerializeField] private float tacticalFOV = 70f;

        private Camera cam;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private float targetFOV;

        private float cinematicTime = 0f;
        private Vector2 headLookAngle = Vector2.zero; // x = yaw, y = pitch

        void Start()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = Camera.main;
            }

            if (boatTransform == null)
            {
                // Try to find boat in scene
                var boat = FindObjectOfType<Boats.BaseBoat>();
                if (boat != null)
                {
                    boatTransform = boat.transform;
                }
            }
        }

        void LateUpdate()
        {
            if (boatTransform == null)
                return;

            // Calculate target camera position and rotation based on mode
            switch (currentMode)
            {
                case CameraMode.FirstPerson:
                    UpdateFirstPersonCamera();
                    break;
                case CameraMode.ThirdPersonChase:
                    UpdateThirdPersonCamera();
                    break;
                case CameraMode.Broadcast:
                    UpdateBroadcastCamera();
                    break;
                case CameraMode.TacticalOverhead:
                    UpdateTacticalCamera();
                    break;
            }

            // Smoothly transition to target
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * transitionSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * transitionSpeed);
        }

        private void UpdateFirstPersonCamera()
        {
            // Position at skipper seat
            Transform referencePoint = skipperSeatTransform != null ? skipperSeatTransform : boatTransform;
            targetPosition = referencePoint.TransformPoint(firstPersonOffset);

            // Look direction with head look
            if (firstPersonHeadLook)
            {
                Quaternion headRotation = Quaternion.Euler(headLookAngle.y, headLookAngle.x, 0f);
                targetRotation = boatTransform.rotation * headRotation;
            }
            else
            {
                targetRotation = boatTransform.rotation;
            }

            targetFOV = firstPersonFOV;
        }

        private void UpdateThirdPersonCamera()
        {
            // Position behind and above boat
            Vector3 offset = -boatTransform.forward * thirdPersonDistance;
            offset += Vector3.up * thirdPersonHeight;

            targetPosition = boatTransform.position + offset;

            // Look at boat with slight downward angle
            Vector3 lookTarget = boatTransform.position + Vector3.up * 1.5f;
            targetRotation = Quaternion.LookRotation(lookTarget - targetPosition);

            targetFOV = thirdPersonFOV;
        }

        private void UpdateBroadcastCamera()
        {
            // Cinematic camera that orbits slowly
            cinematicTime += Time.deltaTime * cinematicSpeed;

            float angle = cinematicTime * 30f; // Slow orbit
            Vector3 orbitPosition = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * broadcastDistance,
                broadcastHeight,
                Mathf.Sin(angle * Mathf.Deg2Rad) * broadcastDistance
            );

            targetPosition = boatTransform.position + orbitPosition;

            // Look at boat
            Vector3 lookTarget = boatTransform.position + Vector3.up * 2f;
            targetRotation = Quaternion.LookRotation(lookTarget - targetPosition);

            targetFOV = broadcastFOV;
        }

        private void UpdateTacticalCamera()
        {
            // Overhead view for tactical decision-making
            Vector3 overhead = boatTransform.position + Vector3.up * tacticalHeight;

            // Slightly behind boat
            overhead += -boatTransform.forward * 5f;

            targetPosition = overhead;

            // Look down at boat
            Vector3 lookTarget = boatTransform.position;
            targetRotation = Quaternion.LookRotation(lookTarget - targetPosition);

            targetFOV = tacticalFOV;
        }

        /// <summary>
        /// Switch to a different camera mode
        /// </summary>
        public void SetCameraMode(CameraMode mode)
        {
            currentMode = mode;
            cinematicTime = 0f; // Reset cinematic timer
        }

        /// <summary>
        /// Cycle to next camera mode
        /// </summary>
        public void CycleCamera()
        {
            int nextMode = ((int)currentMode + 1) % System.Enum.GetValues(typeof(CameraMode)).Length;
            SetCameraMode((CameraMode)nextMode);
        }

        /// <summary>
        /// Set head look direction (for first person mode)
        /// </summary>
        public void SetHeadLook(float yaw, float pitch)
        {
            headLookAngle.x = Mathf.Clamp(yaw, -90f, 90f);
            headLookAngle.y = Mathf.Clamp(pitch, -60f, 60f);
        }

        /// <summary>
        /// Adjust third person distance
        /// </summary>
        public void AdjustThirdPersonDistance(float delta)
        {
            thirdPersonDistance = Mathf.Clamp(thirdPersonDistance + delta, 3f, 20f);
        }

        /// <summary>
        /// Get current camera mode
        /// </summary>
        public CameraMode GetCurrentMode()
        {
            return currentMode;
        }

        // Gizmos for debugging
        void OnDrawGizmos()
        {
            if (!Application.isPlaying || boatTransform == null)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, boatTransform.position);
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
        }
    }
}
