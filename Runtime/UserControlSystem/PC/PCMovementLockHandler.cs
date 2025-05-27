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

namespace AnyVR.UserControlSystem
{
    [RequireComponent(typeof(DynamicMoveProvider))]
    public class PCMovementLockHandler : MonoBehaviour
    {
        private DynamicMoveProvider _moveProvider;
        
        private static PCMovementLockHandler s_instance;

        /// <summary>
        /// Read-only property that indicates whether the movement is currently locked or not.
        /// </summary>
        public static bool IsMovementLocked => !s_instance._moveProvider.enabled;

        [SerializeField]
        private ushort _movementLockCounter; 
        public static bool CanMove => s_instance._movementLockCounter == 0;

        private UnityEvent<bool> _onMovementLockToggle = new();
        public static UnityEvent<bool> OnMovementLockToggle => s_instance._onMovementLockToggle;



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

            _moveProvider = GetComponent<DynamicMoveProvider>();
            _movementLockCounter = 0;
        }

        private async void Start()
        {
            bool isXRPlatform = await PlatformInfo.IsXRPlatformAsync();
            if (isXRPlatform)
            {
                this.enabled = false;
                return;
            }
        }



        private static void TogglePCMovement()
        {
            bool isLocked = s_instance._movementLockCounter > 0;
            s_instance._moveProvider.enabled = !isLocked;
        }
        
        public static void EnablePCMovement()
        {
            if (s_instance == null)
            {
                Debug.LogError($"Could not enable PC movement. PCMovementLockHandler is null.");
                return;
            }
            if (s_instance._movementLockCounter > 0)
            {
                s_instance._movementLockCounter--;
            }
            TogglePCMovement();
        }
        
        public static void DisablePCMovement()
        {
            if (s_instance == null)
            {
                Debug.LogError($"Could not disable PC movement. PCMovementLockHandler is null.");
                return;
            }
            s_instance._movementLockCounter++;
            TogglePCMovement();
        }
    }
}
