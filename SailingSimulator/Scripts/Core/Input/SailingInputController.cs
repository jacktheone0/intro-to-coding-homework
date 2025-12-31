using UnityEngine;
using SailingSimulator.Core.Boats;

namespace SailingSimulator.Core.Input
{
    /// <summary>
    /// Input handling for sailing controls
    /// Supports keyboard + mouse and game controller
    /// </summary>
    public class SailingInputController : MonoBehaviour
    {
        [Header("Boat Reference")]
        [SerializeField] private BaseBoat currentBoat;

        [Header("Camera Reference")]
        [SerializeField] private CameraController cameraController;

        [Header("Input Settings")]
        [SerializeField] private bool useController = false;
        [SerializeField] private float rudderSensitivity = 30f;
        [SerializeField] private float sheetAdjustSpeed = 0.5f;
        [SerializeField] private float travelerAdjustSpeed = 1.0f;

        // Current control states
        private float rudderInput = 0f;
        private float mainsheetInput = 0.5f;
        private float jibSheetInput = 0.5f;
        private float crewAthwartships = 0f;
        private float crewForeAft = 0f;

        // Boat-specific references
        private ILCABoat ilcaBoat;
        private I420Boat i420Boat;
        private Boat29er boat29er;

        void Start()
        {
            if (currentBoat == null)
            {
                currentBoat = FindObjectOfType<BaseBoat>();
            }

            // Determine boat type
            ilcaBoat = currentBoat as ILCABoat;
            i420Boat = currentBoat as I420Boat;
            boat29er = currentBoat as Boat29er;

            if (cameraController == null)
            {
                cameraController = FindObjectOfType<CameraController>();
            }
        }

        void Update()
        {
            if (currentBoat == null)
                return;

            HandleSteeringInput();
            HandleSailControls();
            HandleCrewWeight();
            HandleBoatSpecificControls();
            HandleCameraControls();
            HandleUtilityControls();
        }

        private void HandleSteeringInput()
        {
            // Rudder control
            if (useController)
            {
                // Left stick X-axis for steering
                rudderInput = UnityEngine.Input.GetAxis("Horizontal");
            }
            else
            {
                // Keyboard: A/D or Left/Right arrows
                rudderInput = 0f;
                if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
                    rudderInput = -1f;
                if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
                    rudderInput = 1f;
            }

            // Apply rudder
            float rudderAngle = rudderInput * rudderSensitivity;
            currentBoat.SetRudderAngle(rudderAngle);
        }

        private void HandleSailControls()
        {
            // Mainsheet control
            if (useController)
            {
                // Right trigger = trim in, Left trigger = ease out
                float trimIn = UnityEngine.Input.GetAxis("TrimIn"); // Map to RT
                float easeOut = UnityEngine.Input.GetAxis("EaseOut"); // Map to LT

                mainsheetInput += (trimIn - easeOut) * sheetAdjustSpeed * Time.deltaTime;
            }
            else
            {
                // Keyboard: W/S for mainsheet
                if (UnityEngine.Input.GetKey(KeyCode.W))
                    mainsheetInput += sheetAdjustSpeed * Time.deltaTime;
                if (UnityEngine.Input.GetKey(KeyCode.S))
                    mainsheetInput -= sheetAdjustSpeed * Time.deltaTime;
            }

            mainsheetInput = Mathf.Clamp01(mainsheetInput);

            // Apply to boat
            if (ilcaBoat != null)
            {
                ilcaBoat.SetMainsheetTension(mainsheetInput);
            }
            else if (i420Boat != null)
            {
                i420Boat.SetMainsheetTension(mainsheetInput);
            }
            else if (boat29er != null)
            {
                boat29er.SetMainsheetTension(mainsheetInput);
            }

            // Jib sheet (for i420 and 29er)
            if (i420Boat != null || boat29er != null)
            {
                if (UnityEngine.Input.GetKey(KeyCode.E))
                    jibSheetInput += sheetAdjustSpeed * Time.deltaTime;
                if (UnityEngine.Input.GetKey(KeyCode.Q))
                    jibSheetInput -= sheetAdjustSpeed * Time.deltaTime;

                jibSheetInput = Mathf.Clamp01(jibSheetInput);

                if (i420Boat != null)
                    i420Boat.SetJibSheetTension(jibSheetInput);
                else if (boat29er != null)
                    boat29er.SetJibSheetTension(jibSheetInput);
            }
        }

        private void HandleCrewWeight()
        {
            // Crew weight positioning
            if (useController)
            {
                // Right stick for crew weight
                crewAthwartships = UnityEngine.Input.GetAxis("CrewAthwartships");
                crewForeAft = UnityEngine.Input.GetAxis("CrewForeAft");
            }
            else
            {
                // Arrow keys or IJKL for crew weight
                crewAthwartships = 0f;
                crewForeAft = 0f;

                if (UnityEngine.Input.GetKey(KeyCode.LeftArrow) || UnityEngine.Input.GetKey(KeyCode.J))
                    crewAthwartships = -1f; // To port
                if (UnityEngine.Input.GetKey(KeyCode.RightArrow) || UnityEngine.Input.GetKey(KeyCode.L))
                    crewAthwartships = 1f; // To starboard
                if (UnityEngine.Input.GetKey(KeyCode.UpArrow) || UnityEngine.Input.GetKey(KeyCode.I))
                    crewForeAft = 1f; // Forward
                if (UnityEngine.Input.GetKey(KeyCode.DownArrow) || UnityEngine.Input.GetKey(KeyCode.K))
                    crewForeAft = -1f; // Aft
            }

            // Apply crew position
            if (ilcaBoat != null)
            {
                ilcaBoat.SetCrewPosition(crewAthwartships, crewForeAft);
            }
            else if (i420Boat != null)
            {
                i420Boat.SetSkipperPosition(crewAthwartships, crewForeAft);
                // Crew position could be controlled separately in multiplayer
            }
            else if (boat29er != null)
            {
                // Similar for 29er
            }
        }

