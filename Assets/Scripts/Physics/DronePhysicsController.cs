using UnityEngine;

namespace DroneSimulator.PhysicsEngine
{
    public enum FlightMode
    {
        Angle = 0,       // Stabilized / Self-leveling (DJI / Beginner friendly)
        Acro = 1,        // Rate mode / Free rotation (FPV Freestyle & Racing)
        AltitudeHold = 2 // Barometer assisted altitude lock
    }

    [RequireComponent(typeof(Rigidbody))]
    public class DronePhysicsController : MonoBehaviour
    {
        [Header("Flight Mode & Control")]
        public FlightMode currentMode = FlightMode.Angle;
        
        [Header("Motors (Quad X Configuration)")]
        public MotorRotor motorFrontLeft;
        public MotorRotor motorFrontRight;
        public MotorRotor motorRearLeft;
        public MotorRotor motorRearRight;

        [Header("Flight Dynamics & Limits")]
        [Tooltip("Total drone mass in kg (standard 5-inch drone is ~0.65kg with battery)")]
        public float droneMassKg = 0.68f;

        [Tooltip("Maximum tilt angle in Angle Mode (degrees)")]
        public float maxTiltAngle = 45f;

        [Tooltip("Maximum angular rotation rate in Acro Mode (degrees/second)")]
        public float maxAcroAngularRate = 600f;

        [Tooltip("Air density (kg/m3) at sea level")]
        public float airDensity = 1.225f;

        [Tooltip("Aerodynamic drag area coefficient")]
        public float dragCoefficient = 0.35f;

        [Header("Ground Effect")]
        public bool enableGroundEffect = true;
        public float rotorRadius = 0.065f; // ~5-inch prop radius
        public LayerMask groundLayers = ~0;

        [Header("PID Tuning - Angle Mode")]
        public PIDController pitchAnglePID = new PIDController(4.5f, 0.2f, 1.2f, -1f, 1f, 0.5f);
        public PIDController rollAnglePID = new PIDController(4.5f, 0.2f, 1.2f, -1f, 1f, 0.5f);
        public PIDController yawRatePID = new PIDController(2.8f, 0.1f, 0.5f, -1f, 1f, 0.5f);

        [Header("PID Tuning - Altitude Hold")]
        public PIDController altitudePID = new PIDController(1.5f, 0.1f, 0.8f, -0.4f, 0.5f, 0.3f);
        private float _targetAltitude;

        [Header("LiPo Battery Simulation (4S 1500mAh)")]
        public float batteryCapacityMAh = 1500f;
        public float batteryConsumedMAh = 0f;
        public float batteryCellCount = 4f; // 4S
        public float internalResistanceOhms = 0.025f;
        public float currentVoltage = 16.8f;
        public float currentAmperage = 0f;

        [Header("Telemetry Outputs")]
        public float currentSpeedKmh;
        public float altitudeAGL;
        public float altitudeMSL;
        public float pitchDegrees;
        public float rollDegrees;
        public float yawDegrees;
        public float rawThrottleInput;

        // References
        private Rigidbody _rb;
        private Vector4 _motorCommands; // (FL, FR, RL, RR)

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.mass = droneMassKg;
            _rb.drag = 0.05f;
            _rb.angularDrag = 0.8f;
            _rb.useGravity = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            _targetAltitude = transform.position.y;
        }

        private void Start()
        {
            // Auto configure motor spin directions if not set
            if (motorFrontLeft) motorFrontLeft.spinDirection = RotationDirection.Clockwise;
            if (motorFrontRight) motorFrontRight.spinDirection = RotationDirection.CounterClockwise;
            if (motorRearLeft) motorRearLeft.spinDirection = RotationDirection.CounterClockwise;
            if (motorRearRight) motorRearRight.spinDirection = RotationDirection.Clockwise;
        }

