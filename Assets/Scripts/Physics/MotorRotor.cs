using UnityEngine;

namespace DroneSimulator.PhysicsEngine
{
    public enum RotationDirection
    {
        Clockwise = 1,
        CounterClockwise = -1
    }

    [System.Serializable]
    public class MotorRotor : MonoBehaviour
    {
        [Header("Motor Specifications")]
        [Tooltip("Motor position on the frame (FL, FR, RL, RR)")]
        public string motorID = "M1";
        public RotationDirection spinDirection = RotationDirection.Clockwise;
        
        [Tooltip("Maximum RPM at full throttle")]
        public float maxRPM = 28000f;
        
        [Tooltip("Maximum thrust produced by this motor at max RPM (in Newtons)")]
        public float maxThrustNewtons = 8.5f;

        [Tooltip("Motor reaction time constant (seconds to reach target RPM)")]
        [Range(0.01f, 0.15f)]
        public float motorResponseTime = 0.04f;

        [Tooltip("Propeller aerodynamic drag torque coefficient (causes yaw torque)")]
        public float torqueFactor = 0.0035f;

        [Header("Visual Propeller")]
        public Transform propellerTransform;
        public Renderer propellerRenderer;

        [Header("Runtime Telemetry")]
        [SerializeField] private float currentRPM;
        [SerializeField] private float currentThrust;
        [SerializeField] private float targetRPM;

        public float CurrentRPM => currentRPM;
        public float CurrentThrust => currentThrust;
        public float NormalizedRPM => Mathf.Clamp01(currentRPM / Mathf.Max(1f, maxRPM));

        public void UpdateMotor(float throttleInput, float dt)
        {
            throttleInput = Mathf.Clamp01(throttleInput);
            targetRPM = throttleInput * maxRPM;

            // First-order motor inertia lag
            float changeRate = (targetRPM - currentRPM) / Mathf.Max(0.001f, motorResponseTime);
            currentRPM += changeRate * dt;
            currentRPM = Mathf.Clamp(currentRPM, 0f, maxRPM);

            // Aerodynamic thrust is proportional to RPM squared
            float rpmRatio = currentRPM / maxRPM;
            currentThrust = rpmRatio * rpmRatio * maxThrustNewtons;

            // Visual propeller spin
            if (propellerTransform != null)
            {
                float angle = (float)spinDirection * currentRPM * 6f * dt; // 360 deg / 60s = 6
                propellerTransform.Rotate(Vector3.up, angle, Space.Self);
            }
        }

        public Vector3 GetThrustVector()
        {
            // Thrust pushes up along motor normal
            return transform.up * currentThrust;
        }

        public Vector3 GetTorqueVector()
        {
            // Reactive torque opposes rotation direction around normal axis
            float yawTorque = -(float)spinDirection * currentThrust * torqueFactor;
            return transform.up * yawTorque;
        }
    }
}

