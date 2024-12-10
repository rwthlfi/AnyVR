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

using FishNet.Component.Prediction;
using FishNet.Connection;
using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace AnyVR
{
    [RequireComponent(typeof(XRGrabInteractable), typeof(XRGeneralGrabTransformer))]
    public class NetworkGrabInteractable : NetworkBehaviour
    {
        private XRGrabInteractable _grabInteractable;
        private XRGeneralGrabTransformer _grabTransformer;
        private Rigidbody _body;

        private void Awake()
        {
            _grabInteractable = GetComponent<XRGrabInteractable>();
            _grabTransformer = GetComponent<XRGeneralGrabTransformer>();
            _body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (IsServerInitialized && OwnerId == -1 && _body.isKinematic)
            {
                _body.isKinematic = false;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _body.isKinematic = false;
        }

        public override void OnStartClient()
        {
            _body.isKinematic = true;
            base.OnStartClient();
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);

            _body.isKinematic = !IsOwner;

            if (IsOwner)
            {
                return;
            }

            _grabInteractable.enabled = OwnerId == -1;
            _grabTransformer.enabled = OwnerId == -1;
        }

        public void OnGrabSelectEnter()
        {
            RequestOwnershipRPC();
        }

        public void OnGrabSelectExit()
        {
            RigidbodyState bodyState = new() { Position = _body.position, Rotation = _body.rotation };
            // TODO
            RemoveOwnerRPC(bodyState);
        }

        [ServerRpc(RequireOwnership = true)]
        private void RemoveOwnerRPC(RigidbodyState state, NetworkConnection conn = null)
        {
            if (conn == null)
            {
                return;
            }

            if (OwnerId != conn.ClientId) // Only the owner can remove ownership with this server rpc
            {
                return;
            }

            _body.isKinematic = false;
            RemoveOwnership();
            Debug.Log($"Throwing {name}: v: {state.Velocity}"); //BUG
            _body.position = state.Position;
            _body.rotation = state.Rotation;
            _body.velocity = state.Velocity;
            _body.angularVelocity = state.AngularVelocity;
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestOwnershipRPC(NetworkConnection conn = null)
        {
            if (conn == null)
            {
                return;
            }

            if (OwnerId != -1) // Another client is already owner
            {
                return;
            }

            GiveOwnership(conn);
            _body.isKinematic = true;
        }
    }
}