        private void Update()
        {
            HandleInputs();
            UpdateBattery(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            CalculateAttitude();
            CalculateFlightForces(dt);
            ApplyGroundEffect();
            ApplyAerodynamicDrag();
            ApplyWindForces();
            UpdateTelemetry();
        }

        private void HandleInputs()
        {
            // Read inputs (WASD + Arrow keys / Joysticks)
            // Left Stick: Throttle (W/S or Up/Down), Yaw (A/D or Left/Right)
            // Right Stick: Pitch (I/K or Num8/Num2), Roll (J/L or Num4/Num6)

            float throttle = Input.GetAxis("Vertical"); // W/S or Left Stick Y
            float yawInput = Input.GetAxis("Horizontal"); // A/D or Left Stick X
            
            float pitchInput = 0f;
            if (Input.GetKey(KeyCode.I) || Input.GetKey(KeyCode.Keypad8)) pitchInput += 1f;
            if (Input.GetKey(KeyCode.K) || Input.GetKey(KeyCode.Keypad2)) pitchInput -= 1f;

            float rollInput = 0f;
            if (Input.GetKey(KeyCode.L) || Input.GetKey(KeyCode.Keypad6)) rollInput += 1f;
            if (Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.Keypad4)) rollInput -= 1f;

            // Spacebar throttle boost / Shift descent
            if (Input.GetKey(KeyCode.Space)) throttle = Mathf.Max(throttle, 0.85f);
            if (Input.GetKey(KeyCode.LeftShift)) throttle = Mathf.Min(throttle, 0.15f);

            // Throttle mapped to [0, 1] range (Idle is 0.05 for prop spinning)
            rawThrottleInput = Mathf.Clamp01(throttle * 0.5f + 0.5f);
            if (Input.GetKey(KeyCode.LeftControl)) rawThrottleInput = 0f; // Arm/Disarm kill switch

            // Flight Mode toggle with 'M'
            if (Input.GetKeyDown(KeyCode.M))
            {
                currentMode = (FlightMode)(((int)currentMode + 1) % 3);
                if (currentMode == FlightMode.AltitudeHold) _targetAltitude = transform.position.y;
                Debug.Log($"Flight Mode Switched to: {currentMode}");
            }

            // Compute motor mix based on flight mode
            ComputeMotorMix(rawThrottleInput, pitchInput, rollInput, yawInput, Time.deltaTime);
        }

        private void ComputeMotorMix(float throttle, float pitchIn, float rollIn, float yawIn, float dt)
        {
            float pitchCmd = 0f;
            float rollCmd = 0f;
            float yawCmd = 0f;

            if (currentMode == FlightMode.Angle)
            {
                // Self leveling: Target angle from input
                float targetPitch = -pitchIn * maxTiltAngle;
                float targetRoll = rollIn * maxTiltAngle;

                pitchCmd = pitchAnglePID.Update(targetPitch, pitchDegrees, dt);
                rollCmd = rollAnglePID.Update(targetRoll, rollDegrees, dt);
                yawCmd = yawRatePID.Update(yawIn * 180f, _rb.angularVelocity.y * Mathf.Rad2Deg, dt);
            }
            else if (currentMode == FlightMode.Acro)
            {
                // Acro mode: Target angular rate
                float targetPitchRate = -pitchIn * maxAcroAngularRate;
                float targetRollRate = rollIn * maxAcroAngularRate;
                float targetYawRate = yawIn * (maxAcroAngularRate * 0.6f);

                pitchCmd = pitchAnglePID.Update(targetPitchRate, _rb.angularVelocity.x * Mathf.Rad2Deg, dt);
                rollCmd = rollAnglePID.Update(targetRollRate, _rb.angularVelocity.z * Mathf.Rad2Deg, dt);
                yawCmd = yawRatePID.Update(targetYawRate, _rb.angularVelocity.y * Mathf.Rad2Deg, dt);
            }
            else if (currentMode == FlightMode.AltitudeHold)
            {
                float altCorrection = altitudePID.Update(_targetAltitude, transform.position.y, dt);
                throttle = Mathf.Clamp01(0.48f + altCorrection); // 0.48 is hover thrust baseline

                float targetPitch = -pitchIn * (maxTiltAngle * 0.6f);
                float targetRoll = rollIn * (maxTiltAngle * 0.6f);

                pitchCmd = pitchAnglePID.Update(targetPitch, pitchDegrees, dt);
                rollCmd = rollAnglePID.Update(targetRoll, rollDegrees, dt);
                yawCmd = yawRatePID.Update(yawIn * 120f, _rb.angularVelocity.y * Mathf.Rad2Deg, dt);
            }

            // Motor Quad-X mixing matrix
            // FL = Throttle - Pitch + Roll - Yaw
            // FR = Throttle - Pitch - Roll + Yaw
            // RL = Throttle + Pitch + Roll + Yaw
            // RR = Throttle + Pitch - Roll - Yaw
            _motorCommands.x = Mathf.Clamp01(throttle - pitchCmd + rollCmd - yawCmd); // FL
            _motorCommands.y = Mathf.Clamp01(throttle - pitchCmd - rollCmd + yawCmd); // FR
            _motorCommands.z = Mathf.Clamp01(throttle + pitchCmd + rollCmd + yawCmd); // RL
            _motorCommands.w = Mathf.Clamp01(throttle + pitchCmd - rollCmd - yawCmd); // RR
        }