        private void HandleBoatSpecificControls()
        {
            // Traveler control (T/G keys or D-pad)
            float travelerInput = 0f;

            if (UnityEngine.Input.GetKey(KeyCode.T))
                travelerInput = 1f; // To starboard
            if (UnityEngine.Input.GetKey(KeyCode.G))
                travelerInput = -1f; // To port

            if (Mathf.Abs(travelerInput) > 0.01f)
            {
                if (ilcaBoat != null)
                    ilcaBoat.AdjustTraveler(travelerInput);
                else if (i420Boat != null)
                    i420Boat.AdjustTraveler(travelerInput);
                else if (boat29er != null)
                    boat29er.AdjustTraveler(travelerInput);
            }

            // Centerboard/Daggerboard (Z/X keys)
            if (UnityEngine.Input.GetKeyDown(KeyCode.Z))
            {
                float current = currentBoat.GetType().GetProperty("centerboardExtension") != null ?
                    0.5f : 0.5f; // Simplified - would need proper getter
                currentBoat.SetCenterboardExtension(Mathf.Clamp01(current + 0.2f));
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.X))
            {
                float current = 0.5f;
                currentBoat.SetCenterboardExtension(Mathf.Clamp01(current - 0.2f));
            }

            // Spinnaker controls (i420 and 29er)
            if (i420Boat != null)
            {
                // Deploy/douse spinnaker
                if (UnityEngine.Input.GetKeyDown(KeyCode.P))
                {
                    i420Boat.DeploySpinnaker(!i420Boat.IsSpinnakerDeployed());
                }

                // Trapeze
                if (UnityEngine.Input.GetKeyDown(KeyCode.R))
                {
                    i420Boat.SetCrewOnTrapeze(!i420Boat.IsCrewOnTrapeze());
                }
            }
            else if (boat29er != null)
            {
                // Deploy/douse asymmetric spinnaker
                if (UnityEngine.Input.GetKeyDown(KeyCode.P))
                {
                    boat29er.DeployAsymSpinnaker(!boat29er.IsAsymSpinnakerDeployed());
                }

                // Trapeze (both crew)
                if (UnityEngine.Input.GetKeyDown(KeyCode.R))
                {
                    boat29er.SetCrewOnTrapeze(!boat29er.IsCrewOnTrapeze());
                }
                if (UnityEngine.Input.GetKeyDown(KeyCode.F))
                {
                    boat29er.SetSkipperOnTrapeze(!boat29er.IsSkipperOnTrapeze());
                }
            }

            // Vang control (V/B keys)
            if (UnityEngine.Input.GetKey(KeyCode.V))
            {
                if (ilcaBoat != null)
                {
                    float current = ilcaBoat.GetVangTension();
                    ilcaBoat.SetVangTension(Mathf.Clamp01(current + Time.deltaTime * 0.3f));
                }
            }
            if (UnityEngine.Input.GetKey(KeyCode.B))
            {
                if (ilcaBoat != null)
                {
                    float current = ilcaBoat.GetVangTension();
                    ilcaBoat.SetVangTension(Mathf.Clamp01(current - Time.deltaTime * 0.3f));
                }
            }
        }

        private void HandleCameraControls()
        {
            if (cameraController == null)
                return;

            // Cycle camera mode (C key or Select button)
            if (UnityEngine.Input.GetKeyDown(KeyCode.C))
            {
                cameraController.CycleCamera();
            }

            // Adjust third-person distance (Mouse wheel or shoulder buttons)
            float scrollDelta = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                cameraController.AdjustThirdPersonDistance(scrollDelta * 5f);
            }
        }

        private void HandleUtilityControls()
        {
            // Pause (Escape or Start button)
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                // Toggle pause menu
            }

            // Reset boat (R key - if not used for trapeze)
            if (UnityEngine.Input.GetKeyDown(KeyCode.Backspace))
            {
                // Reset boat position/rotation
                if (currentBoat != null)
                {
                    currentBoat.transform.position = Vector3.up * 0.5f;
                    currentBoat.transform.rotation = Quaternion.identity;
                }
            }
        }

        /// <summary>
        /// Display input hints on screen
        /// </summary>
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 500));
            GUILayout.Label("=== SAILING CONTROLS ===");
            GUILayout.Label("Steering: A/D or ← →");
            GUILayout.Label("Mainsheet: W (in) / S (out)");
            GUILayout.Label("Jib Sheet: E (in) / Q (out)");
            GUILayout.Label("Crew Weight: Arrow Keys or IJKL");
            GUILayout.Label("Traveler: T (starboard) / G (port)");
            GUILayout.Label("Centerboard: Z (down) / X (up)");
            GUILayout.Label("Spinnaker: P (deploy/douse)");
            GUILayout.Label("Trapeze: R (crew)");
            GUILayout.Label("Vang: V (more) / B (less)");
            GUILayout.Label("Camera: C (cycle modes)");
            GUILayout.Label("");
            GUILayout.Label($"Boat Speed: {currentBoat.GetBoatSpeedKnots():F1} knots");
            GUILayout.Label($"Heel: {currentBoat.GetHeelAngle():F1}°");
            GUILayout.Label($"AWA: {currentBoat.GetApparentWindAngle():F0}°");
            GUILayout.Label($"AWS: {currentBoat.GetApparentWindSpeed():F1} knots");
            GUILayout.EndArea();
        }
    }
}
