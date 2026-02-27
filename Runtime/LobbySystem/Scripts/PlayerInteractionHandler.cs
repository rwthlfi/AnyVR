using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

namespace AnyVR.LobbySystem
{
    // TODO delete
    [Obsolete]
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
