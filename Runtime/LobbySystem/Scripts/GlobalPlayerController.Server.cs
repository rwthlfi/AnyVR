using System.Collections;
using System.Text.RegularExpressions;
using AnyVR.LobbySystem.Internal;
using FishNet.Managing.Server;
using FishNet.Object;
using UnityEngine;

namespace AnyVR.LobbySystem
{
    public partial class GlobalPlayerController
    {
        private Coroutine _kickPlayerCoroutine;

        [ServerRpc(RequireOwnership = true)]
        private void ServerRPC_SetName(string playerName)
        {
            PlayerNameUpdateResult result = SetName_Internal(playerName);

            TargetRPC_OnNameChange(Owner, result);

            if (result == PlayerNameUpdateResult.Success || GetPlayerState<GlobalPlayerState>().Name != "null")
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

            if (GetPlayerState<GlobalPlayerState>().Name != "null")
                yield break;

            Owner.Kick(KickReason.UnusualActivity);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _kickPlayerCoroutine = StartCoroutine(KickIfNameNotSet(3));
        }

        [Server]
        private PlayerNameUpdateResult SetName_Internal(string playerName)
        {
            playerName = Regex.Replace(playerName.Trim(), @"\s+", " ");

            if (playerName.Equals(GetPlayerState<GlobalPlayerState>().Name))
            {
                return PlayerNameUpdateResult.AlreadySet;
            }

            PlayerNameUpdateResult result = PlayerNameValidator.ValidatePlayerName(playerName);
            if (result == PlayerNameUpdateResult.Success)
            {
                GetPlayerState<GlobalPlayerState>().SetName(playerName);
            }

            return result;
        }
    }
}
