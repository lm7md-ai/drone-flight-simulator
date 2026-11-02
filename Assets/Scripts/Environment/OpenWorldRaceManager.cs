using System.Collections.Generic;
using UnityEngine;
using DroneSimulator.UGS;

namespace DroneSimulator.Environment
{
    public class OpenWorldRaceManager : MonoBehaviour
    {
        public static OpenWorldRaceManager Instance { get; private set; }

        [Header("Race Track")]
        public List<RaceCheckpointRing> raceGates = new List<RaceCheckpointRing>();
        public int currentGateIndex = 0;
        public int totalGates => raceGates.Count;

        [Header("Timing")]
        public bool isRaceActive = false;
        public float currentRaceTime = 0f;
        public float bestLapTime = 9999f;

        [Header("Feedback Sounds")]
        public AudioSource gateAudioSource;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // Auto find gates in children if list is empty
            if (raceGates.Count == 0)
            {
                raceGates.AddRange(GetComponentsInChildren<RaceCheckpointRing>());
            }

            for (int i = 0; i < raceGates.Count; i++)
            {
                raceGates[i].gateIndex = i;
                raceGates[i].OnGateTriggered += HandleGateTriggered;
                raceGates[i].ResetGate();
            }

            if (raceGates.Count > 0)
            {
                raceGates[0].SetTarget(true);
            }
        }

        private void Update()
        {
            if (isRaceActive)
            {
                currentRaceTime += Time.deltaTime;
            }
        }

        private void HandleGateTriggered(RaceCheckpointRing gate, Collider droneCol)
        {
            if (gate.gateIndex == currentGateIndex)
            {
                PlayGateSound();
                gate.SetPassed();

                if (currentGateIndex == 0 && !isRaceActive)
                {
                    // Race Started!
                    isRaceActive = true;
                    currentRaceTime = 0f;
                    Debug.Log("<color=cyan>[Race] Lap Started!</color>");
                }

                currentGateIndex++;

                if (currentGateIndex >= raceGates.Count)
                {
                    // Finished the lap!
                    isRaceActive = false;
                    Debug.Log($"<color=green>[Race] Lap Completed! Time: {currentRaceTime:F2}s</color>");

                    if (currentRaceTime < bestLapTime)
                    {
                        bestLapTime = currentRaceTime;
                    }

                    // Report score to UGS Leaderboard!
                    if (DroneLeaderboardService.Instance != null)
                    {
                        _ = DroneLeaderboardService.Instance.SubmitRaceScoreAsync(currentRaceTime);
                    }

                    // Reset track after 3 seconds
                    Invoke(nameof(ResetTrack), 3f);
                }
                else
                {
                    raceGates[currentGateIndex].SetTarget(true);
                }
            }
        }

        public void ResetTrack()
        {
            currentGateIndex = 0;
            isRaceActive = false;
            for (int i = 0; i < raceGates.Count; i++)
            {
                raceGates[i].ResetGate();
            }
            if (raceGates.Count > 0)
            {
                raceGates[0].SetTarget(true);
            }
        }

        private void PlayGateSound()
        {
            if (gateAudioSource != null)
            {
                gateAudioSource.pitch = 1.0f + (currentGateIndex * 0.08f);
                gateAudioSource.Play();
            }
        }
    }
}