        private void CalculateFlightForces(float dt)
        {
            if (motorFrontLeft) motorFrontLeft.UpdateMotor(_motorCommands.x, dt);
            if (motorFrontRight) motorFrontRight.UpdateMotor(_motorCommands.y, dt);
            if (motorRearLeft) motorRearLeft.UpdateMotor(_motorCommands.z, dt);
            if (motorRearRight) motorRearRight.UpdateMotor(_motorCommands.w, dt);

            // Apply forces at motor positions for realistic aerodynamic torque
            ApplyMotorForce(motorFrontLeft);
            ApplyMotorForce(motorFrontRight);
            ApplyMotorForce(motorRearLeft);
            ApplyMotorForce(motorRearRight);
        }

        private void ApplyMotorForce(MotorRotor motor)
        {
            if (motor == null) return;
            Vector3 force = motor.GetThrustVector();
            Vector3 torque = motor.GetTorqueVector();

            _rb.AddForceAtPosition(force, motor.transform.position, ForceMode.Force);
            _rb.AddTorque(torque, ForceMode.Force);
        }

        private void ApplyGroundEffect()
        {
            if (!enableGroundEffect) return;

            RaycastHit hit;
            if (Physics.Raycast(transform.position, -Vector3.up, out hit, 3.0f, groundLayers))
            {
                altitudeAGL = hit.distance;
                float height = Mathf.Max(0.08f, hit.distance);
                
                // Cheeseman and Bennett ground effect formula: T / T_inf = 1 / (1 - (R / (4h))^2)
                float ratio = rotorRadius / (4f * height);
                if (ratio < 0.9f)
                {
                    float factor = 1f / (1f - (ratio * ratio));
                    float groundEffectBoost = Mathf.Clamp(factor - 1f, 0f, 0.45f);
                    _rb.AddForce(transform.up * (groundEffectBoost * _rb.mass * 9.81f), ForceMode.Force);
                }
            }
            else
            {
                altitudeAGL = transform.position.y;
            }
        }

        private void ApplyAerodynamicDrag()
        {
            // Aerodynamic drag opposes velocity: F_drag = 0.5 * rho * Cd * A * v^2
            Vector3 vel = _rb.velocity;
            float speedSq = vel.sqrMagnitude;
            Vector3 dragForce = -vel.normalized * (0.5f * airDensity * dragCoefficient * speedSq);
            _rb.AddForce(dragForce, ForceMode.Force);

            // Rotational damping from air resistance on propellers
            _rb.AddTorque(-_rb.angularVelocity * 0.15f, ForceMode.Force);
        }

        private void ApplyWindForces()
        {
            if (WindSystem.Instance == null) return;
            Vector3 wind = WindSystem.Instance.GetWindAtPosition(transform.position);
            Vector3 relativeWind = wind - _rb.velocity;
            _rb.AddForce(relativeWind * 0.4f, ForceMode.Force);
        }

        private void CalculateAttitude()
        {
            Vector3 euler = transform.eulerAngles;
            pitchDegrees = (euler.x > 180f) ? euler.x - 360f : euler.x;
            rollDegrees = (euler.z > 180f) ? euler.z - 360f : euler.z;
            yawDegrees = euler.y;
        }

        private void UpdateTelemetry()
        {
            currentSpeedKmh = _rb.velocity.magnitude * 3.6f;
            altitudeMSL = transform.position.y;
        }

        private void UpdateBattery(float dt)
        {
            // Calculate total current draw
            float idleDraw = 0.4f; // FPV VTx, Camera, Flight Controller electronics
            float motorDraw = (_motorCommands.x + _motorCommands.y + _motorCommands.z + _motorCommands.w) * 18f; // ~72A max burst
            currentAmperage = idleDraw + motorDraw;

            // Voltage sag: V = V_resting - (I * R_internal)
            float chargeFraction = Mathf.Clamp01(1f - (batteryConsumedMAh / batteryCapacityMAh));
            float restingCellVolt = Mathf.Lerp(3.4f, 4.2f, chargeFraction);
            float nominalRestingVolt = restingCellVolt * batteryCellCount;
            currentVoltage = Mathf.Max(12.0f, nominalRestingVolt - (currentAmperage * internalResistanceOhms));

            // Accumulate mAh consumed
            batteryConsumedMAh += (currentAmperage * 1000f) * (dt / 3600f);
        }
    }
}

