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
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace AnyVR.UserControlSystem
{
    [RequireComponent(typeof(DynamicMoveProvider))]
    public class PCMovementLockHandler : MonoBehaviour
    {
        private DynamicMoveProvider _moveProvider;
        private static bool s_pcPlatform;

        private static PCMovementLockHandler s_instance;

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
            _moveProvider = GetComponent<DynamicMoveProvider>();
            s_pcPlatform = !PlatformInfo.IsXRPlatform();
        }

        public static void EnablePCMovement()
        {
            if (s_instance == null)
            {
                Debug.LogWarning("Could not enable PC movement. PCMovementLockHandler is null.");
            }
            s_instance._moveProvider.enabled = true;
        }
        
        public static void DisablePCMovement()
        {
            if (s_instance == null)
            {
                Debug.LogWarning("Could not disable PC movement. PCMovementLockHandler is null.");
            }
            if (s_pcPlatform)
            {
                s_instance._moveProvider.enabled = false;
            }
        }
    }
}
