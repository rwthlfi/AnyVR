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

using FishNet.Object;
using FishNet.Observing;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace AnyVR.UserControlSystem.Interaction
{
    /// <summary>
    ///     A networked simple interactable for XR interactions.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkObserver))]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(XRSimpleInteractable))]
    [RequireComponent(typeof(XRSynchronizedInteractions))]
    public class XRNetworkedSimpleInteractable : XRNetworkedBaseInteractable
    {
        // This class can be expanded in the future for simple interactable objects.
    }
}
