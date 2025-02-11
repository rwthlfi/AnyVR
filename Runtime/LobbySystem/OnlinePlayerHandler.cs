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

using FishNet.Object;
using UnityEngine;
using UnityEngine.Animations;

namespace AnyVr.LobbySystem
{
    public class OnlinePlayerHandler : NetworkBehaviour
    {
        [SerializeField] private PositionConstraint headPositionConstraint;
        [SerializeField] private RotationConstraint headRotationConstraint;

        [SerializeField] private PositionConstraint leftHandPositionConstraint;
        [SerializeField] private RotationConstraint leftHandRotationConstraint;

        [SerializeField] private PositionConstraint rightHandPositionConstraint;
        [SerializeField] private RotationConstraint rightHandRotationConstraint;

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
                Logger.LogError("Could not find an instance of PlayerInteractionHandler");
                return;
            }

            headPositionConstraint.AddSource(new ConstraintSource { sourceTransform = _handler.Head, weight = 1 });
            headRotationConstraint.AddSource(new ConstraintSource { sourceTransform = _handler.Head, weight = 1 });
            leftHandPositionConstraint.AddSource(new ConstraintSource
            {
                sourceTransform = _handler.LeftHand, weight = 1
            });
            leftHandRotationConstraint.AddSource(new ConstraintSource
            {
                sourceTransform = _handler.LeftHand, weight = 1
            });
            rightHandPositionConstraint.AddSource(new ConstraintSource
            {
                sourceTransform = _handler.RightHand, weight = 1
            });
            rightHandRotationConstraint.AddSource(new ConstraintSource
            {
                sourceTransform = _handler.RightHand, weight = 1
            });

            _constraints = new IConstraint[]
            {
                headPositionConstraint, headRotationConstraint, leftHandPositionConstraint,
                leftHandRotationConstraint, rightHandPositionConstraint, rightHandRotationConstraint
            };

            foreach (IConstraint c in _constraints)
            {
                c.constraintActive = true;
            }

            // Disable renderers for the owning player.
            foreach (Renderer r in gameObject.GetComponentsInChildren<Renderer>())
            {
                r.enabled = false;
            }
        }
    }
}