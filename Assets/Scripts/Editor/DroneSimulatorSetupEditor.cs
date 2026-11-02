#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DroneSimulator.PhysicsEngine;
using DroneSimulator.Cameras;
using DroneSimulator.UI;
using DroneSimulator.Audio;
using DroneSimulator.Environment;
using DroneSimulator.UGS;

namespace DroneSimulator.Editor
{
    public class DroneSimulatorSetupEditor : EditorWindow
    {
        [MenuItem("Tools/3D Drone Simulator/Setup Open World & Drone", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<DroneSimulatorSetupEditor>("Drone Sim Setup");
            window.minSize = new Vector2(420, 360);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(12);
            GUILayout.Label("🚁 3D Drone Simulator AAA - Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Creates a complete AAA-grade 3D Drone Game with:\n" +
                "• Aerodynamic Carbon-Monocoque Drone (HD FPV camera, TPU skids, 4 brushless motors, LiPo 4S, lollipop antenna)\n" +
                "• 750m Multi-Biome Open World (Airport, City Skyscrapers, Mountain Lake, Suspension Bridge, Wind Farm)\n" +
                "• 12-Gate Neon Racing Course + 20 Collectible Energy Orbs\n" +
                "• BetaFlight OSD Telemetry + Procedural Audio + UGS Live Game backend.",
                MessageType.Info
            );

            GUILayout.Space(15);
            if (GUILayout.Button("🚀 1-Click Generate Complete Game Scene", GUILayout.Height(50)))
            {
                SetupCompleteScene();
            }

            GUILayout.Space(10);
            if (GUILayout.Button("🚁 Spawn High-Detail Drone Prefab Only", GUILayout.Height(32)))
            {
                SpawnDetailedDrone(Vector3.up * 1.5f);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("🌄 Generate AAA Open World Only", GUILayout.Height(32)))
            {
                GenerateEnvironment();
            }
        }

        public static void SetupCompleteScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var existingCam = Camera.main;
            if (existingCam != null) DestroyImmediate(existingCam.gameObject);

            // 1. Root Managers & Services
            GameObject managersRoot = new GameObject("--- MANAGERS & SERVICES ---");
            managersRoot.AddComponent<WindSystem>();
            managersRoot.AddComponent<UGSManager>();
            managersRoot.AddComponent<DroneLeaderboardService>();
            managersRoot.AddComponent<DroneCloudSaveService>();

            // 2. Open World Environment
            GameObject worldRoot = new GameObject("--- OPEN WORLD ENVIRONMENT ---");
            ProceduralOpenWorld worldGen = worldRoot.AddComponent<ProceduralOpenWorld>();
            worldGen.GenerateWorld(worldRoot.transform);

            // 3. High-Detail Drone
            SpawnDetailedDrone(new Vector3(0, 0.45f, 0));

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/OpenWorldDroneSimulator.unity");
            Debug.Log("<color=green><b>[DroneSimulator] Scene successfully generated and saved to Assets/Scenes/OpenWorldDroneSimulator.unity!</b></color>");
            EditorUtility.DisplayDialog("Setup Complete", "3D Open World Drone Simulator scene generated successfully with high-detail drone and rich environment!\n\nPress PLAY to fly.", "OK");
        }

        private static void GenerateEnvironment()
        {
            GameObject worldRoot = new GameObject("--- OPEN WORLD ENVIRONMENT ---");
            ProceduralOpenWorld worldGen = worldRoot.AddComponent<ProceduralOpenWorld>();
            worldGen.GenerateWorld(worldRoot.transform);
        }

