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

using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace AnyVR
{
    [RequireComponent(typeof(NetworkTransform), typeof(XRGrabInteractable), typeof(XRGeneralGrabTransformer))]
    public class NetworkGrabInteractable : NetworkBehaviour
    {
        private XRGrabInteractable _grabInteractable;
        private XRGeneralGrabTransformer _grabTransformer;

        private void Awake()
        {
            _grabInteractable = GetComponent<XRGrabInteractable>();
            _grabTransformer = GetComponent<XRGeneralGrabTransformer>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Debug.Log("owner id on init: " + OwnerId);
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);

            Debug.Log($"Current owner: {OwnerId}");
            
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
            
            RemoveOwnerRPC();
        }

        [ServerRpc(RequireOwnership = true)]
        private void RemoveOwnerRPC(NetworkConnection conn = null)
        {
            if (conn == null)
            {
                return;
            }
            if (OwnerId != conn.ClientId) // Only the owner can remove ownership with this server rpc
            {
                return;
            }
            RemoveOwnership();
        }
        [ServerRpc (RequireOwnership = false)]
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
        }
    }
}
