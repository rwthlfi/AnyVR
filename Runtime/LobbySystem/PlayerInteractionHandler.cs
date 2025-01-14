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

using UnityEngine;

namespace AnyVr.LobbySystem
{
    public class PlayerInteractionHandler : MonoBehaviour
    {
        [SerializeField] internal Transform _leftController, _rightController;
        [SerializeField] internal Transform _rig;
        [SerializeField] internal Transform _cam;

        private void OnDestroy()
        {
            s_interactionHandler = null;
        }

        #region Singleton

        internal static PlayerInteractionHandler s_interactionHandler;

        private void Awake()
        {
            if (s_interactionHandler != null)
            {
                Destroy(s_interactionHandler.gameObject);
                return;
            }

            s_interactionHandler = this;
        }

        #endregion
    }
}