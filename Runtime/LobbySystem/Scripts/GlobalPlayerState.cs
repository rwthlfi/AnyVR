using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AnyVR.LobbySystem.Internal;
using AnyVR.PlatformManagement;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public class GlobalPlayerState : NetworkBehaviour
    {
        private readonly RpcAwaiter<PlayerNameUpdateResult> _playerNameUpdateAwaiter = new(PlayerNameUpdateResult.Timeout, PlayerNameUpdateResult.Cancelled);

        private Coroutine _kickPlayerCoroutine;

#region Replicated Properties

        private readonly SyncVar<string> _playerName = new("null");

        private readonly SyncVar<PlatformType> _platformType = new(PlatformType.Unknown);
        public static GlobalPlayerState LocalPlayer { get; private set; }

#endregion

#region Lifecycle Overrides

        public override void OnStartServer()
        {
            base.OnStartServer();
            _kickPlayerCoroutine = StartCoroutine(KickIfNameNotSet(3));
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!IsOwner)
                return;

            if (!IsController)
                return;

            Assert.IsNull(LocalPlayer);
            LocalPlayer = this;
        }

#endregion

#region Public API

        public int GetID()
        {
            return OwnerId;
        }

        public string GetName()
        {
            return _playerName.Value;
        }

        public PlatformType GetPlatformType()
        {
            return _platformType.Value;
        }

        [Client]
        public Task<PlayerNameUpdateResult> SetName([DisallowNull] string playerName, TimeSpan? timeout = null)
        {
            if (playerName is null)
                throw new ArgumentNullException(nameof(playerName));

            Task<PlayerNameUpdateResult> task = _playerNameUpdateAwaiter.WaitForResult(timeout);
            ServerRPC_SetName(playerName);
            return task;
        }

#endregion

#region Server Method

        [ServerRpc(RequireOwnership = true)]
        private void ServerRPC_SetName(string playerName)
        {
            PlayerNameUpdateResult result = SetName_Internal(playerName);

            TargetRPC_OnNameChange(Owner, result);

            if (result == PlayerNameUpdateResult.Success || _playerName.Value != "null")
                return;

            if (_kickPlayerCoroutine != null)
            {
                StopCoroutine(_kickPlayerCoroutine);
                _kickPlayerCoroutine = null;
            }

            // Ensure the TargetRPC_OnNameChange is received before the kick.
            _kickPlayerCoroutine = StartCoroutine(KickIfNameNotSet(0.1f));
        }

        private IEnumerator KickIfNameNotSet(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (_playerName.Value != "null")
                yield break;

            Owner.Kick(KickReason.UnusualActivity);
        }

        [Server]
        private PlayerNameUpdateResult SetName_Internal(string playerName)
        {
            playerName = Regex.Replace(playerName.Trim(), @"\s+", " ");

            if (playerName.Equals(_playerName.Value))
            {
                return PlayerNameUpdateResult.AlreadySet;
            }

            PlayerNameUpdateResult result = PlayerNameValidator.ValidatePlayerName(playerName);
            if (result == PlayerNameUpdateResult.Success)
            {
                _playerName.Value = playerName;
            }

            return result;
        }

        [TargetRpc]
        private void TargetRPC_OnNameChange(NetworkConnection _, PlayerNameUpdateResult playerNameUpdateResult)
        {
            _playerNameUpdateAwaiter?.Complete(playerNameUpdateResult);
        }

        [ServerRpc(RequireOwnership = true)]
        private void ServerRPC_SetPlatformType(PlatformType platformType)
        {
            _platformType.Value = platformType;
        }

#endregion
    }
}
