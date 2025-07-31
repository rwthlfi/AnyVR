// AnyVR is a multiuser, multiplatform XR framework.
// Copyright (C) 2025 Engineering Hydrology, RWTH Aachen University.
// 
// AnyVR is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published
// by the Free Software Foundation, either version 3 of the License,
// or (at your option) any later version.
// 
// AnyVR is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANT-
// ABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with AnyVR.
// If not, see <https://www.gnu.org/licenses/>.

using AnyVR.PlatformManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace AnyVR.UserControlSystem.PC
{
    /// <summary>
    ///     Handles the locking and unlocking of the cursor in a PC environment.
    /// </summary>
    public class PCCursorLockHandler : MonoBehaviour
    {
        private static PCCursorLockHandler s_instance;

        [SerializeField]
        [Tooltip("The Input System Action that will be used to unlock the cursor.")]
        private InputActionProperty _cursorUnlockAction =
            new(new InputAction("Interaction", expectedControlType: "Button"));

        [SerializeField]
        [Tooltip("The Input System Action that will be used to lock the cursor.")]
        private InputActionProperty _cursorLockAction =
            new(new InputAction("Interaction", expectedControlType: "Button"));

        [SerializeField]
        private bool _isCursorUnlocked;
        /// <summary>
        ///     Read-only property that indicates whether the cursor is currently unlocked or not.
        /// </summary>
        public static bool IsCursorUnlocked
        {
            get => s_instance._isCursorUnlocked;
            private set
            {
                s_instance._isCursorUnlocked = value;
                s_instance.ToggleCursorUnlock(value);
            }
        }

        private readonly UnityEvent<bool> _onCursorUnlockToggle = new();
        public static UnityEvent<bool> OnCursorUnlockToggle => s_instance._onCursorUnlockToggle;



        private void Awake()
        {
            if (s_instance == null)
            {
                s_instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            PlatformManager.Instance.OnInitialized += () =>
            {
                if (PlatformInfo.IsXRPlatform())
                {
                    enabled = false;
                    return;
                }

                // Subscribes to the action events.
                _cursorUnlockAction.action.performed += UnlockCursorInputActionCallback;
                _cursorLockAction.action.performed += LockCursorInputActionCallback;

                // Locks cursor initially.
                IsCursorUnlocked = false;
            };
        }
        
        private void OnDestroy()
        {
            // Unsubscribes from the action events to prevent memory leaks.
            _cursorUnlockAction.action.performed -= UnlockCursorInputActionCallback;
            _cursorLockAction.action.performed -= LockCursorInputActionCallback;
        }



        private void ToggleCursorUnlock(bool isUnlocked)
        {
            Cursor.visible = isUnlocked;
            Cursor.lockState = isUnlocked ? CursorLockMode.None : CursorLockMode.Locked;

            // Disables and enables the respective actions.
            if (isUnlocked)
            {
                _cursorUnlockAction.action.Disable();
                _cursorLockAction.action.Enable();
            }
            else
            {
                _cursorUnlockAction.action.Enable();
                _cursorLockAction.action.Disable();
            }

            // Toggles movement and turning depending on the cursor state.
            if (isUnlocked)
            {
                PCMovementLockHandler.DisablePCMovement();
                PCTurnLockHandler.DisablePCTurning();
            }
            else
            {
                PCMovementLockHandler.EnablePCMovement();
                PCTurnLockHandler.EnablePCTurning();
            }

            // Invokes the event to notify subscribers about the cursor state change.
            OnCursorUnlockToggle.Invoke(isUnlocked);
        }

        private void UnlockCursorInputActionCallback(InputAction.CallbackContext context)
        {
            IsCursorUnlocked = true;
        }

        private void LockCursorInputActionCallback(InputAction.CallbackContext context)
        {
            IsCursorUnlocked = false;
        }

        /// <summary>
        ///     Unlocks the cursor.
        /// </summary>
        public static void UnlockCursor()
        {
            if (s_instance == null)
            {
                Debug.LogWarning("Could not unlock cursor. PCCursorLockHandler is null.");
                return;
            }
            IsCursorUnlocked = true;
        }

        /// <summary>
        ///     Locks the cursor.
        /// </summary>
        public static void LockCursor()
        {
            if (s_instance == null)
            {
                Debug.LogWarning("Could not lock cursor. PCCursorLockHandler is null.");
                return;
            }
            IsCursorUnlocked = false;
        }
    }
}
