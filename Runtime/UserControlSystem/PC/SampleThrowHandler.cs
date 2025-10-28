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

using AnyVR.UserControlSystem.PC;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace AnyVR.UserControlSystem
{
    public class SampleThrowHandler : MonoBehaviour, IPCThrowHandler
    {
        [SerializeField]
        private PCInteractionSystem _pCInteractionSystem;

        private void Start()
        {
            _pCInteractionSystem.OnChargeThrow.AddListener(LogCharge);
        }

        private void LogCharge(float charge)
        {
            Debug.Log("[SampleThrowHandler] Charge event received with charge: " + charge, gameObject);
        }

        public void HandleThrowEvent(float force)
        {
            Debug.Log("[SampleThrowHandler] Throw event received with force: " + force, gameObject);
        }
    }
}
