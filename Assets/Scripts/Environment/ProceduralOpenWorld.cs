using System.Collections.Generic;
using UnityEngine;

namespace DroneSimulator.Environment
{
    public class ProceduralOpenWorld : MonoBehaviour
    {
        [Header("World Dimensions")]
        public int worldSize = 750;
        public float maxHeight = 65f;
        public float terrainNoiseScale = 0.007f;

        [Header("Biomes & Districts")]
        public bool spawnAirport = true;
        public bool spawnCityDistrict = true;
        public bool spawnSuspensionBridge = true;
        public bool spawnLake = true;
        public bool spawnWindFarm = true;
        public bool spawnCollectibles = true;

        [Header("Race Track")]
        public int numberOfRaceGates = 12;

        public void GenerateWorld(Transform worldRoot)
        {
            Debug.Log("<color=cyan>[OpenWorld] Building AAA Interactive Open World Environment...</color>");

            // 1. Terrain Mesh with multi-layer altitude colors
            CreateTerrain(worldRoot);

            // 2. Lake Water Plane
            if (spawnLake) CreateLakeWater(worldRoot);

            // 3. Airport Runway & Tower
            if (spawnAirport) CreateAirport(worldRoot);

            // 4. City District Skyscraper Cluster
            if (spawnCityDistrict) CreateCityDistrict(worldRoot);

            // 5. Suspension Bridge & Highway
            if (spawnSuspensionBridge) CreateSuspensionBridge(worldRoot);

            // 6. Wind Turbines Farm
            if (spawnWindFarm) CreateWindTurbines(worldRoot);

            // 7. Interactive Neon Race Gates
            CreateRaceCourse(worldRoot);

            // 8. Collectible Energy Orbs & Boost Rings
            if (spawnCollectibles) CreateInteractiveCollectibles(worldRoot);

            // 9. Atmosphere, Fog & Directional Lighting
            SetupAtmosphere(worldRoot);

            Debug.Log("<color=green>[OpenWorld] AAA Open World Generation Complete!</color>");
        }

        public float GetTerrainHeight(float x, float z)
        {
            float distCenter = Mathf.Sqrt(x * x + z * z);
            if (distCenter < 50f) return 0f; // Airport flat apron

            // Lake depression in East
            float lakeDist = Mathf.Sqrt((x - 180f) * (x - 180f) + z * z);
            if (lakeDist < 75f)
            {
                return -Mathf.Cos((lakeDist / 75f) * (Mathf.PI * 0.5f)) * 12f;
            }

            // Mountain ridges & valleys
            float n1 = Mathf.Sin(x * 0.007f + 1.2f) * Mathf.Cos(z * 0.007f + 2.4f);
            float n2 = Mathf.Sin(x * 0.018f - z * 0.015f) * 0.4f;
            float n3 = Mathf.Cos(x * 0.035f + z * 0.03f) * 0.15f;
            float h = (n1 + n2 + n3 + 1.2f) * 26f;

            // Canyon
            float canyonCurve = Mathf.Sin(z * 0.01f) * 60f;
            float distFromCanyon = Mathf.Abs(x - canyonCurve);
            if (distFromCanyon < 45f && z > 60f)
            {
                float canyonFactor = Mathf.Cos((distFromCanyon / 45f) * (Mathf.PI * 0.5f));
                h -= canyonFactor * 28f;
            }

            if (distCenter < 75f)
            {
                float blend = (distCenter - 50f) / 25f;
                h *= blend;
            }

            return Mathf.Max(-12f, h);
        }

        private void CreateTerrain(Transform root)
        {
            GameObject terrainObj = new GameObject("Terrain_MultiBiome");
            terrainObj.transform.SetParent(root);
            terrainObj.layer = LayerMask.NameToLayer("Terrain") != -1 ? LayerMask.NameToLayer("Terrain") : 0;

            MeshFilter mf = terrainObj.AddComponent<MeshFilter>();
            MeshRenderer mr = terrainObj.AddComponent<MeshRenderer>();
            MeshCollider mc = terrainObj.AddComponent<MeshCollider>();

            int res = 120;
            Vector3[] vertices = new Vector3[res * res];
            Vector2[] uvs = new Vector2[res * res];
            Color[] colors = new Color[res * res];
            float step = (float)worldSize / (res - 1);
            float halfSize = worldSize * 0.5f;

            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    int idx = z * res + x;
                    float wx = x * step - halfSize;
                    float wz = z * step - halfSize;
                    float wy = GetTerrainHeight(wx, wz);

                    vertices[idx] = new Vector3(wx, wy, wz);
                    uvs[idx] = new Vector2((float)x / res * 15f, (float)z / res * 15f);

                    if (wy < 0.5f) colors[idx] = new Color(0.76f, 0.70f, 0.50f); // Sand
                    else if (wy < 28f) colors[idx] = Color.Lerp(new Color(0.22f, 0.48f, 0.18f), new Color(0.35f, 0.42f, 0.25f), wy / 28f); // Grass
                    else colors[idx] = Color.Lerp(new Color(0.45f, 0.46f, 0.48f), new Color(0.85f, 0.88f, 0.92f), (wy - 28f) / 35f); // Mountain & Snow
                }
            }

