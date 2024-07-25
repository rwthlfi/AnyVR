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
using System.Collections.Generic;
using UnityEngine;

namespace LobbySystem
{
    public class OnlinePlayerHandler : NetworkBehaviour
    {
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _leftController;
        [SerializeField] private Transform _rightController;

        private bool _isInit;

        private PlayerInteractionHandler _handler;

        private Renderer[] _renderers;
        
        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                return;
            }

            _handler = PlayerInteractionHandler.s_interactionHandler;
            if (_handler == null)
            {
                Debug.LogError("Could not find an instance of PlayerInteractionHandler");
            }

            _renderers = gameObject.transform.GetComponentsInChildren<Renderer>();
            int ownerLayer = LayerMask.NameToLayer("OwnerRenderLayer");
            foreach (Renderer r in _renderers)
            {
                r.gameObject.layer = ownerLayer;
            }

            _isInit = true;
        }

        private void Update()
        {
            if(!_isInit)
            {
                return;
            }

            transform.position = _handler._rig.position;

            _head.position = _handler._cam.position;
            _head.rotation = _handler._cam.rotation;
            _leftController.position = _handler._leftController.position;
            _leftController.rotation = _handler._leftController.rotation;
            _rightController.position = _handler._rightController.position;
            _rightController.rotation = _handler._rightController.rotation;
        }
    }
}
