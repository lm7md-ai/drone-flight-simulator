using System;
using UnityEngine;

namespace DroneSimulator.PhysicsEngine
{
    [System.Serializable]
    public class PIDController
    {
        [Header("Gains")]
        public float Kp = 2.0f;
        public float Ki = 0.5f;
        public float Kd = 0.8f;

        [Header("Limits")]
        public float outputMin = -100f;
        public float outputMax = 100f;
        public float integralLimit = 20f;

        private float _integral;
        private float _lastError;
        private bool _isFirstRun = true;

        public PIDController(float kp, float ki, float kd, float min, float max, float intLimit)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;
            outputMin = min;
            outputMax = max;
            integralLimit = intLimit;
            Reset();
        }

        public void Reset()
        {
            _integral = 0f;
            _lastError = 0f;
            _isFirstRun = true;
        }

        public float Update(float setpoint, float actual, float dt)
        {
            if (dt <= 0.00001f) return 0f;

            float error = setpoint - actual;

            // Proportional term
            float p = Kp * error;

            // Integral term with anti-windup clamping
            _integral += error * dt;
            _integral = Mathf.Clamp(_integral, -integralLimit, integralLimit);
            float i = Ki * _integral;

            // Derivative term with first-run protection
            float d = 0f;
            if (!_isFirstRun)
            {
                d = Kd * ((error - _lastError) / dt);
            }
            else
            {
                _isFirstRun = false;
            }

            _lastError = error;

            // Total output clamped within valid bounds
            float output = p + i + d;
            return Mathf.Clamp(output, outputMin, outputMax);
        }
    }
}

