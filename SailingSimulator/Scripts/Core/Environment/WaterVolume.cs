using UnityEngine;

namespace SailingSimulator.Core.Environment
{
    /// <summary>
    /// Represents a 3D volume of water for visual and physics purposes
    /// Attach this to a cube GameObject to create a water volume
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class WaterVolume : MonoBehaviour
    {
        [Header("Water Volume Settings")]
        [SerializeField] private float waterSurfaceY = 0f; // Y-coordinate of water surface
        [SerializeField] private float waterDepth = 10f; // How deep the water is

        [Header("Visual Settings")]
        [SerializeField] private Color waterColor = new Color(0.1f, 0.4f, 0.7f, 0.5f);
        [SerializeField] private bool showWaterSurface = true;

        private BoxCollider waterCollider;
        private WaterSystem waterSystem;
        private GameObject waterSurfaceQuad;

        void Start()
        {
            SetupWaterVolume();
            SetupWaterSurface();

            // Make sure WaterSystem knows the water level
            waterSystem = GetComponent<WaterSystem>();
            if (waterSystem != null)
            {
                Debug.Log($"Water volume created: Surface at Y={waterSurfaceY}, Depth={waterDepth}m");
            }
        }

        private void SetupWaterVolume()
        {
            // Setup the box collider as a trigger
            waterCollider = GetComponent<BoxCollider>();
            waterCollider.isTrigger = true;

            // Position the water volume so the top is at waterSurfaceY
            Vector3 pos = transform.position;
            pos.y = waterSurfaceY - (waterDepth / 2f);
            transform.position = pos;

            // Set the scale for the water depth
            Vector3 scale = transform.localScale;
            scale.y = waterDepth;
            transform.localScale = scale;

            // Apply water material
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material waterMat = renderer.material;
                waterMat.color = waterColor;

                // Make sure it's using transparent rendering
                waterMat.SetFloat("_Mode", 3); // Transparent
                waterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                waterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                waterMat.SetInt("_ZWrite", 0);
                waterMat.DisableKeyword("_ALPHATEST_ON");
                waterMat.EnableKeyword("_ALPHABLEND_ON");
                waterMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                waterMat.renderQueue = 3000;
            }
        }

        private void SetupWaterSurface()
        {
            if (!showWaterSurface) return;

            // Create a quad for the water surface
            waterSurfaceQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            waterSurfaceQuad.name = "WaterSurface";
            waterSurfaceQuad.transform.SetParent(transform);
            waterSurfaceQuad.transform.localPosition = new Vector3(0, waterDepth / 2f, 0);
            waterSurfaceQuad.transform.localRotation = Quaternion.Euler(90, 0, 0);

            // Scale to match water volume
            float scaleX = transform.localScale.x;
            float scaleZ = transform.localScale.z;
            waterSurfaceQuad.transform.localScale = new Vector3(scaleX, scaleZ, 1);

            // Apply slightly more opaque material to surface
            MeshRenderer surfaceRenderer = waterSurfaceQuad.GetComponent<MeshRenderer>();
            if (surfaceRenderer != null)
            {
                Material surfaceMat = new Material(Shader.Find("Standard"));
                Color surfaceColor = waterColor;
                surfaceColor.a = 0.8f; // More opaque
                surfaceMat.color = surfaceColor;
                surfaceMat.SetFloat("_Metallic", 0.8f);
                surfaceMat.SetFloat("_Glossiness", 0.95f);
                surfaceRenderer.material = surfaceMat;
            }

            // Remove collider from surface quad (we only want the volume collider)
            Destroy(waterSurfaceQuad.GetComponent<Collider>());
        }

        /// <summary>
        /// Check if a point is underwater
        /// </summary>
        public bool IsUnderwater(Vector3 worldPosition)
        {
            return worldPosition.y < waterSurfaceY;
        }

        /// <summary>
        /// Get depth below water surface
        /// </summary>
        public float GetDepthBelow(Vector3 worldPosition)
        {
            return Mathf.Max(0, waterSurfaceY - worldPosition.y);
        }

        /// <summary>
        /// Get the water surface Y coordinate
        /// </summary>
        public float GetWaterSurfaceY()
        {
            return waterSurfaceY;
        }

        // Visualization
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.5f, 0.9f, 0.3f);

            // Draw water volume bounds
            Vector3 size = transform.localScale;
            size.y = waterDepth;
            Gizmos.DrawCube(transform.position, size);

            // Draw water surface line
            Gizmos.color = Color.cyan;
            Vector3 surfacePos = transform.position;
            surfacePos.y = waterSurfaceY;
            Gizmos.DrawWireCube(surfacePos, new Vector3(size.x, 0.1f, size.z));
        }

        void OnDrawGizmosSelected()
        {
            // Show water surface when selected
            Gizmos.color = new Color(0, 1, 1, 0.5f);
            Vector3 size = transform.localScale;
            Vector3 surfacePos = transform.position;
            surfacePos.y = waterSurfaceY;
            Gizmos.DrawCube(surfacePos, new Vector3(size.x, 0.05f, size.z));

            // Label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(surfacePos, $"Water Surface (Y = {waterSurfaceY})");
            #endif
        }
    }
}
