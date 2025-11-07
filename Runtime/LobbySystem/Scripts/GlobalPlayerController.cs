using System.Threading.Tasks;
using AnyVR.LobbySystem.Internal;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public partial class GlobalPlayerController : PlayerController
    {
        private readonly RpcAwaiter<PlayerNameUpdateResult> _playerNameUpdateAwaiter = new(PlayerNameUpdateResult.Timeout, PlayerNameUpdateResult.Cancelled);

        public static GlobalPlayerController Instance { get; private set; }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Assert.IsNull(Instance);
            Instance = this;
        }

        /// <summary>
        ///     Updates the player's name.
        ///     Does not succeed if this is not the local player's global state.
        /// </summary>
        /// <param name="playerName">The desired name</param>
        /// <returns>A PlayerNameUpdateResult indicating the result of the update.</returns>
        [Client]
        public Task<PlayerNameUpdateResult> SetName(string playerName)
        {
            Task<PlayerNameUpdateResult> task = _playerNameUpdateAwaiter.WaitForResult();
            ServerRPC_SetName(playerName);
            return task;
        }

        [TargetRpc]
        private void TargetRPC_OnNameChange(NetworkConnection _, PlayerNameUpdateResult playerNameUpdateResult)
        {
            _playerNameUpdateAwaiter?.Complete(playerNameUpdateResult);
        }
    }
}
