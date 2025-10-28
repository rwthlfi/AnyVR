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
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace AnyVR.UserControlSystem
{
    [RequireComponent(typeof(XRBaseInteractable))]
    public class PCSecondarySelectionAction : MonoBehaviour
    {
        [SerializeField]
        private ActivateEvent _secondarySelectActivated = new();
        /// <summary>
        ///   Event triggered when the secondary select action is activated and the interactable is selected. 
        ///   Can be used to trigger events that are otherwise handled by VR affordances.
        /// </summary>
        public ActivateEvent SecondarySelectActivated => _secondarySelectActivated;

        [SerializeField]
        private DeactivateEvent _secondarySelectDeactivated = new();
        /// <summary>
        ///  Event triggered when the secondary select action is deactivated and the interactable is selected.
        /// <!/summary>
        public DeactivateEvent SecondarySelectDeactivated => _secondarySelectDeactivated;
    }
}
