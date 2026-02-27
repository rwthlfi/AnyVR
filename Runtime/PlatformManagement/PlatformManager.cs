using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

namespace AnyVR.PlatformManagement
{
    public sealed class PlatformManager : MonoBehaviour
    {
        private const string Tag = nameof(PlatformManager);

        private XRManagerSettings _xrManager;

        /// <summary>
        ///     True after XR initialization has been attempted, regardless of success.
        /// </summary>
        public bool IsXrStartupAttempted { get; private set; }

        /// <summary>
        ///     True if XR successfully initialized and an active loader is available.
        /// </summary>
        public bool IsXrActive => _xrManager != null && _xrManager.activeLoader != null;

        private void Awake()
        {
            InitSingleton();
        }

        private IEnumerator Start()
        {
            Debug.Log("Initializing XR...");

            // XRGeneralSettings.Instance is null in headless server builds
            if (XRGeneralSettings.Instance != null)
            {
                _xrManager = XRGeneralSettings.Instance.Manager;
            }

            yield return _xrManager?.InitializeLoader();

            if (IsXrActive)
            {
                _xrManager?.StartSubsystems();
            }

            IsXrStartupAttempted = true;
            OnInitialized?.Invoke();

            Debug.Log($"XR initialization {(IsXrActive ? "succeeded" : "failed")}");
            SceneManager.activeSceneChanged += (_, _) => OnInitialized?.Invoke();
        }

        /// <summary>
        ///     Called after the xr initialization was attempted.
        ///     After this <see cref="PlatformManager.IsXrActive" /> is true on XR platforms.
        /// </summary>
        public event Action OnInitialized;

#region Singleton

        public static PlatformManager Instance { get; private set; }

        private void InitSingleton()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                StartCoroutine(Instance.Start());
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

#endregion
    }
}
