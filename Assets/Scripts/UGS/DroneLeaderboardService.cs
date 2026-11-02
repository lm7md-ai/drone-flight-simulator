using System;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_SERVICES_LEADERBOARDS
using Unity.Services.Leaderboards;
#endif

namespace DroneSimulator.UGS
{
    public class DroneLeaderboardService : MonoBehaviour
    {
        public static DroneLeaderboardService Instance { get; private set; }

        public const string RACE_LEADERBOARD_ID = "open_world_drone_race";

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public async Task SubmitRaceScoreAsync(float lapTimeSeconds)
        {
            Debug.Log($"<color=cyan>[Leaderboard] Submitting lap time {lapTimeSeconds:F2}s to leaderboard '{RACE_LEADERBOARD_ID}'...</color>");

            try
            {
#if UNITY_SERVICES_LEADERBOARDS
                var response = await LeaderboardsService.Instance.AddPlayerScoreAsync(RACE_LEADERBOARD_ID, lapTimeSeconds);
                Debug.Log($"<color=green>[Leaderboard] Score submitted! Rank: {response.Rank + 1}</color>");
#else
                await Task.Yield();
                Debug.Log($"[Leaderboard] (Simulated) Score of {lapTimeSeconds:F2}s recorded successfully.");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Leaderboard] Submission notice: {ex.Message}");
            }
        }
    }
}

