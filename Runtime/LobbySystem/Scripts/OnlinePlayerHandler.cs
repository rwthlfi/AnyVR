using System;
using AnyVR.Logging;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    // TODO delete
    [Obsolete]
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
