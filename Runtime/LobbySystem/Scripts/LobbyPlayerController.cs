using AnyVR.LobbySystem.Internal;
using AnyVR.Voicechat;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Serialization;

namespace AnyVR.LobbySystem
{
    public partial class LobbyPlayerController : PlayerController
    {
#region Serialized Properties

        /// <summary>
        ///     Prefab to spawn for the player.
        /// </summary>
        [FormerlySerializedAs("_playerPrefab")]
        [SerializeField] private NetworkObject _playerAvatarPrefab;

#endregion

#region Private Fields

        protected LiveKitClient LiveKitClient;

        private readonly RpcAwaiter<PlayerKickResult> _playerKickUpdateAwaiter = new(PlayerKickResult.Timeout, PlayerKickResult.Cancelled);

        private readonly RpcAwaiter<PlayerPromotionResult> _playerPromoteUpdateAwaiter = new(PlayerPromotionResult.Timeout, PlayerPromotionResult.Cancelled);

#endregion

#region Singleton

        private static LobbyPlayerController _instance;

        private void Awake()
        {
            _instance = this;
        }

        [Client]
        public static LobbyPlayerController GetInstance()
        {
            return _instance;
        }

#endregion
    }
}
