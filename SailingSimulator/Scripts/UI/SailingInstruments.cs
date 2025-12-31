using UnityEngine;
using UnityEngine.UI;
using SailingSimulator.Core.Boats;
using SailingSimulator.Core.Environment;
using TMPro; // TextMeshPro for better text rendering

namespace SailingSimulator.UI
{
    /// <summary>
    /// Displays sailing performance instruments and data
    /// Similar to real sailing instruments
    /// </summary>
    public class SailingInstruments : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BaseBoat boat;
        [SerializeField] private WindSystem windSystem;

        [Header("UI Text Elements - Basic Data")]
        [SerializeField] private TextMeshProUGUI boatSpeedText;
        [SerializeField] private TextMeshProUGUI heelAngleText;
        [SerializeField] private TextMeshProUGUI headingText;

        [Header("UI Text Elements - Wind Data")]
        [SerializeField] private TextMeshProUGUI apparentWindAngleText;
        [SerializeField] private TextMeshProUGUI apparentWindSpeedText;
        [SerializeField] private TextMeshProUGUI trueWindAngleText;
        [SerializeField] private TextMeshProUGUI trueWindSpeedText;

        [Header("UI Text Elements - Performance")]
        [SerializeField] private TextMeshProUGUI vmgUpwindText;
        [SerializeField] private TextMeshProUGUI vmgDownwindText;
        [SerializeField] private TextMeshProUGUI targetSpeedText;

        [Header("Visual Indicators")]
        [SerializeField] private Image windArrow;
        [SerializeField] private Image heelIndicator;
        [SerializeField] private Slider travelerPositionSlider;

        [Header("Colors")]
        [SerializeField] private Color optimalColor = Color.green;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color dangerColor = Color.red;

        void Start()
        {
            if (boat == null)
                boat = FindObjectOfType<BaseBoat>();

            if (windSystem == null)
                windSystem = FindObjectOfType<WindSystem>();
        }

        void Update()
        {
            if (boat == null)
                return;

            UpdateBasicData();
            UpdateWindData();
            UpdatePerformanceData();
            UpdateVisualIndicators();
        }

        private void UpdateBasicData()
        {
            // Boat speed
            float speedKnots = boat.GetBoatSpeedKnots();
            if (boatSpeedText != null)
            {
                boatSpeedText.text = $"{speedKnots:F1} kt";
                boatSpeedText.color = GetSpeedColor(speedKnots);
            }

            // Heel angle
            float heelAngle = boat.GetHeelAngle();
            if (heelAngleText != null)
            {
                heelAngleText.text = $"{Mathf.Abs(heelAngle):F0}°";
                heelAngleText.color = GetHeelColor(heelAngle);
            }

            // Heading (compass)
            float heading = boat.transform.eulerAngles.y;
            if (headingText != null)
            {
                headingText.text = $"{heading:F0}°";
            }
        }

        private void UpdateWindData()
        {
            // Apparent Wind Angle
            float awa = boat.GetApparentWindAngle();
            if (apparentWindAngleText != null)
            {
                string side = awa > 0 ? "S" : "P"; // Starboard or Port
                apparentWindAngleText.text = $"{Mathf.Abs(awa):F0}° {side}";
            }

            // Apparent Wind Speed
            float aws = boat.GetApparentWindSpeed();
            if (apparentWindSpeedText != null)
            {
                apparentWindSpeedText.text = $"{aws:F1} kt";
            }

            // True Wind Angle
            float twa = boat.GetTrueWindAngle();
            if (trueWindAngleText != null)
            {
                string side = twa > 0 ? "S" : "P";
                trueWindAngleText.text = $"{Mathf.Abs(twa):F0}° {side}";
            }

            // True Wind Speed
            if (windSystem != null && trueWindSpeedText != null)
            {
                float tws = windSystem.GetWindSpeedKnots();
                trueWindSpeedText.text = $"{tws:F1} kt";
            }
        }

        private void UpdatePerformanceData()
        {
            // VMG Upwind
            float vmgUp = boat.GetVMGUpwind();
            if (vmgUpwindText != null)
            {
                vmgUpwindText.text = $"VMG ↑: {vmgUp:F1} kt";
                vmgUpwindText.color = vmgUp > 0 ? optimalColor : Color.gray;
            }

            // VMG Downwind
            float vmgDown = boat.GetVMGDownwind();
            if (vmgDownwindText != null)
            {
                vmgDownwindText.text = $"VMG ↓: {vmgDown:F1} kt";
                vmgDownwindText.color = vmgDown > 0 ? optimalColor : Color.gray;
            }

            // Target speed (from polar diagram - simplified)
            if (targetSpeedText != null)
            {
                float targetSpeed = CalculateTargetSpeed();
                targetSpeedText.text = $"Target: {targetSpeed:F1} kt";
            }
        }

        private void UpdateVisualIndicators()
        {
            // Wind arrow - points to apparent wind
            if (windArrow != null)
            {
                float awa = boat.GetApparentWindAngle();
                windArrow.rectTransform.rotation = Quaternion.Euler(0, 0, -awa);
            }

            // Heel indicator - visual bar showing heel
            if (heelIndicator != null)
            {
                float heelAngle = boat.GetHeelAngle();
                heelIndicator.fillAmount = Mathf.Abs(heelAngle) / 45f; // Max display at 45°
                heelIndicator.color = GetHeelColor(heelAngle);

                // Flip for port/starboard
                if (heelAngle < 0)
                {
                    heelIndicator.rectTransform.localScale = new Vector3(-1, 1, 1);
                }
                else
                {
                    heelIndicator.rectTransform.localScale = new Vector3(1, 1, 1);
                }
            }

            // Traveler position
            if (travelerPositionSlider != null)
            {
                // Would need to get traveler position from boat
                // This is simplified
                travelerPositionSlider.value = 0.5f; // Placeholder
            }
        }

        private float CalculateTargetSpeed()
        {
            // Simplified polar diagram
            // In reality, this would use actual boat polars
            float twa = Mathf.Abs(boat.GetTrueWindAngle());
            float tws = windSystem != null ? windSystem.GetWindSpeedKnots() : 10f;

            // Very simplified target speed calculation
            if (twa < 45f) // Close-hauled
            {
                return tws * 0.4f;
            }
            else if (twa < 90f) // Reaching
            {
                return tws * 0.7f;
            }
            else if (twa < 135f) // Broad reach
            {
                return tws * 0.8f;
            }
            else // Running
            {
                return tws * 0.6f;
            }
        }

        private Color GetSpeedColor(float speedKnots)
        {
            float targetSpeed = CalculateTargetSpeed();

            if (speedKnots >= targetSpeed * 0.9f)
                return optimalColor; // Within 90% of target
            else if (speedKnots >= targetSpeed * 0.7f)
                return warningColor; // Within 70% of target
            else
                return dangerColor; // Below 70% of target
        }

        private Color GetHeelColor(float heelAngle)
        {
            float absHeel = Mathf.Abs(heelAngle);

            if (absHeel < 15f)
                return optimalColor; // Good heel
            else if (absHeel < 25f)
                return warningColor; // Moderate heel
            else
                return dangerColor; // Excessive heel
        }

        /// <summary>
        /// Create a simple instruments panel dynamically if UI elements not assigned
        /// </summary>
        public void CreateDefaultUI()
        {
            // This would create UI elements programmatically
            // For now, we'll use the OnGUI fallback in the input controller
            Debug.Log("Default UI creation not implemented - use OnGUI fallback");
        }
    }
}
