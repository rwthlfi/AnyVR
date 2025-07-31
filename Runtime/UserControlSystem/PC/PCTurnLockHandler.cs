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

namespace AnyVR.UserControlSystem.PC
{
    public class PCTurnLockHandler : MonoBehaviour
    {
        private static PCTurnLockHandler s_instance;

        [SerializeField]
        private ushort _turnLockCounter;

        private readonly UnityEvent<bool> _onTurnLockToggle = new();
        private PCTurnProvider _turnProvider;
        public static bool CanTurn => s_instance._turnLockCounter == 0;
        public static UnityEvent<bool> OnTurnLockToggle => s_instance._onTurnLockToggle;



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

            _turnProvider = GetComponent<PCTurnProvider>();
            _turnLockCounter = 0;
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

        private static void TogglePCTurning()
        {
            bool isLocked = s_instance._turnLockCounter > 0;
            s_instance._turnProvider.enabled = !isLocked;
        }

        public static void EnablePCTurning()
        {
            if (s_instance == null)
            {
                Debug.LogError("PCTurnLockHandler instance is null. Ensure it is initialized before calling this method.");
                return;
            }
            if (s_instance._turnLockCounter > 0)
            {
                s_instance._turnLockCounter--;
            }
            TogglePCTurning();
        }

        public static void DisablePCTurning()
        {
            if (s_instance == null)
            {
                Debug.LogError("PCTurnLockHandler instance is null. Ensure it is initialized before calling this method.");
                return;
            }
            s_instance._turnLockCounter++;
            TogglePCTurning();
        }
    }
}
