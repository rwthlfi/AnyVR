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

using AnyVR.Logging;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    public class OnlinePlayerHandler : NetworkBehaviour
    {
        private const string Tag = nameof(OnlinePlayerHandler);

        [FormerlySerializedAs("headPositionConstraint")]
        [SerializeField] private PositionConstraint _headPositionConstraint;
        [FormerlySerializedAs("headRotationConstraint")]
        [SerializeField] private RotationConstraint _headRotationConstraint;

        [FormerlySerializedAs("leftHandPositionConstraint")]
        [SerializeField] private PositionConstraint _leftHandPositionConstraint;
        [FormerlySerializedAs("leftHandRotationConstraint")]
        [SerializeField] private RotationConstraint _leftHandRotationConstraint;

        [FormerlySerializedAs("rightHandPositionConstraint")]
        [SerializeField] private PositionConstraint _rightHandPositionConstraint;
        [FormerlySerializedAs("rightHandRotationConstraint")]
        [SerializeField] private RotationConstraint _rightHandRotationConstraint;

        private IConstraint[] _constraints;

        private PlayerInteractionHandler _handler;

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsOwner)
            {
                return;
            }

            _handler = PlayerInteractionHandler.GetInstance();
            if (_handler == null)
            {
                Logger.Log(LogLevel.Error, Tag, "Could not find an instance of PlayerInteractionHandler");
                return;
            }

            _headPositionConstraint.AddSource(new ConstraintSource
            {
                sourceTransform = _handler.Head, weight = 1
            });
            _headRotationConstraint.AddSource(new ConstraintSource
            {
                sourceTransform = _handler.Head, weight = 1
            });
            _leftHandPositionConstraint.AddSource(new ConstraintSource
            {
                sourceTransform = _handler.LeftHand, weight = 1
            });
            _leftHandRotationConstraint.AddSource(new ConstraintSource
            {
                sourceTransform = _handler.LeftHand, weight = 1
            });
            _rightHandPositionConstraint.AddSource(new ConstraintSource
            {
                sourceTransform = _handler.RightHand, weight = 1
            });
            _rightHandRotationConstraint.AddSource(new ConstraintSource
            {
                sourceTransform = _handler.RightHand, weight = 1
            });

            _constraints = new IConstraint[]
            {
                _headPositionConstraint, _headRotationConstraint, _leftHandPositionConstraint, _leftHandRotationConstraint, _rightHandPositionConstraint, _rightHandRotationConstraint
            };

            foreach (IConstraint c in _constraints)
            {
                c.constraintActive = true;
            }
        }
    }
}
