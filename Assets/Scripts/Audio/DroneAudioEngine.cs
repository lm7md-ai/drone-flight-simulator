using UnityEngine;
using DroneSimulator.PhysicsEngine;

namespace DroneSimulator.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class DroneAudioEngine : MonoBehaviour
    {
        [Header("Target Drone")]
        public DronePhysicsController drone;

        [Header("Audio Tuning")]
        [Tooltip("Base motor pitch frequency")]
        public float basePitch = 0.5f;
        public float maxPitch = 2.4f;
        public float pitchResponsiveness = 10f;

        [Header("Wind Audio Source")]
        public AudioSource windAudioSource;
        public float maxWindVolume = 0.6f;

        private AudioSource _motorAudioSource;
        private float _currentPitch;

        private void Awake()
        {
            _motorAudioSource = GetComponent<AudioSource>();
            _motorAudioSource.loop = true;
            _motorAudioSource.playOnAwake = false;
            _motorAudioSource.spatialBlend = 0.8f; // 3D spatial sound

            // Generate procedural motor buzz if no clip assigned
            if (_motorAudioSource.clip == null)
            {
                _motorAudioSource.clip = CreateProceduralMotorClip();
            }
        }

        private void Start()
        {
            if (drone == null) drone = GetComponentInParent<DronePhysicsController>();
            _motorAudioSource.Play();
            _currentPitch = basePitch;
        }

        private void Update()
        {
            if (drone == null) return;

            // Average motor throttle
            float throttle = drone.rawThrottleInput;
            float targetPitch = Mathf.Lerp(basePitch, maxPitch, throttle);

            _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, Time.deltaTime * pitchResponsiveness);
            _motorAudioSource.pitch = _currentPitch;
            _motorAudioSource.volume = Mathf.Lerp(0.15f, 0.9f, throttle);

            // Update wind rushing audio with speed
            if (windAudioSource != null)
            {
                float speedRatio = Mathf.Clamp01(drone.currentSpeedKmh / 120f);
                windAudioSource.volume = speedRatio * maxWindVolume;
                windAudioSource.pitch = 0.8f + speedRatio * 0.6f;
            }
        }

        private AudioClip CreateProceduralMotorClip()
        {
            int sampleRate = 44100;
            float duration = 1.0f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            // Harmonic synthesis of 4 brushless motors (harmonics at fundamental, 2x, 4x, plus high-pitch PWM whistle)
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float f0 = 110f; // 110Hz idle rumble
                float wave = 0.4f * Mathf.Sin(2f * Mathf.PI * f0 * t)
                           + 0.25f * Mathf.Sin(2f * Mathf.PI * f0 * 2f * t)
                           + 0.15f * Mathf.Sin(2f * Mathf.PI * f0 * 4f * t)
                           + 0.1f * Mathf.Sin(2f * Mathf.PI * 1800f * t) // ESC PWM whistle
                           + 0.05f * (Random.value * 2f - 1f); // Air turbulence noise

                samples[i] = wave * 0.4f;
            }

            AudioClip clip = AudioClip.Create("ProceduralMotorLoop", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