        public static GameObject SpawnDetailedDrone(Vector3 spawnPos)
        {
            GameObject drone = new GameObject("Quadcopter_Drone_AAA");
            drone.tag = "Drone";
            drone.transform.position = spawnPos;

            // Physics & Rigidbody
            Rigidbody rb = drone.AddComponent<Rigidbody>();
            rb.mass = 0.68f;
            rb.drag = 0.05f;
            rb.angularDrag = 0.85f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            BoxCollider bodyCol = drone.AddComponent<BoxCollider>();
            bodyCol.size = new Vector3(0.35f, 0.08f, 0.42f);

            // 1. Main Carbon Monocoque Fuselage Plate
            GameObject carbonPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carbonPlate.name = "Carbon_Fuselage_Plate";
            carbonPlate.transform.SetParent(drone.transform);
            carbonPlate.transform.localPosition = Vector3.zero;
            carbonPlate.transform.localScale = new Vector3(0.24f, 0.04f, 0.52f);
            DestroyImmediate(carbonPlate.GetComponent<Collider>());

            // 2. Aerodynamic FPV Canopy
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            canopy.name = "Aerodynamic_Canopy";
            canopy.transform.SetParent(drone.transform);
            canopy.transform.localPosition = new Vector3(0, 0.05f, 0.05f);
            canopy.transform.localRotation = Quaternion.Euler(75f, 0, 0);
            canopy.transform.localScale = new Vector3(0.16f, 0.18f, 0.16f);
            DestroyImmediate(canopy.GetComponent<Collider>());

            // 3. HD FPV Camera with Lens
            GameObject camHousing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            camHousing.name = "HD_FPV_Camera";
            camHousing.transform.SetParent(drone.transform);
            camHousing.transform.localPosition = new Vector3(0, 0.04f, 0.28f);
            camHousing.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            camHousing.transform.localScale = new Vector3(0.06f, 0.08f, 0.06f);
            DestroyImmediate(camHousing.GetComponent<Collider>());

            // 4. LiPo 4S Battery Pack
            GameObject battery = GameObject.CreatePrimitive(PrimitiveType.Cube);
            battery.name = "LiPo_4S_1500mAh_Battery";
            battery.transform.SetParent(drone.transform);
            battery.transform.localPosition = new Vector3(0, 0.08f, -0.06f);
            battery.transform.localScale = new Vector3(0.18f, 0.08f, 0.30f);
            DestroyImmediate(battery.GetComponent<Collider>());

            // 5. Rear Lollipop 5.8GHz FPV Antenna
            GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            antenna.name = "Lollipop_5.8GHz_Antenna";
            antenna.transform.SetParent(drone.transform);
            antenna.transform.localPosition = new Vector3(0, 0.22f, -0.30f);
            antenna.transform.localScale = Vector3.one * 0.06f;
            DestroyImmediate(antenna.GetComponent<Collider>());

            // 6. 4 Carbon Arms, Brushless Motors, TPU Skids, and Propellers
            float armDist = 0.18f;
            MotorRotor mFL = CreateDetailedMotorArm("Motor_FL", drone.transform, new Vector3(-armDist, 0, armDist), RotationDirection.Clockwise);
            MotorRotor mFR = CreateDetailedMotorArm("Motor_FR", drone.transform, new Vector3(armDist, 0, armDist), RotationDirection.CounterClockwise);
            MotorRotor mRL = CreateDetailedMotorArm("Motor_RL", drone.transform, new Vector3(-armDist, 0, -armDist), RotationDirection.CounterClockwise);
            MotorRotor mRR = CreateDetailedMotorArm("Motor_RR", drone.transform, new Vector3(armDist, 0, -armDist), RotationDirection.Clockwise);

            // 7. Flight Controller & Scripts
            DronePhysicsController droneCtrl = drone.AddComponent<DronePhysicsController>();
            droneCtrl.motorFrontLeft = mFL;
            droneCtrl.motorFrontRight = mFR;
            droneCtrl.motorRearLeft = mRL;
            droneCtrl.motorRearRight = mRR;

            drone.AddComponent<DroneAudioEngine>();

            // 8. Main Camera with FPV / Chase controller
            GameObject camObj = new GameObject("Drone_MainCamera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 1600f;
            camObj.AddComponent<AudioListener>();

            DroneCameraController camCtrl = camObj.AddComponent<DroneCameraController>();
            camCtrl.droneTarget = drone.transform;

            // 9. BetaFlight OSD HUD
            GameObject hudObj = new GameObject("Drone_OSD_HUD");
            DroneOSDTelemetry osd = hudObj.AddComponent<DroneOSDTelemetry>();
            osd.drone = droneCtrl;
            osd.cameraController = camCtrl;

            Selection.activeGameObject = drone;
            return drone;
        }

        private static MotorRotor CreateDetailedMotorArm(string name, Transform parent, Vector3 localPos, RotationDirection dir)
        {
            GameObject arm = new GameObject(name);
            arm.transform.SetParent(parent);
            arm.transform.localPosition = localPos;

            // Carbon Tube
            GameObject tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tube.name = "Arm_Tube";
            tube.transform.SetParent(arm.transform);
            tube.transform.position = (parent.position + arm.transform.position) * 0.5f;
            tube.transform.up = arm.transform.position - parent.position;
            tube.transform.localScale = new Vector3(0.024f, localPos.magnitude * 0.5f, 0.024f);
            DestroyImmediate(tube.GetComponent<Collider>());

            // Brushless Motor Bell
            GameObject bell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bell.name = "Brushless_Motor_Bell";
            bell.transform.SetParent(arm.transform);
            bell.transform.localPosition = new Vector3(0, 0.02f, 0);
            bell.transform.localScale = new Vector3(0.06f, 0.024f, 0.06f);
            DestroyImmediate(bell.GetComponent<Collider>());

            // TPU Landing Skid Foot
            GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            foot.name = "TPU_Landing_Skid";
            foot.transform.SetParent(arm.transform);
            foot.transform.localPosition = new Vector3(0, -0.035f, 0);
            foot.transform.localScale = new Vector3(0.02f, 0.04f, 0.02f);
            DestroyImmediate(foot.GetComponent<Collider>());

            // 3-Blade Propeller
            GameObject prop = new GameObject("Propeller_TriBlade");
            prop.transform.SetParent(arm.transform);
            prop.transform.localPosition = new Vector3(0, 0.045f, 0);

            for (int b = 0; b < 3; b++)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = $"Blade_{b}";
                blade.transform.SetParent(prop.transform);
                blade.transform.localRotation = Quaternion.Euler(0, b * 120f, 0);
                blade.transform.localPosition = blade.transform.localRotation * new Vector3(0, 0, 0.07f);
                blade.transform.localScale = new Vector3(0.024f, 0.004f, 0.16f);
                DestroyImmediate(blade.GetComponent<Collider>());
            }

            MotorRotor motor = arm.AddComponent<MotorRotor>();
            motor.motorID = name;
            motor.spinDirection = dir;
            motor.propellerTransform = prop.transform;

            return motor;
        }
    }
}
#endif
