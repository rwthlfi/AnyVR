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

using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

namespace AnyVR.LobbySystem
{
    public class PlayerInteractionHandler : MonoBehaviour
    {
        [FormerlySerializedAs("head")]
        [SerializeField] private Transform _head;

        [FormerlySerializedAs("leftHand")]
        [SerializeField] private Transform _leftHand;

        [FormerlySerializedAs("rightHand")]
        [SerializeField] private Transform _rightHand;
        public Transform Head => _head;
        public Transform LeftHand => _leftHand;
        public Transform RightHand => _rightHand;

        #region Singleton

        private static PlayerInteractionHandler _instance;

        [CanBeNull]
        public static PlayerInteractionHandler GetInstance()
        {
            return _instance;
        }

        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
            }

            _instance = this;
        }

        #endregion
    }
}
