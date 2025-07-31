// AnyVR is a multiuser, multiplatform XR framework.
// Copyright (C) 2024 Engineering Hydrology, RWTH Aachen University.
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
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace AnyVR.UserControlSystem.PC
{
    [RequireComponent(typeof(DynamicMoveProvider))]
    public class PCMovementLockHandler : MonoBehaviour
    {
        private static PCMovementLockHandler _instance;

        [SerializeField]
        private ushort _movementLockCounter;
        private DynamicMoveProvider _moveProvider;

        private readonly UnityEvent<bool> _onMovementLockToggle = new();

        /// <summary>
        ///     Read-only property that indicates whether the movement is currently locked or not.
        /// </summary>
        public static bool IsMovementLocked => !_instance._moveProvider.enabled;
        public static bool CanMove => _instance._movementLockCounter == 0;
        public static UnityEvent<bool> OnMovementLockToggle => _instance._onMovementLockToggle;



        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            _moveProvider = GetComponent<DynamicMoveProvider>();
            _movementLockCounter = 0;
        }

        private void Start()
        {
            PlatformManager.Instance.OnInitialized += () => 
            {
                if (PlatformInfo.IsXRPlatform())
                {
                    enabled = false;
                }
            };
        }

        private static void TogglePCMovement()
        {
            bool isLocked = _instance._movementLockCounter > 0;
            _instance._moveProvider.enabled = !isLocked;
        }

        public static void EnablePCMovement()
        {
            if (_instance == null)
            {
                Debug.LogError("Could not enable PC movement. PCMovementLockHandler is null.");
                return;
            }
            if (_instance._movementLockCounter > 0)
            {
                _instance._movementLockCounter--;
            }
            TogglePCMovement();
        }

        public static void DisablePCMovement()
        {
            if (_instance == null)
            {
                Debug.LogError("Could not disable PC movement. PCMovementLockHandler is null.");
                return;
            }
            _instance._movementLockCounter++;
            TogglePCMovement();
        }
    }
}
