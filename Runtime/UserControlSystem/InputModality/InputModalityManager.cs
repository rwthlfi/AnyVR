using AnyVR.PlatformManagement;
using AnyVR.UserControlSystem.PC;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace AnyVR.UserControlSystem.InputModality
{
    /// <summary>
    ///     Manages the user input based on the XR platform availability. Supports PC and XR input modalities.
    /// </summary>
    public class InputModalityManager : MonoBehaviour
    {
        [SerializeField] private ContinuousTurnProvider _xrTurnProvider;
        [SerializeField] private PCTurnProvider _pcTurnProvider;
        [SerializeField] private PCInteractionSystem _pcInteractionSystem;
        [SerializeField] private TrackedPoseDriver _cameraTrackedPoseDriver;
        [SerializeField] private Transform _xrGazeInteractionOrigin;
        [SerializeField] private Transform _pcGazeInteractionOrigin;
        [SerializeField] private XRGazeInteractor _gazeInteractor;

#if !UNITY_SERVER
        private void Start()
        {
            if (PlatformManager.Instance.IsXrStartupAttempted)
            {
                InitializeUserInput(PlatformInfo.IsXRPlatform());
                return;
            }

            PlatformManager.Instance.OnInitialized += Handler;
            return;

            void Handler()
            {
                InitializeUserInput(PlatformInfo.IsXRPlatform());
                PlatformManager.Instance.OnInitialized -= Handler;
            }
        }
#endif

        /// <summary>
        ///     Initializes the user input based on the XR platform availability.
        /// </summary>
        private void InitializeUserInput(bool isXRActive)
        {
            Debug.Log($"[InputModalityManager] Initializing user input. XR Active: {isXRActive}");
            ToggleXRControls(isXRActive);
            InitializeTurnProvider(isXRActive);
            InitializeGazeInteractor(isXRActive);
            SetCursorVisibility(false);
        }

        private void ToggleXRControls(bool isXRActive)
        {
            _xrTurnProvider.enabled = isXRActive;
            _pcTurnProvider.enabled = !isXRActive;
            _pcInteractionSystem.gameObject.SetActive(!isXRActive);
            _pcInteractionSystem.enabled = !isXRActive;
        }

        private void InitializeTurnProvider(bool isXRActive)
        {
            _cameraTrackedPoseDriver.enabled = isXRActive;
        }

        private void InitializeGazeInteractor(bool isXRActive)
        {
            _gazeInteractor.rayOriginTransform = isXRActive ? _xrGazeInteractionOrigin : _pcGazeInteractionOrigin;
        }

        private static void SetCursorVisibility(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}
