using UnityEngine;

namespace DroneSimulator.Cameras
{
    public enum CameraViewMode
    {
        FPVCockpit = 0,
        ThirdPersonChase = 1
    }

    public class DroneCameraController : MonoBehaviour
    {
        [Header("Target & Mode")]
        public Transform droneTarget;
        public CameraViewMode currentMode = CameraViewMode.FPVCockpit;

        [Header("FPV Cockpit Settings")]
        [Tooltip("FPV Camera Up-tilt angle in degrees (FPV pilots use 25-45 deg)")]
        [Range(0f, 60f)]
        public float fpvUptiltAngle = 30f;
        public Vector3 fpvLocalOffset = new Vector3(0f, 0.08f, 0.15f);
        public float vibrationIntensity = 0.002f;

        [Header("Third-Person Chase Settings")]
        public float chaseDistance = 3.5f;
        public float chaseHeight = 1.2f;
        public float positionDamping = 8.0f;
        public float rotationDamping = 6.0f;

        [Header("Field of View")]
        public float fpvFov = 110f;
        public float chaseFov = 75f;

        private Camera _cam;
        private Rigidbody _droneRb;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
        }

        private void Start()
        {
            if (droneTarget != null)
            {
                _droneRb = droneTarget.GetComponent<Rigidbody>();
            }
        }

        private void Update()
        {
            // Toggle view mode with key 'C'
            if (Input.GetKeyDown(KeyCode.C))
            {
                currentMode = (currentMode == CameraViewMode.FPVCockpit) 
                    ? CameraViewMode.ThirdPersonChase 
                    : CameraViewMode.FPVCockpit;
            }

            // Adjust FPV uptilt on the fly (PageUp / PageDown or Bracket keys)
            if (Input.GetKey(KeyCode.PageUp) || Input.GetKey(KeyCode.RightBracket))
            {
                fpvUptiltAngle = Mathf.Min(60f, fpvUptiltAngle + 20f * Time.deltaTime);
            }
            if (Input.GetKey(KeyCode.PageDown) || Input.GetKey(KeyCode.LeftBracket))
            {
                fpvUptiltAngle = Mathf.Max(0f, fpvUptiltAngle - 20f * Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            if (droneTarget == null) return;

            if (currentMode == CameraViewMode.FPVCockpit)
            {
                UpdateFPVView();
            }
            else
            {
                UpdateChaseView();
            }
        }

        private void UpdateFPVView()
        {
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, fpvFov, Time.deltaTime * 5f);

            // Position rigidly with drone
            Vector3 targetPos = droneTarget.TransformPoint(fpvLocalOffset);

            // Add subtle high-speed vibration
            if (_droneRb != null && _droneRb.velocity.sqrMagnitude > 1f)
            {
                float speedFactor = Mathf.Clamp01(_droneRb.velocity.magnitude / 30f);
                Vector3 jitter = Random.insideUnitSphere * (vibrationIntensity * speedFactor);
                targetPos += jitter;
            }

            transform.position = targetPos;

            // Rotation follows drone frame + uptilt pitch
            Quaternion droneRot = droneTarget.rotation;
            Quaternion tiltRot = Quaternion.Euler(-fpvUptiltAngle, 0f, 0f);
            transform.rotation = droneRot * tiltRot;
        }

        private void UpdateChaseView()
        {
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, chaseFov, Time.deltaTime * 5f);

            // Target position behind and above the drone
            Vector3 forwardVec = droneTarget.forward;
            forwardVec.y = 0f;
            if (forwardVec.sqrMagnitude < 0.001f) forwardVec = Vector3.forward;
            forwardVec.Normalize();

            Vector3 wantedPos = droneTarget.position - forwardVec * chaseDistance + Vector3.up * chaseHeight;

            transform.position = Vector3.Lerp(transform.position, wantedPos, Time.deltaTime * positionDamping);

            // Look towards the drone
            Quaternion wantedRot = Quaternion.LookRotation(droneTarget.position + Vector3.up * 0.3f - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, wantedRot, Time.deltaTime * rotationDamping);
        }
    }
}

