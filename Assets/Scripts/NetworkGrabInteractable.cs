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

using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace AnyVR
{
    [RequireComponent(typeof(XRGrabInteractable), typeof(XRGeneralGrabTransformer))]
    public class NetworkGrabInteractable : NetworkBehaviour
    {
        private XRGrabInteractable _grabInteractable;
        private XRGeneralGrabTransformer _grabTransformer;
        private Rigidbody _body;

        private bool _jump;

        private void Awake()
        {
            _grabInteractable = GetComponent<XRGrabInteractable>();
            _grabTransformer = GetComponent<XRGeneralGrabTransformer>();
            _body = GetComponent<Rigidbody>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Debug.Log("owner id on init: " + OwnerId);
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            TimeManager.OnTick += TimeManager_Tick;
            TimeManager.OnPostTick += TimeManager_PostTick;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            TimeManager.OnTick += TimeManager_Tick;
            TimeManager.OnPostTick += TimeManager_PostTick;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _jump = true;
            }
        }


        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);

            Debug.Log($"Current owner: {OwnerId}");
            
            if (IsOwner)
            {
                return;
            }

            _grabInteractable.enabled = OwnerId == -1;
            _grabTransformer.enabled = OwnerId == -1;
        }

        public void OnGrabSelectEnter()
        {
            RequestOwnershipRPC();
        }
        public void OnGrabSelectExit()
        {
            RemoveOwnerRPC();
        }

        private void TimeManager_Tick()
        {
            RunInputs(CreateReplicate());
        }

        private GrabReplicateData CreateReplicate()
        {
            if (!HasAuthority)
            {
                return default;
            }

            if (_jump)
            {
                Debug.LogWarning("Jumping");
                //_body.AddForce(Vector3.up);
                _jump = false;
            }

            GrabReplicateData data = new();
            return data;
        }

        private void TimeManager_PostTick()
        {
            if (HasAuthority)
            {
                CreateReconcile();
            }
        }

        [Replicate]
        private void RunInputs(GrabReplicateData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
        }

        public override void CreateReconcile()
        {
            RigidbodyState data = new()
            {
                Position = _body.position, 
                Rotation = _body.rotation,
                Velocity = _body.velocity,
                AngularVelocity = _body.angularVelocity
            };
            Debug.Log("Created Reconcile");
            Reconcile(data);
        }

        [Reconcile]
        private void Reconcile(RigidbodyState data, Channel channel = Channel.Unreliable)
        {
            Debug.Log("Reconciling");
            return;
            _body.position = data.Position;
            _body.rotation = data.Rotation;
            _body.velocity = data.Velocity;
            _body.angularVelocity = data.AngularVelocity;
        }

        [ServerRpc(RequireOwnership = true)]
        private void RemoveOwnerRPC(NetworkConnection conn = null)
        {
            if (conn == null)
            {
                return;
            }
            if (OwnerId != conn.ClientId) // Only the owner can remove ownership with this server rpc
            {
                return;
            }
            RemoveOwnership();
        }
        [ServerRpc (RequireOwnership = false)]
        private void RequestOwnershipRPC(NetworkConnection conn = null)
        {
            if (conn == null)
            {
                return;
            }
            if (OwnerId != -1) // Another client is already owner
            {
                return;
            }
            GiveOwnership(conn);
        }
    }
    
    public struct RigidbodyState : IReconcileData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public struct GrabReplicateData : IReplicateData
    {
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }
}
