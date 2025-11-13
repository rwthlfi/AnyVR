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

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AnyVR.UserControlSystem.Interaction
{
    /// <summary>
    ///     An interaction filter that only allows selection of networked objects if the local user has ownership.
    ///     Doesnt affect interaction with non-networked objects.
    /// </summary>
    public class NetObjectOwnershipInteractionFilter : MonoBehaviour, IXRSelectFilter
    {
        public bool canProcess => isActiveAndEnabled;

        /// <summary>
        ///     Processes the selection interaction based on network ownership
        /// </summary>
        /// <param name="interactor"> The interactor attempting to select the interactable.</param>
        /// <param name="interactable"> The interactable being selected.</param>
        /// <returns>
        ///     Returns <see langword="true" /> if no other client owns the interactable or if the local user is the owner;
        ///     otherwise, <see langword="false" />.
        /// </returns>
        /// "/>
        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            bool isNetworked = interactable.transform.TryGetComponent(out INetworkOwnableInteractable networkOwnableInteractable);
            if (isNetworked)
            {
                Debug.Log($"[NetObjectOwnershipInteractionFilter] Checking ownership for network interactable {interactable}.", interactable.transform);
                // Checks if there is already an owner different from the local user.
                if (networkOwnableInteractable.Owner.ClientId != -1)
                {
                    Debug.Log($"[NetObjectOwnershipInteractionFilter] Interactable {interactable} has owner with ClientId {networkOwnableInteractable.Owner.ClientId}. " +
                        $"Local ownership status: {networkOwnableInteractable.NetworkObject.IsOwner}.", interactable.transform);
                    return networkOwnableInteractable.NetworkObject.IsOwner;
                }
                Debug.Log($"[NetObjectOwnershipInteractionFilter] Interactable {interactable} has no owner. Selection possible.", interactable.transform);
                return true;
            }
            Debug.Log($"[NetObjectOwnershipInteractionFilter] Interactable {interactable} is not networked. Selection possible.", interactable.transform);
            return true;
        }
    }
}
