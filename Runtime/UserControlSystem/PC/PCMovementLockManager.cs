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

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace AnyVR.UserControlSystem
{
    /// <summary>
    ///     Manages locking (resp. unlocking) of the player movement when an InputField field is selected (resp. deselected).
    ///     The input field has to have the <see cref="AnyVrKeyboardDisplayHandler"/> component which disables the xr keyboard display on PC.
    /// </summary>
    internal class PCMovementLockManager : MonoBehaviour
    {
        [SerializeField] private DynamicMoveProvider moveProvider;
        private void Start()
        {
            AnyVrKeyboardDisplayHandler.OnSelect += OnInputFieldSelected;
            AnyVrKeyboardDisplayHandler.OnDeselect += OnInputFieldDeselected;
        }

        private void OnInputFieldSelected(BaseEventData obj)
        {
            moveProvider.enabled = false;
        }
        
        private void OnInputFieldDeselected(BaseEventData obj)
        {
            moveProvider.enabled = true;
        }

        private void OnDestroy()
        {
            AnyVrKeyboardDisplayHandler.OnSelect -= OnInputFieldSelected;
            AnyVrKeyboardDisplayHandler.OnDeselect -= OnInputFieldDeselected;
        }
    }
}
