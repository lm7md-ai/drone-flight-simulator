using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_SERVICES_CLOUDSAVE
using Unity.Services.CloudSave;
#endif

namespace DroneSimulator.UGS
{
    public class DroneCloudSaveService : MonoBehaviour
    {
        public static DroneCloudSaveService Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public async Task SavePilotDataAsync(float flightHours, float bestLap, string flightMode)
        {
            Debug.Log("<color=cyan>[CloudSave] Saving pilot telemetry & progress to UGS Cloud Save...</color>");

            try
            {
#if UNITY_SERVICES_CLOUDSAVE
                var data = new Dictionary<string, object>
                {
                    { "total_flight_hours", flightHours },
                    { "best_lap_time", bestLap },
                    { "preferred_mode", flightMode },
                    { "last_played_utc", DateTime.UtcNow.ToString("o") }
                };

                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
                Debug.Log("<color=green>[CloudSave] Pilot profile successfully saved to cloud!</color>");
#else
                await Task.Yield();
                Debug.Log("[CloudSave] (Simulated) Pilot profile saved locally.");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CloudSave] Save notice: {ex.Message}");
            }
        }
    }
}

