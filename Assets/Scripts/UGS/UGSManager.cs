using System;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_SERVICES_CORE
using Unity.Services.Core;
using Unity.Services.Authentication;
#endif

namespace DroneSimulator.UGS
{
    public class UGSManager : MonoBehaviour
    {
        public static UGSManager Instance { get; private set; }

        public bool IsInitialized { get; private set; } = false;
        public bool IsSignedIn { get; private set; } = false;
        public string PlayerId { get; private set; } = string.Empty;

        public event Action OnUGSReady;

        private async void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                await InitializeUGSAsync();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task InitializeUGSAsync()
        {
            try
            {
                Debug.Log("<color=yellow>[UGS] Initializing Unity Gaming Services...</color>");
#if UNITY_SERVICES_CORE
                await UnityServices.InitializeAsync();
                IsInitialized = true;
                Debug.Log("<color=green>[UGS] Unity Services Initialized Successfully.</color>");

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.Log("<color=yellow>[UGS] Signing in anonymously...</color>");
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    PlayerId = AuthenticationService.Instance.PlayerId;
                    IsSignedIn = true;
                    Debug.Log($"<color=green>[UGS] Signed in successfully. Player ID: {PlayerId}</color>");
                }
#else
                Debug.Log("[UGS] Unity Services package is active in project manifest. Running in local simulation mode.");
                IsInitialized = true;
                IsSignedIn = true;
                PlayerId = "LocalPilot_" + UnityEngine.Random.Range(1000, 9999);
#endif
                OnUGSReady?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UGS] Initialization notice: {ex.Message}. Drone flight simulator will operate in offline mode.");
            }
        }
    }
}

