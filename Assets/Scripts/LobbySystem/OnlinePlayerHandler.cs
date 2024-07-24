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

using FishNet.Object;
using System;
using UnityEngine;

namespace LobbySystem
{
    public class OnlinePlayerHandler : NetworkBehaviour
    {
        [SerializeField] private Transform _leftController, _rightController;
        
        private PlayerInteractionHandler _interactionHandler;

        private bool _isInitialized = false;
        
        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                return;
            }
            
            GameObject setup = GameObject.Find("Player Interaction Setup");
            if (setup == null)
            {
                Debug.LogError("Error finding the Player Interaction Setup");
                return;
            }

            if (!setup.TryGetComponent(out PlayerInteractionHandler handler))
            {
                Debug.LogError("Player Interaction Setup has no PlayerInteractionHandler component");
                return;
            }

            _interactionHandler = handler;
            _isInitialized = true;
        }

        private void FixedUpdate()
        {
            if (!_isInitialized)
            {
                return;
            }

            _leftController.position = _interactionHandler._leftController.position;
            _leftController.rotation = _interactionHandler._leftController.rotation;
            _rightController.position = _interactionHandler._rightController.position;
            _rightController.rotation = _interactionHandler._rightController.rotation;
            transform.position = _interactionHandler._rig.position;
            transform.rotation = _interactionHandler._rig.rotation;
        }
    }
}
