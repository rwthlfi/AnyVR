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
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AnyVR.UserControlSystem.Interaction
{
    /// <summary>
    /// Manages ownership requests for interactables which have a <see cref="XRNetworkedGrabInteractable"> component 
    /// when they are selected or deselected by an interactor.
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractor))]
    public class InteractionOwnershipManager : MonoBehaviour
    {
        private void Awake()
        {
            XRBaseInteractor xRBaseInteractor = GetComponent<XRBaseInteractor>();
            xRBaseInteractor.selectEntered.AddListener(OnSelectEntered);
            xRBaseInteractor.selectExited.AddListener(OnSelectExited);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactableObject.transform.TryGetComponent(out INetworkOwnableInteractable networkOwnableInteractable))
            {
                Debug.Log("[InteractionOwnershipManager] Requesting ownership for interactable.", args.interactableObject.transform);
                networkOwnableInteractable.RequestOwnership();
            }
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (args.interactableObject.transform.TryGetComponent(out INetworkOwnableInteractable networkOwnableInteractable))
            {
                Debug.Log("[InteractionOwnershipManager] Releasing ownership for interactable.", args.interactableObject.transform);
                networkOwnableInteractable.ReleaseOwnership();
            }
        }
    }
}
