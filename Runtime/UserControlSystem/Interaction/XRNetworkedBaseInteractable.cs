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

using FishNet.Component.Ownership;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace AnyVR.UserControlSystem.Interaction
{
    [RequireComponent(typeof(PredictedOwner))]
    public abstract class XRNetworkedBaseInteractable : NetworkBehaviour, INetworkOwnableInteractable
    {
        private PredictedOwner _predictedOwner;

        private void Awake()
        {
            _predictedOwner = GetComponent<PredictedOwner>();
        }

        [Client]
        public void RequestOwnership(NetworkConnection conn = null)
        {
            Debug.Log($"[XRNetworkedBaseInteractable] Trying to take ownership for {name}", this);
            if (Owner.ClientId != -1 && Owner != conn)
            {
                Debug.Log($"[XRNetworkedBaseInteractable] Ownership request denied. Object is already owned by another client: {Owner}", this);
                return;
            }
            else if (Owner == conn)
            {
                Debug.Log($"[XRNetworkedBaseInteractable] Ownership request ignored. Requesting client already owns the object: {Owner}", this);
            }
            else
            {
                Debug.Log($"[XRNetworkedBaseInteractable] Taking ownership", this);
                _predictedOwner.TakeOwnership(true);
                RequestAllowTakeOwnership(false);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestAllowTakeOwnership(bool allowOwnership)
        {
            _predictedOwner.SetAllowTakeOwnership(allowOwnership);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ReleaseOwnership(NetworkConnection conn = null)
        {
            Debug.Log($"[XRNetworkedBaseInteractable] Ownership release request received from {conn}", this);
            if (Owner != conn)
            {
                Debug.Log($"[XRNetworkedBaseInteractable] Ownership release request denied. Requesting client does not own the object: {Owner}", this);
                return;
            }
            else
            {
                Debug.Log($"[XRNetworkedBaseInteractable] Releasing ownership from {conn}", this);
                NetworkObject.RemoveOwnership();
                _predictedOwner.SetAllowTakeOwnership(true);
            }
        }
    }
}
