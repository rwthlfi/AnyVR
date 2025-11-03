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

namespace AnyVR.UserControlSystem.PC
{
    public class PCInteractionDebugger : MonoBehaviour
    {
        private void LogInteraction(string message)
        {
            Debug.Log("[PC Interaction] " + message);
        }

        public void SelectionEntered()
        {
            LogInteraction("Selection Entered");
        }

        public void SelectionExited()
        {
            LogInteraction("Selection Exited");
        }

        public void PrimarySelectionAction()
        {
            LogInteraction("Primary Selection Action Triggered");
        }

        public void PrimarySelectionActionCanceled()
        {
            LogInteraction("Primary Selection Action Canceled");
        }

        public void SecondarySelectionAction()
        {
            LogInteraction("Secondary Selection Action Triggered");
        }

        public void SecondarySelectionActionCanceled()
        {
            LogInteraction("Secondary Selection Action Canceled");
        }
    }
}
