using UnityEngine;
using DroneSimulator.PhysicsEngine;
using DroneSimulator.Cameras;
using DroneSimulator.Environment;

namespace DroneSimulator.UI
{
    public class DroneOSDTelemetry : MonoBehaviour
    {
        [Header("Target Drone")]
        public DronePhysicsController drone;
        public DroneCameraController cameraController;
        public OpenWorldRaceManager raceManager;

        [Header("OSD Appearance")]
        public Color osdColor = new Color(0.2f, 1.0f, 0.4f, 0.95f); // Classic phosphor green
        public Color warningColor = new Color(1.0f, 0.2f, 0.2f, 0.95f);
        public bool showArtificialHorizon = true;
        public bool showCompass = true;
        public bool showTelemetryBars = true;

        private GUIStyle _hudTextStyle;
        private GUIStyle _hudSmallStyle;
        private GUIStyle _centerStyle;
        private Texture2D _whiteTexture;

        private void Start()
        {
            if (drone == null) drone = FindObjectOfType<DronePhysicsController>();
            if (cameraController == null) cameraController = FindObjectOfType<DroneCameraController>();
            if (raceManager == null) raceManager = FindObjectOfType<OpenWorldRaceManager>();

            // Create 1x1 white texture for drawing OSD lines
            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();
        }

        private void InitStyles()
        {
            if (_hudTextStyle == null)
            {
                _hudTextStyle = new GUIStyle();
                _hudTextStyle.fontStyle = FontStyle.Bold;
                _hudTextStyle.fontSize = 14;
                _hudTextStyle.normal.textColor = osdColor;

                _hudSmallStyle = new GUIStyle(_hudTextStyle);
                _hudSmallStyle.fontSize = 11;

                _centerStyle = new GUIStyle(_hudTextStyle);
                _centerStyle.alignment = TextAnchor.MiddleCenter;
            }
        }

        private void OnGUI()
        {
            if (drone == null) return;
            InitStyles();

            float screenW = Screen.width;
            float screenH = Screen.height;
            Vector2 screenCenter = new Vector2(screenW * 0.5f, screenH * 0.5f);

            // 1. Artificial Horizon & Center Reticle
            if (showArtificialHorizon)
            {
                DrawArtificialHorizon(screenCenter);
            }

            // 2. Top Compass Ribbon
            if (showCompass)
            {
                DrawCompass(screenCenter.x, 30f);
            }

            // 3. Left Speed & Throttle Bar
            DrawLeftTelemetry(40f, screenCenter.y);

            // 4. Right Altitude & Vario Bar
            DrawRightTelemetry(screenW - 140f, screenCenter.y);

            // 5. Bottom Status: Battery, Flight Mode, Camera Tilt, Lap Timer
            DrawBottomStatus(screenW, screenH);
        }

        private void DrawArtificialHorizon(Vector2 center)
        {
            float pitch = drone.pitchDegrees;
            float roll = drone.rollDegrees;

            // Center crosshair / flight path marker
            GUI.color = osdColor;
            DrawLine(new Vector2(center.x - 20, center.y), new Vector2(center.x - 6, center.y), 2f);
            DrawLine(new Vector2(center.x + 6, center.y), new Vector2(center.x + 20, center.y), 2f);
            DrawLine(new Vector2(center.x, center.y - 12), new Vector2(center.x, center.y - 4), 2f);
            DrawLine(new Vector2(center.x, center.y + 4), new Vector2(center.x, center.y + 12), 2f);

            // Horizon ladder lines that rotate with roll and offset with pitch
            Matrix4x4 origMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(-roll, center);

            float pixelsPerDegree = 3.5f;
            for (int p = -40; p <= 40; p += 10)
            {
                if (p == 0)
                {
                    // Level horizon
                    float yPos = center.y + (pitch - p) * pixelsPerDegree;
                    DrawLine(new Vector2(center.x - 70, yPos), new Vector2(center.x - 25, yPos), 2f);
                    DrawLine(new Vector2(center.x + 25, yPos), new Vector2(center.x + 70, yPos), 2f);
                }
                else
                {
                    float yPos = center.y + (pitch - p) * pixelsPerDegree;
                    if (yPos > center.y - 150 && yPos < center.y + 150)
                    {
                        float lineW = (p > 0) ? 35f : 25f;
                        DrawLine(new Vector2(center.x - lineW, yPos), new Vector2(center.x - 12, yPos), 1.5f);
                        DrawLine(new Vector2(center.x + 12, yPos), new Vector2(center.x + lineW, yPos), 1.5f);
                        GUI.Label(new Rect(center.x + lineW + 4, yPos - 8, 30, 20), Mathf.Abs(p).ToString(), _hudSmallStyle);
                    }
                }
            }

            GUI.matrix = origMatrix;
        }

