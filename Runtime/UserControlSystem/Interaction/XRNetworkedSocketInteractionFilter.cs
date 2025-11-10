// Anywhere Academy is a multiuser, multiplatform XR framework.
// Copyright (C) 2025 Engineering Hydrology, RWTH Aachen University.
// 
// Anywhere Academy is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published
// by the Free Software Foundation, either version 3 of the License,
// or (at your option) any later version.
// 
// Anywhere Academy is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANT-
// ABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Anywhere Academy.
// If not, see <https://www.gnu.org/licenses/>.

using FishNet.Object;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AnyVR.UserControlSystem
{
    public class XRNetworkedSocketInteractionFilter : MonoBehaviour, IXRHoverFilter, IXRSelectFilter
    {
       public bool canProcess => isActiveAndEnabled;

        public bool Process(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
            bool canHover = interactable.transform.gameObject.TryGetComponent(out NetworkObject netObj);
            canHover = canHover && netObj.Owner.ClientId == -1;
            return canHover;
        }

        public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            bool canHover = interactable.transform.gameObject.TryGetComponent(out NetworkObject netObj);
            canHover = canHover && netObj.Owner.ClientId == -1;
            return canHover;
        }
    }
}
