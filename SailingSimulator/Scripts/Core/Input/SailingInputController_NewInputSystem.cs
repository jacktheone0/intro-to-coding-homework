using UnityEngine;
using UnityEngine.InputSystem; // New Input System
using SailingSimulator.Core.Boats;

namespace SailingSimulator.Core.Input
{
    /// <summary>
    /// Input handling for sailing controls using NEW Unity Input System
    /// NOTE: Requires Input System package and input actions asset
    /// </summary>
    public class SailingInputController_NewInputSystem : MonoBehaviour
    {
        [Header("Boat Reference")]
        [SerializeField] private BaseBoat currentBoat;

        [Header("Camera Reference")]
        [SerializeField] private CameraController cameraController;

        [Header("Input Settings")]
        [SerializeField] private float rudderSensitivity = 30f;
        [SerializeField] private float sheetAdjustSpeed = 0.5f;

        // Input values
        private float rudderInput = 0f;
        private float mainsheetInput = 0.5f;
        private float jibSheetInput = 0.5f;
        private Vector2 crewWeight;
        private float travelerInput = 0f;

        // Boat references
        private ILCABoat ilcaBoat;
        private I420Boat i420Boat;
        private Boat29er boat29er;

        void Start()
        {
            if (currentBoat == null)
                currentBoat = FindObjectOfType<BaseBoat>();

            ilcaBoat = currentBoat as ILCABoat;
            i420Boat = currentBoat as I420Boat;
            boat29er = currentBoat as Boat29er;

            if (cameraController == null)
                cameraController = FindObjectOfType<CameraController>();
        }

        void Update()
        {
            if (currentBoat == null)
                return;

            HandleSteering();
            HandleSailControls();
            HandleCrewWeight();
            HandleBoatSpecificControls();
        }

        // New Input System callbacks
        public void OnSteer(InputAction.CallbackContext context)
        {
            rudderInput = context.ReadValue<float>();
        }

        public void OnMainsheet(InputAction.CallbackContext context)
        {
            float input = context.ReadValue<float>();
            mainsheetInput += input * sheetAdjustSpeed * Time.deltaTime;
            mainsheetInput = Mathf.Clamp01(mainsheetInput);
        }

        public void OnJibSheet(InputAction.CallbackContext context)
        {
            float input = context.ReadValue<float>();
            jibSheetInput += input * sheetAdjustSpeed * Time.deltaTime;
            jibSheetInput = Mathf.Clamp01(jibSheetInput);
        }

        public void OnCrewWeight(InputAction.CallbackContext context)
        {
            crewWeight = context.ReadValue<Vector2>();
        }

        public void OnTraveler(InputAction.CallbackContext context)
        {
            travelerInput = context.ReadValue<float>();
        }

        public void OnCycleCamera(InputAction.CallbackContext context)
        {
            if (context.performed && cameraController != null)
            {
                cameraController.CycleCamera();
            }
        }

        public void OnDeploySpinnaker(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            if (i420Boat != null)
                i420Boat.DeploySpinnaker(!i420Boat.IsSpinnakerDeployed());
            else if (boat29er != null)
                boat29er.DeployAsymSpinnaker(!boat29er.IsAsymSpinnakerDeployed());
        }

        public void OnTrapeze(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            if (i420Boat != null)
                i420Boat.SetCrewOnTrapeze(!i420Boat.IsCrewOnTrapeze());
            else if (boat29er != null)
                boat29er.SetCrewOnTrapeze(!boat29er.IsCrewOnTrapeze());
        }

        // Apply inputs
        private void HandleSteering()
        {
            float rudderAngle = rudderInput * rudderSensitivity;
            currentBoat.SetRudderAngle(rudderAngle);
        }

        private void HandleSailControls()
        {
            if (ilcaBoat != null)
                ilcaBoat.SetMainsheetTension(mainsheetInput);
            else if (i420Boat != null)
            {
                i420Boat.SetMainsheetTension(mainsheetInput);
                i420Boat.SetJibSheetTension(jibSheetInput);
            }
            else if (boat29er != null)
            {
                boat29er.SetMainsheetTension(mainsheetInput);
                boat29er.SetJibSheetTension(jibSheetInput);
            }
        }

        private void HandleCrewWeight()
        {
            if (ilcaBoat != null)
                ilcaBoat.SetCrewPosition(crewWeight.x, crewWeight.y);
            else if (i420Boat != null)
                i420Boat.SetSkipperPosition(crewWeight.x, crewWeight.y);
        }

        private void HandleBoatSpecificControls()
        {
            if (Mathf.Abs(travelerInput) > 0.01f)
            {
                if (ilcaBoat != null)
                    ilcaBoat.AdjustTraveler(travelerInput);
                else if (i420Boat != null)
                    i420Boat.AdjustTraveler(travelerInput);
                else if (boat29er != null)
                    boat29er.AdjustTraveler(travelerInput);
            }
        }

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            GUILayout.Label("=== NEW INPUT SYSTEM ===");
            GUILayout.Label("See Input Actions for controls");
            GUILayout.Label("");
            if (currentBoat != null)
            {
                GUILayout.Label($"Speed: {currentBoat.GetBoatSpeedKnots():F1} kt");
                GUILayout.Label($"Heel: {currentBoat.GetHeelAngle():F1}°");
                GUILayout.Label($"AWA: {currentBoat.GetApparentWindAngle():F0}°");
            }
            GUILayout.EndArea();
        }
    }
}
