using UnityEngine;

namespace DroneSimulator.PhysicsEngine
{
    public class WindSystem : MonoBehaviour
    {
        public static WindSystem Instance { get; private set; }

        [Header("Base Wind")]
        [Tooltip("Base wind direction vector")]
        public Vector3 baseWindDirection = new Vector3(1f, 0f, 0.5f);
        [Tooltip("Base wind speed in meters per second (m/s)")]
        public float baseWindSpeed = 3.5f;

        [Header("Gusts & Turbulence")]
        [Tooltip("Turbulence intensity scale")]
        public float turbulenceScale = 2.0f;
        [Tooltip("Frequency of Perlin noise gusts")]
        public float gustFrequency = 0.4f;

        [Header("Thermal Updrafts")]
        [Tooltip("Thermal updraft strength near ground/sunlit terrain")]
        public float thermalStrength = 1.0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public Vector3 GetWindAtPosition(Vector3 worldPos)
        {
            float time = Time.time * gustFrequency;
            
            // 3D Simplex-like Perlin sampling
            float noiseX = Mathf.PerlinNoise(worldPos.x * 0.02f + time, worldPos.z * 0.02f) - 0.5f;
            float noiseZ = Mathf.PerlinNoise(worldPos.z * 0.02f - time, worldPos.x * 0.02f) - 0.5f;
            float noiseY = Mathf.PerlinNoise(worldPos.x * 0.05f, worldPos.y * 0.05f + time) - 0.3f;

            Vector3 baseVec = baseWindDirection.normalized * baseWindSpeed;
            Vector3 gustVec = new Vector3(noiseX, noiseY * 0.5f, noiseZ) * (turbulenceScale * 2f);

            // Updraft when low to ground
            float updraft = 0f;
            if (worldPos.y < 30f)
            {
                updraft = Mathf.Max(0f, (30f - worldPos.y) / 30f) * thermalStrength * Mathf.Max(0f, noiseY);
            }

            return baseVec + gustVec + new Vector3(0f, updraft, 0f);
        }
    }
}

