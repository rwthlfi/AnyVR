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
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace AnyVR.UserControlSystem.Interaction
{
    /// <summary>
    ///     An XR Socket Interactor that is network-aware.
    /// </summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    public class XRNetworkedSocketInteractor : NetworkBehaviour
    {
        [SerializeField]
        protected NetworkObject _objectInSocket;

        [SerializeField]
        protected UnityEvent<NetworkObject> _onSocketSelectOnClient;

        [SerializeField]
        protected UnityEvent<NetworkObject> _onSocketSelectExitOnClient;
        protected XRSocketInteractor _socketInteractor;
        public NetworkObject ObjectInSocket => _objectInSocket;
        public bool HasObjectInSocket => _objectInSocket != null;
        /// <summary>
        ///     Event invoked on clients when an object is selected in the socket on the server.
        /// </summary>
        public UnityEvent<NetworkObject> OnSocketSelectOnClient => _onSocketSelectOnClient;
        /// <summary>
        ///     Event invoked on clients when an object is released from the socket on the server.
        /// </summary>
        public UnityEvent<NetworkObject> OnSocketSelectExitOnClient => _onSocketSelectExitOnClient;

        protected virtual void Awake()
        {
            _socketInteractor = GetComponent<XRSocketInteractor>();
        }

        public override void OnStartServer()
        {
            _socketInteractor.selectEntered.AddListener(OnSocketSelect);

            _socketInteractor.selectEntered.AddListener(args =>
            {
                BroadCastSelectEnter(args.interactableObject.transform.GetComponent<NetworkObject>());
            });

            _socketInteractor.selectExited.AddListener(OnSocketRelease);

            _socketInteractor.selectExited.AddListener(args =>
            {
                BroadCastSelectExit(args.interactableObject.transform.GetComponent<NetworkObject>());
            });
        }

        public override void OnStartClient()
        {
            // Socket interactor grabs on server side, so client doesn't need it.
            _socketInteractor.enabled = false;
        }



        [ObserversRpc]
        private void UpdateObjectInSocket(NetworkObject netObj)
        {
            if (netObj == null)
            {
                Debug.Log($"[XRNetworkedSocketInteractor] Clearing object in socket {transform.name}.");
            }
            else
            {
                Debug.Log($"[XRNetworkedSocketInteractor] Updating object in socket {transform.name} to {netObj.transform.name}.");
            }
            _objectInSocket = netObj;
        }

        [Server]
        protected virtual void OnSocketSelect(SelectEnterEventArgs args)
        {
            Debug.Log($"[XRNetworkedSocketInteractor] Object {args.interactableObject.transform.name} selected in socket {transform.name} on server.");
            if (args.interactableObject.transform.TryGetComponent(out NetworkObject netObj))
            {
                _objectInSocket = netObj;
                UpdateObjectInSocket(netObj);
            }
        }

        [Server]
        private void OnSocketRelease(SelectExitEventArgs args)
        {
            Debug.Log($"[XRNetworkedSocketInteractor] Object {args.interactableObject.transform.name} released from socket {transform.name} on server.");
            if (args.interactableObject.transform.TryGetComponent(out Rigidbody rb))
            {
                // Stops physics simulation on server side when released from socket.
                // This is needed because server does not know that client grabbed object.
                rb.isKinematic = true;
            }
            _objectInSocket = null;
            UpdateObjectInSocket(null);
        }

        [Server]
        protected virtual void BroadCastSelectEnter(NetworkObject selectedObject)
        {
            NotifySelectEnterOnClients(selectedObject);
        }

        [ObserversRpc]
        protected virtual void NotifySelectEnterOnClients(NetworkObject selectedObject)
        {
            _onSocketSelectOnClient?.Invoke(selectedObject);
        }

        [Server]
        protected virtual void BroadCastSelectExit(NetworkObject releasedObject)
        {
            NotifySelectExitOnClients(releasedObject);
        }

        [ObserversRpc]
        protected virtual void NotifySelectExitOnClients(NetworkObject releasedObject)
        {
            _onSocketSelectExitOnClient?.Invoke(releasedObject);
        }
    }
}