            int[] triangles = new int[(res - 1) * (res - 1) * 6];
            int tIdx = 0;
            for (int z = 0; z < res - 1; z++)
            {
                for (int x = 0; x < res - 1; x++)
                {
                    int bl = z * res + x;
                    int br = bl + 1;
                    int tl = (z + 1) * res + x;
                    int tr = tl + 1;

                    triangles[tIdx++] = bl; triangles[tIdx++] = tl; triangles[tIdx++] = br;
                    triangles[tIdx++] = br; triangles[tIdx++] = tl; triangles[tIdx++] = tr;
                }
            }

            Mesh mesh = new Mesh { name = "ProceduralBiomeTerrain", vertices = vertices, uv = uvs, colors = colors, triangles = triangles };
            mesh.RecalculateNormals();
            mf.sharedMesh = mesh;
            mc.sharedMesh = mesh;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mr.sharedMaterial = mat;
        }

        private void CreateLakeWater(Transform root)
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "Mountain_Lake_Water";
            water.transform.SetParent(root);
            water.transform.position = new Vector3(180f, -0.2f, 0f);
            water.transform.localScale = new Vector3(150f, 0.1f, 150f);

            Renderer r = water.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(0.0f, 0.55f, 0.85f, 0.8f);
            r.sharedMaterial = mat;
        }

        private void CreateAirport(Transform root)
        {
            GameObject airport = new GameObject("Airport_Runway_District");
            airport.transform.SetParent(root);

            // Main Runway
            GameObject runway = GameObject.CreatePrimitive(PrimitiveType.Cube);
            runway.name = "Runway_Strip";
            runway.transform.SetParent(airport.transform);
            runway.transform.position = new Vector3(0, 0.08f, 0);
            runway.transform.localScale = new Vector3(26f, 0.16f, 95f);

            // Control Tower
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "ATC_ControlTower";
            tower.transform.SetParent(airport.transform);
            tower.transform.position = new Vector3(-28f, 18f, -25f);
            tower.transform.localScale = new Vector3(5f, 18f, 5f);

            // Hangar
            GameObject hangar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hangar.name = "Aircraft_Hangar";
            hangar.transform.SetParent(airport.transform);
            hangar.transform.position = new Vector3(-28f, 7f, 15f);
            hangar.transform.localScale = new Vector3(20f, 14f, 32f);
        }

        private void CreateCityDistrict(Transform root)
        {
            GameObject city = new GameObject("City_Skyscrapers_District");
            city.transform.SetParent(root);

            float cx = -150f;
            float cz = -140f;

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    float bx = cx + c * 35f;
                    float bz = cz + r * 35f;
                    float by = GetTerrainHeight(bx, bz);
                    float h = 45f + Random.Range(0f, 65f);

                    GameObject bldg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bldg.name = $"Skyscraper_{r}_{c}";
                    bldg.transform.SetParent(city.transform);
                    bldg.transform.position = new Vector3(bx, by + h * 0.5f, bz);
                    bldg.transform.localScale = new Vector3(16f, h, 16f);
                }
            }
        }

        private void CreateSuspensionBridge(Transform root)
        {
            GameObject bridge = new GameObject("Canyon_SuspensionBridge");
            bridge.transform.SetParent(root);

            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "BridgeDeck";
            deck.transform.SetParent(bridge.transform);
            deck.transform.position = new Vector3(0, 16f, 125f);
            deck.transform.localScale = new Vector3(14f, 1.5f, 95f);

            GameObject arch1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arch1.name = "BridgeTower_North";
            arch1.transform.SetParent(bridge.transform);
            arch1.transform.position = new Vector3(0, 24f, 90f);
            arch1.transform.localScale = new Vector3(16f, 28f, 2f);

            GameObject arch2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arch2.name = "BridgeTower_South";
            arch2.transform.SetParent(bridge.transform);
            arch2.transform.position = new Vector3(0, 24f, 160f);
            arch2.transform.localScale = new Vector3(16f, 28f, 2f);
        }

        private void CreateWindTurbines(Transform root)
        {
            GameObject turbines = new GameObject("WindTurbinesFarm");
            turbines.transform.SetParent(root);

            for (int i = 0; i < 6; i++)
            {
                float angle = ((float)i / 6f) * Mathf.PI * 2f;
                float tx = Mathf.Sin(angle) * 220f;
                float tz = Mathf.Cos(angle) * 220f;
                float ty = GetTerrainHeight(tx, tz);

                GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tower.name = $"TurbineTower_{i}";
                tower.transform.SetParent(turbines.transform);
                tower.transform.position = new Vector3(tx, ty + 22.5f, tz);
                tower.transform.localScale = new Vector3(2.5f, 22.5f, 2.5f);

                GameObject hub = new GameObject("Hub");
                hub.transform.SetParent(tower.transform);
                hub.transform.localPosition = new Vector3(0, 1.0f, 1.0f);
                hub.AddComponent<TurbineRotator>().rotationSpeed = 30f + i * 2f;

                for (int b = 0; b < 3; b++)
                {
                    GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    blade.transform.SetParent(hub.transform);
                    blade.transform.localRotation = Quaternion.Euler(0, 0, b * 120f);
                    blade.transform.localPosition = blade.transform.localRotation * new Vector3(0, 7f, 0);
                    blade.transform.localScale = new Vector3(0.5f, 14f, 0.1f);
                    DestroyImmediate(blade.GetComponent<Collider>());
                }
            }
        }

        private void CreateRaceCourse(Transform root)
        {
            GameObject course = new GameObject("RaceCourse_Circuit");
            course.transform.SetParent(root);

            OpenWorldRaceManager raceMgr = course.AddComponent<OpenWorldRaceManager>();
            raceMgr.gateAudioSource = course.AddComponent<AudioSource>();

            Vector3[] path = {
                new Vector3(0, 12, 40),
                new Vector3(30, 16, 90),
                new Vector3(0, 22, 130),   // Bridge crossing
                new Vector3(-50, 18, 120),
                new Vector3(-110, 28, 60),
                new Vector3(-140, 35, -40),
                new Vector3(-150, 55, -130), // Skyscraper rooftop weave
                new Vector3(-80, 40, -150),
                new Vector3(20, 30, -120),
                new Vector3(140, 16, -60),
                new Vector3(180, 12, 0),    // Lake low-pass
                new Vector3(90, 14, 25)
            };

            List<RaceCheckpointRing> gates = new List<RaceCheckpointRing>();
            for (int i = 0; i < path.Length; i++)
            {
                Vector3 p = path[i];
                Vector3 next = path[(i + 1) % path.Length];
                Vector3 look = (next - p).normalized;

                GameObject gateObj = new GameObject($"Gate_{i:00}");
                gateObj.transform.SetParent(course.transform);
                gateObj.transform.position = p;
                gateObj.transform.rotation = Quaternion.LookRotation(look);

                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.transform.SetParent(gateObj.transform);
                visual.transform.localScale = new Vector3(10f, 10f, 0.4f);

                BoxCollider col = gateObj.AddComponent<BoxCollider>();
                col.size = new Vector3(8f, 8f, 2f);
                col.isTrigger = true;

                RaceCheckpointRing ring = gateObj.AddComponent<RaceCheckpointRing>();
                ring.ringRenderer = visual.GetComponent<Renderer>();
                ring.gateIndex = i;
                gates.Add(ring);
            }
            raceMgr.raceGates = gates;
        }

        private void CreateInteractiveCollectibles(Transform root)
        {
            GameObject collectibles = new GameObject("Interactive_Collectibles");
            collectibles.transform.SetParent(root);

            for (int i = 0; i < 20; i++)
            {
                float ox = Random.Range(-200f, 200f);
                float oz = Random.Range(-200f, 200f);
                float oy = GetTerrainHeight(ox, oz) + Random.Range(4f, 15f);

                GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = $"EnergyOrb_{i}";
                orb.transform.SetParent(collectibles.transform);
                orb.transform.position = new Vector3(ox, oy, oz);
                orb.transform.localScale = Vector3.one * 1.8f;
                orb.GetComponent<Collider>().isTrigger = true;

                Renderer rend = orb.GetComponent<Renderer>();
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                m.color = Color.cyan;
                rend.sharedMaterial = m;
            }
        }

        private void SetupAtmosphere(Transform root)
        {
            GameObject sun = new GameObject("Directional_SunLight");
            sun.transform.SetParent(root);
            Light l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1.0f, 0.98f, 0.90f);
            l.intensity = 1.35f;
            l.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(46f, -32f, 0f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0018f;
            RenderSettings.fogColor = new Color(0.72f, 0.84f, 0.94f);
        }
    }
}
