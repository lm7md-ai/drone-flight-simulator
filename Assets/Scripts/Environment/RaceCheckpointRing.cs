using System;
using UnityEngine;

namespace DroneSimulator.Environment
{
    [RequireComponent(typeof(Collider))]
    public class RaceCheckpointRing : MonoBehaviour
    {
        [Header("Gate Settings")]
        public int gateIndex = 0;
        public bool isCurrentTarget = false;
        public bool isPassed = false;

        [Header("Visuals")]
        public Renderer ringRenderer;
        public Color normalColor = new Color(1.0f, 0.7f, 0.0f, 1.0f); // Amber
        public Color activeColor = new Color(0.1f, 0.8f, 1.0f, 1.0f); // Cyan
        public Color passedColor = new Color(0.1f, 1.0f, 0.3f, 1.0f); // Green

        public event Action<RaceCheckpointRing, Collider> OnGateTriggered;

        private Material _instancedMat;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;

            if (ringRenderer != null)
            {
                _instancedMat = ringRenderer.material;
            }
            UpdateVisualState();
        }

        public void SetTarget(bool isTarget)
        {
            isCurrentTarget = isTarget;
            UpdateVisualState();
        }

        public void SetPassed()
        {
            isPassed = true;
            isCurrentTarget = false;
            UpdateVisualState();
        }

        public void ResetGate()
        {
            isPassed = false;
            isCurrentTarget = false;
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (_instancedMat == null && ringRenderer != null) _instancedMat = ringRenderer.material;
            if (_instancedMat == null) return;

            Color targetColor = normalColor;
            if (isPassed) targetColor = passedColor;
            else if (isCurrentTarget) targetColor = activeColor;

            _instancedMat.color = targetColor;
            if (_instancedMat.HasProperty("_EmissionColor"))
            {
                _instancedMat.SetColor("_EmissionColor", targetColor * 2.0f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Drone") || other.attachedRigidbody != null)
            {
                OnGateTriggered?.Invoke(this, other);
            }
        }
    }
}