        private void DrawCompass(float centerX, float topY)
        {
            float yaw = drone.yawDegrees;
            float ribbonW = 320f;
            float startX = centerX - ribbonW * 0.5f;

            // Background line
            GUI.color = new Color(osdColor.r, osdColor.g, osdColor.b, 0.4f);
            DrawLine(new Vector2(startX, topY + 15), new Vector2(startX + ribbonW, topY + 15), 1.5f);

            GUI.color = osdColor;
            // Center heading pip
            DrawLine(new Vector2(centerX, topY + 18), new Vector2(centerX - 5, topY + 26), 2f);
            DrawLine(new Vector2(centerX, topY + 18), new Vector2(centerX + 5, topY + 26), 2f);

            string[] cardinals = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            float[] angles = { 0, 45, 90, 135, 180, 225, 270, 315 };

            for (int i = 0; i < angles.Length; i++)
            {
                float diff = Mathf.DeltaAngle(yaw, angles[i]);
                if (Mathf.Abs(diff) < 55f)
                {
                    float x = centerX + (diff / 55f) * (ribbonW * 0.5f);
                    GUI.Label(new Rect(x - 12, topY - 5, 24, 20), cardinals[i], _centerStyle);
                    DrawLine(new Vector2(x, topY + 12), new Vector2(x, topY + 18), 1.5f);
                }
            }

            // Heading degree text
            GUI.Label(new Rect(centerX - 25, topY + 28, 50, 20), $"{(int)yaw:000}°", _centerStyle);
        }

        private void DrawLeftTelemetry(float leftX, float centerY)
        {
            GUI.color = osdColor;
            // Speed indicator
            GUI.Label(new Rect(leftX, centerY - 60, 110, 25), $"SPD: {drone.currentSpeedKmh:F1} km/h", _hudTextStyle);
            
            // Throttle indicator
            int throttlePct = Mathf.RoundToInt(drone.rawThrottleInput * 100f);
            GUI.Label(new Rect(leftX, centerY - 35, 110, 25), $"THR: {throttlePct}%", _hudTextStyle);
            DrawLine(new Vector2(leftX, centerY - 10), new Vector2(leftX + (throttlePct * 0.8f), centerY - 10), 4f);

            // View Mode
            string viewName = cameraController != null ? cameraController.currentMode.ToString() : "FPV";
            GUI.Label(new Rect(leftX, centerY + 10, 110, 25), $"CAM: {viewName}", _hudSmallStyle);
            if (cameraController != null && cameraController.currentMode == CameraViewMode.FPVCockpit)
            {
                GUI.Label(new Rect(leftX, centerY + 30, 110, 25), $"TILT: {(int)cameraController.fpvUptiltAngle}°", _hudSmallStyle);
            }
        }

        private void DrawRightTelemetry(float rightX, float centerY)
        {
            GUI.color = osdColor;
            // Altitude AGL (Radar)
            GUI.Label(new Rect(rightX, centerY - 60, 120, 25), $"AGL: {drone.altitudeAGL:F1} m", _hudTextStyle);

            // Altitude MSL (Barometric)
            GUI.Label(new Rect(rightX, centerY - 35, 120, 25), $"MSL: {drone.altitudeMSL:F1} m", _hudSmallStyle);

            // Wind Info
            if (WindSystem.Instance != null)
            {
                Vector3 w = WindSystem.Instance.GetWindAtPosition(drone.transform.position);
                GUI.Label(new Rect(rightX, centerY + 10, 120, 25), $"WIND: {w.magnitude:F1} m/s", _hudSmallStyle);
            }
        }

        private void DrawBottomStatus(float screenW, float screenH)
        {
            float bottomY = screenH - 50f;

            // Battery telemetry with low voltage warning
            bool isLowVolt = drone.currentVoltage < (drone.batteryCellCount * 3.5f);
            Color voltColor = (isLowVolt && (Time.time % 0.5f > 0.25f)) ? warningColor : osdColor;
            _hudTextStyle.normal.textColor = voltColor;

            string batText = $"BAT: {drone.currentVoltage:F2}V  |  {drone.currentAmperage:F1}A  |  {(int)drone.batteryConsumedMAh} mAh";
            GUI.Label(new Rect(40f, bottomY, 320, 25), batText, _hudTextStyle);

            // Flight Mode
            _hudTextStyle.normal.textColor = osdColor;
            string modeText = $"MODE: {drone.currentMode.ToString().ToUpper()} [M]";
            GUI.Label(new Rect(screenW * 0.5f - 80, bottomY, 160, 25), modeText, _centerStyle);

            // Race / Checkpoint Status
            if (raceManager != null && raceManager.isRaceActive)
            {
                string raceText = $"GATE: {raceManager.currentGateIndex}/{raceManager.totalGates}  |  TIME: {raceManager.currentRaceTime:F2}s";
                GUI.Label(new Rect(screenW - 280f, bottomY, 260, 25), raceText, _hudTextStyle);
            }
            else
            {
                GUI.Label(new Rect(screenW - 240f, bottomY, 220, 25), "FREE FLIGHT / EXPLORE", _hudSmallStyle);
            }

            // Quick Controls Overlay at top right
            GUI.Label(new Rect(screenW - 220f, 15f, 210, 60), 
                "CONTROLS:\n[W/S]: Throttle   [A/D]: Yaw\n[I/K]: Pitch      [J/L]: Roll\n[C]: Camera      [M]: Mode", 
                _hudSmallStyle);
        }

        private void DrawLine(Vector2 pointA, Vector2 pointB, float width)
        {
            Vector2 delta = pointB - pointA;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            float length = delta.magnitude;

            Matrix4x4 orig = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, pointA);
            GUI.DrawTexture(new Rect(pointA.x, pointA.y - width * 0.5f, length, width), _whiteTexture);
            GUI.matrix = orig;
        }
    }
}

